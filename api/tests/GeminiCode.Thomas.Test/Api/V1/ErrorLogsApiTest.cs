using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetById;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetPaginated;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetById;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using GeminiCode.Thomas.Test.Integration.Setup;
using GeminiCode.Thomas.Test.Shared.Faker;
using Xunit;

namespace GeminiCode.Thomas.Test.Api.V1
{
    /// <summary>
    /// Exercises the whole error log pipeline through the API: the synchronization collects the
    /// errors from (a fake) NewRelic, stores them on the knowledge base and keeps it up to date.
    /// </summary>
    public class ErrorLogsApiTest : IntegrationTestsBootstrapper
    {
        public ErrorLogsApiTest(IntegrationTestsFixture fixture) : base(fixture)
        {
            LoginWithDefaultUser();
        }

        [Fact]
        public async Task SyncErrors_OnTheFirstRun_RegistersTheErrorsAsNew()
        {
            //arrange
            var project = await CreateProjectAsync();

            IntegrationTestsFixture.NewRelicErrorProvider.SetEvents(project.NewRelicId,
                ErrorLogFaker.GetNewRelicErrorEvent("System.NullReferenceException", "Object reference not set"),
                ErrorLogFaker.GetNewRelicErrorEvent("System.NullReferenceException", "Object reference not set"),
                ErrorLogFaker.GetNewRelicErrorEvent("System.TimeoutException", "Request 42 timed out"));

            //act
            var httpResponse = await AuthenticatedHttpClient.PostAsync($"/Projects/{project.PublicId}/Errors/Sync", null);

            var summary = await GetResultContentAsync<SyncProjectErrorsResponse>(httpResponse);

            //assert
            Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
            Assert.Equal(project.PublicId, summary.ProjectId);
            Assert.Equal(3, summary.EventsProcessed);
            Assert.Equal(2, summary.NewErrors);
            Assert.Equal(0, summary.UpdatedErrors);

            var errorLogs = await GetErrorLogsAsync(project.PublicId);

            Assert.Equal(2, errorLogs.Count);
            Assert.All(errorLogs.Items, errorLog => Assert.Equal(ErrorLogStatus.New, errorLog.Status));

            var nullReference = Assert.Single(errorLogs.Items, errorLog => errorLog.ErrorClass == "System.NullReferenceException");
            Assert.Equal(2, nullReference.Occurrences);

            var projectAfterSync = await GetResultContentAsync<ProjectGetByIdResponse>(await AuthenticatedHttpClient.GetAsync($"/Projects/{project.PublicId}"));
            Assert.Equal(summary.Until, projectAfterSync.LastErrorSyncAt);
        }

        [Fact]
        public async Task SyncErrors_OnTheNextRuns_UpdatesTheKnownErrorsAndCollectsSinceTheLastSync()
        {
            //arrange
            var project = await CreateProjectAsync();

            IntegrationTestsFixture.NewRelicErrorProvider.SetEvents(project.NewRelicId,
                ErrorLogFaker.GetNewRelicErrorEvent("System.InvalidOperationException", "Sequence contains no elements"));

            var firstSummary = await GetResultContentAsync<SyncProjectErrorsResponse>(
                await AuthenticatedHttpClient.PostAsync($"/Projects/{project.PublicId}/Errors/Sync", null));

            IntegrationTestsFixture.NewRelicErrorProvider.SetEvents(project.NewRelicId,
                ErrorLogFaker.GetNewRelicErrorEvent("System.InvalidOperationException", "Sequence contains no elements"),
                ErrorLogFaker.GetNewRelicErrorEvent("System.InvalidOperationException", "Sequence contains no elements"),
                ErrorLogFaker.GetNewRelicErrorEvent("System.ArgumentException", "Value cannot be null"));

            //act
            var httpResponse = await AuthenticatedHttpClient.PostAsync($"/Projects/{project.PublicId}/Errors/Sync", null);

            var secondSummary = await GetResultContentAsync<SyncProjectErrorsResponse>(httpResponse);

            //assert
            Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
            Assert.Equal(1, secondSummary.NewErrors);
            Assert.Equal(1, secondSummary.UpdatedErrors);
            Assert.Equal(firstSummary.Until, secondSummary.Since);

            var errorLogs = await GetErrorLogsAsync(project.PublicId);

            Assert.Equal(2, errorLogs.Count);

            var recurring = Assert.Single(errorLogs.Items, errorLog => errorLog.ErrorClass == "System.InvalidOperationException");
            Assert.Equal(ErrorLogStatus.Recurring, recurring.Status);
            Assert.Equal(3, recurring.Occurrences);

            var newError = Assert.Single(errorLogs.Items, errorLog => errorLog.ErrorClass == "System.ArgumentException");
            Assert.Equal(ErrorLogStatus.New, newError.Status);
            Assert.Equal(1, newError.Occurrences);

            var calls = IntegrationTestsFixture.NewRelicErrorProvider.Calls.Where(call => call.NewRelicId == project.NewRelicId).ToList();
            Assert.Equal(2, calls.Count);
            Assert.Equal(calls[0].Until, calls[1].Since);
        }

        [Fact]
        public async Task SyncErrors_WithInvalidProjectId_ReturnsNotFound()
        {
            //arrange & act
            var httpResponse = await AuthenticatedHttpClient.PostAsync($"/Projects/{Guid.NewGuid()}/Errors/Sync", null);

            //assert
            Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        }

        [Fact]
        public async Task GetErrorLogById_ReturnsTheDetailsWithTheStackTrace()
        {
            //arrange
            var project = await CreateProjectAsync();
            var errorEvent = ErrorLogFaker.GetNewRelicErrorEvent("System.DivideByZeroException", "Attempted to divide by zero");

            IntegrationTestsFixture.NewRelicErrorProvider.SetEvents(project.NewRelicId, errorEvent);

            await AuthenticatedHttpClient.PostAsync($"/Projects/{project.PublicId}/Errors/Sync", null);

            var listed = Assert.Single((await GetErrorLogsAsync(project.PublicId)).Items);

            //act
            var httpResponse = await AuthenticatedHttpClient.GetAsync($"/ErrorLogs/{listed.PublicId}");

            var errorLog = await GetResultContentAsync<ErrorLogGetByIdResponse>(httpResponse);

            //assert
            Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
            Assert.Equal(listed.PublicId, errorLog.PublicId);
            Assert.Equal(project.PublicId, errorLog.ProjectId);
            Assert.Equal(errorEvent.ErrorClass, errorLog.ErrorClass);
            Assert.Equal(errorEvent.Message, errorLog.Message);
            Assert.Equal(errorEvent.StackTrace, errorLog.StackTrace);
            Assert.Equal(errorEvent.TransactionName, errorLog.TransactionName);
            Assert.Equal(errorEvent.ErrorGroupId, errorLog.NewRelicErrorGroupId);
            Assert.Equal(errorEvent.Timestamp, errorLog.FirstSeenAt);
            Assert.Equal(errorEvent.Timestamp, errorLog.LastSeenAt);
        }

        [Fact]
        public async Task GetErrorLogById_WithInvalidId_ReturnsNotFound()
        {
            //arrange & act
            var httpResponse = await AuthenticatedHttpClient.GetAsync($"/ErrorLogs/{Guid.NewGuid()}");

            //assert
            Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        }

        [Fact]
        public async Task GetErrorLogs_WithStatusAndQueryFilters_ReturnsOnlyTheMatchingErrors()
        {
            //arrange
            var project = await CreateProjectAsync();

            IntegrationTestsFixture.NewRelicErrorProvider.SetEvents(project.NewRelicId,
                ErrorLogFaker.GetNewRelicErrorEvent("System.Net.Sockets.SocketException", "Connection refused"),
                ErrorLogFaker.GetNewRelicErrorEvent("System.FormatException", "Input string was not in a correct format"));

            await AuthenticatedHttpClient.PostAsync($"/Projects/{project.PublicId}/Errors/Sync", null);

            //act
            var byQuery = await GetErrorLogsAsync(project.PublicId, "&filter.Query=socket");
            var byStatusNew = await GetErrorLogsAsync(project.PublicId, $"&filter.Status={(int)ErrorLogStatus.New}");
            var byStatusResolved = await GetErrorLogsAsync(project.PublicId, $"&filter.Status={(int)ErrorLogStatus.Resolved}");

            //assert
            Assert.Equal(1, byQuery.Count);
            Assert.Equal("System.Net.Sockets.SocketException", Assert.Single(byQuery.Items).ErrorClass);
            Assert.Equal(2, byStatusNew.Count);
            Assert.Equal(0, byStatusResolved.Count);
        }

        [Fact]
        public async Task ResolveErrorLog_MarksItAsResolvedAndTheNextSyncReopensIt()
        {
            //arrange
            var project = await CreateProjectAsync();

            IntegrationTestsFixture.NewRelicErrorProvider.SetEvents(project.NewRelicId,
                ErrorLogFaker.GetNewRelicErrorEvent("System.IO.IOException", "Disk full"));

            await AuthenticatedHttpClient.PostAsync($"/Projects/{project.PublicId}/Errors/Sync", null);

            var errorLog = Assert.Single((await GetErrorLogsAsync(project.PublicId)).Items);

            //act
            var httpResponseResolve = await AuthenticatedHttpClient.PatchAsync($"/ErrorLogs/{errorLog.PublicId}/Resolve", null);
            var httpResponseResolveAgain = await AuthenticatedHttpClient.PatchAsync($"/ErrorLogs/{errorLog.PublicId}/Resolve", null);

            var resolved = await GetResultContentAsync<ErrorLogGetByIdResponse>(await AuthenticatedHttpClient.GetAsync($"/ErrorLogs/{errorLog.PublicId}"));

            await AuthenticatedHttpClient.PostAsync($"/Projects/{project.PublicId}/Errors/Sync", null);

            var reopened = await GetResultContentAsync<ErrorLogGetByIdResponse>(await AuthenticatedHttpClient.GetAsync($"/ErrorLogs/{errorLog.PublicId}"));

            //assert
            Assert.Equal(HttpStatusCode.Accepted, httpResponseResolve.StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, httpResponseResolveAgain.StatusCode);

            Assert.Equal(ErrorLogStatus.Resolved, resolved.Status);
            Assert.NotNull(resolved.ResolvedAt);

            Assert.Equal(ErrorLogStatus.Reopened, reopened.Status);
            Assert.Equal(2, reopened.Occurrences);
        }

        [Fact]
        public async Task ResolveErrorLog_WithInvalidId_ReturnsNotFound()
        {
            //arrange & act
            var httpResponse = await AuthenticatedHttpClient.PatchAsync($"/ErrorLogs/{Guid.NewGuid()}/Resolve", null);

            //assert
            Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteProject_RemovesItsKnowledgeBase()
        {
            //arrange
            var project = await CreateProjectAsync();

            IntegrationTestsFixture.NewRelicErrorProvider.SetEvents(project.NewRelicId, ErrorLogFaker.GetNewRelicErrorEvent());

            await AuthenticatedHttpClient.PostAsync($"/Projects/{project.PublicId}/Errors/Sync", null);

            Assert.Equal(1, (await GetErrorLogsAsync(project.PublicId)).Count);

            //act
            var httpResponseDelete = await AuthenticatedHttpClient.DeleteAsync($"/Projects/{project.PublicId}");

            //assert
            Assert.Equal(HttpStatusCode.NoContent, httpResponseDelete.StatusCode);
            Assert.Equal(0, (await GetErrorLogsAsync(project.PublicId)).Count);
        }

        private async Task<ProjectCreateResponse> CreateProjectAsync()
        {
            var httpResponse = await AuthenticatedHttpClient.PostAsync("/Projects", GetStringContent(ProjectFaker.GetProjectCreateRequest()));

            Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

            return await GetResultContentAsync<ProjectCreateResponse>(httpResponse);
        }

        private async Task<PagedList<ErrorLogGetPaginatedResponse>> GetErrorLogsAsync(string projectId, string extraQuery = "")
        {
            var httpResponse = await AuthenticatedHttpClient.GetAsync($"/ErrorLogs?filter.ProjectId={projectId}{extraQuery}");

            Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);

            return await GetResultContentAsync<PagedList<ErrorLogGetPaginatedResponse>>(httpResponse);
        }
    }
}
