using System;
using System.Net;
using System.Threading.Tasks;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetById;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetPaginated;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using GeminiCode.Thomas.Test.Integration.Setup;
using GeminiCode.Thomas.Test.Shared.Faker;
using Newtonsoft.Json.Linq;
using Xunit;

namespace GeminiCode.Thomas.Test.Api.V1
{
    public class ProjectsApiTest : IntegrationTestsBootstrapper
    {
        public ProjectsApiTest(IntegrationTestsFixture fixture) : base(fixture)
        {
            LoginWithDefaultUser();
        }

        [Fact]
        public async Task GetProjects_WithoutAuthentication_ReturnsUnauthorized()
        {
            //arrange & act
            var httpResponse = await HttpClient.GetAsync("/Projects");

            //assert
            Assert.Equal(HttpStatusCode.Unauthorized, httpResponse.StatusCode);
        }

        [Fact]
        public async Task PostProject_WithValidData_CreatesAProjectWithoutExposingTheKeys()
        {
            //arrange
            var request = ProjectFaker.GetProjectCreateRequest();

            //act
            var httpResponse = await AuthenticatedHttpClient.PostAsync("/Projects", GetStringContent(request));

            var content = await httpResponse.Content.ReadAsStringAsync();
            var projectCreated = JObject.Parse(content).ToObject<ProjectCreateResponse>();

            //assert
            Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

            Assert.NotNull(projectCreated);
            Assert.Equal(request.Name, projectCreated.Name);
            Assert.Equal(request.Description, projectCreated.Description);
            Assert.Equal(request.NewRelicId, projectCreated.NewRelicId);
            Assert.Equal(request.GitHubRepository, projectCreated.GitHubRepository);
            Assert.Equal(request.PriorityBranch, projectCreated.PriorityBranch);
            Assert.Null(projectCreated.LastErrorSyncAt);
            Assert.True(projectCreated.CreatedAt > DateTime.MinValue);
            Assert.NotNull(projectCreated.PublicId);

            //the GitHub keys are write-only
            Assert.DoesNotContain(request.GitHubKeys.AccessToken, content);
            Assert.DoesNotContain("gitHubKeys", content, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PostProject_WithTheSameName_ReturnsConflict()
        {
            //arrange
            var request = ProjectFaker.GetProjectCreateRequest();
            await CreateProjectAsync(request);

            var duplicated = ProjectFaker.GetProjectCreateRequest();
            duplicated.Name = request.Name;

            //act
            var httpResponse = await AuthenticatedHttpClient.PostAsync("/Projects", GetStringContent(duplicated));

            //assert
            Assert.Equal(HttpStatusCode.Conflict, httpResponse.StatusCode);
        }

        [Theory]
        [InlineData("", "guid", "owner/repo", "token", "main")]
        [InlineData("Name", "", "owner/repo", "token", "main")]
        [InlineData("Name", "guid", "", "token", "main")]
        [InlineData("Name", "guid", "https://github.com/owner/repo", "token", "main")]
        [InlineData("Name", "guid", "owner/repo", "", "main")]
        [InlineData("Name", "guid", "owner/repo", "token", "")]
        public async Task PostProject_WithInvalidData_ReturnsBadRequest(string name, string newRelicId, string repository, string accessToken, string branch)
        {
            //arrange
            var request = new ProjectCreateRequest
            {
                Name = name,
                NewRelicId = newRelicId,
                GitHubRepository = repository,
                GitHubKeys = new GitHubKeys { AccessToken = accessToken },
                PriorityBranch = branch
            };

            //act
            var httpResponse = await AuthenticatedHttpClient.PostAsync("/Projects", GetStringContent(request));

            //assert
            Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        }

        [Fact]
        public async Task PostProject_WithoutKeys_ReturnsBadRequest()
        {
            //arrange
            var request = ProjectFaker.GetProjectCreateRequest();
            request.GitHubKeys = null;

            //act
            var httpResponse = await AuthenticatedHttpClient.PostAsync("/Projects", GetStringContent(request));

            //assert
            Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        }

        [Fact]
        public async Task GetProjectById_WithValidId_ReturnsTheProject()
        {
            //arrange
            var projectCreated = await CreateProjectAsync();

            //act
            var httpResponse = await AuthenticatedHttpClient.GetAsync($"/Projects/{projectCreated.PublicId}");

            var project = await GetResultContentAsync<ProjectGetByIdResponse>(httpResponse);

            //assert
            Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
            Assert.Equal(projectCreated.PublicId, project.PublicId);
            Assert.Equal(projectCreated.Name, project.Name);
            Assert.True(project.HasGitHubKeys);
        }

        [Fact]
        public async Task GetProjectById_WithInvalidId_ReturnsNotFound()
        {
            //arrange & act
            var httpResponse = await AuthenticatedHttpClient.GetAsync($"/Projects/{Guid.NewGuid()}");

            //assert
            Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        }

        [Fact]
        public async Task GetProjectsPaginated_WithNameFilter_ReturnsTheProject()
        {
            //arrange
            var projectCreated = await CreateProjectAsync();

            //act
            var httpResponse = await AuthenticatedHttpClient.GetAsync($"/Projects?filter.Query={Uri.EscapeDataString(projectCreated.Name)}");

            var pagedList = await GetResultContentAsync<PagedList<ProjectGetPaginatedResponse>>(httpResponse);

            //assert
            Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
            Assert.Equal(1, pagedList.Count);
            Assert.Contains(pagedList.Items, x => x.PublicId == projectCreated.PublicId);
        }

        [Fact]
        public async Task PutProject_WithValidData_UpdatesTheProject()
        {
            //arrange
            var projectCreated = await CreateProjectAsync();
            var updateRequest = ProjectFaker.GetProjectUpdateRequest();

            //act
            var httpResponsePut = await AuthenticatedHttpClient.PutAsync($"/Projects/{projectCreated.PublicId}", GetStringContent(updateRequest));

            Assert.Equal(HttpStatusCode.Accepted, httpResponsePut.StatusCode);

            var httpResponseGet = await AuthenticatedHttpClient.GetAsync($"/Projects/{projectCreated.PublicId}");
            var project = await GetResultContentAsync<ProjectGetByIdResponse>(httpResponseGet);

            //assert
            Assert.Equal(updateRequest.Name, project.Name);
            Assert.Equal(updateRequest.Description, project.Description);
            Assert.Equal(updateRequest.NewRelicId, project.NewRelicId);
            Assert.Equal(updateRequest.GitHubRepository, project.GitHubRepository);
            Assert.Equal(updateRequest.PriorityBranch, project.PriorityBranch);
            Assert.True(project.LastUpdated > projectCreated.LastUpdated);
        }

        [Fact]
        public async Task PutProject_WithoutKeys_KeepsTheCurrentKeys()
        {
            //arrange
            var projectCreated = await CreateProjectAsync();
            var updateRequest = ProjectFaker.GetProjectUpdateRequest();
            updateRequest.GitHubKeys = null;

            //act
            var httpResponsePut = await AuthenticatedHttpClient.PutAsync($"/Projects/{projectCreated.PublicId}", GetStringContent(updateRequest));

            var httpResponseGet = await AuthenticatedHttpClient.GetAsync($"/Projects/{projectCreated.PublicId}");
            var project = await GetResultContentAsync<ProjectGetByIdResponse>(httpResponseGet);

            //assert
            Assert.Equal(HttpStatusCode.Accepted, httpResponsePut.StatusCode);
            Assert.Equal(updateRequest.Name, project.Name);
            Assert.True(project.HasGitHubKeys);
        }

        [Fact]
        public async Task PutProject_WithInvalidId_ReturnsNotFound()
        {
            //arrange & act
            var httpResponse = await AuthenticatedHttpClient.PutAsync($"/Projects/{Guid.NewGuid()}", GetStringContent(ProjectFaker.GetProjectUpdateRequest()));

            //assert
            Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        }

        [Fact]
        public async Task DeleteProject_WithValidId_DeletesTheProject()
        {
            //arrange
            var projectCreated = await CreateProjectAsync();

            //act
            var httpResponseDelete = await AuthenticatedHttpClient.DeleteAsync($"/Projects/{projectCreated.PublicId}");

            Assert.Equal(HttpStatusCode.NoContent, httpResponseDelete.StatusCode);

            var httpResponseGet = await AuthenticatedHttpClient.GetAsync($"/Projects/{projectCreated.PublicId}");

            //assert
            Assert.Equal(HttpStatusCode.NotFound, httpResponseGet.StatusCode);
        }

        [Fact]
        public async Task DeleteProject_WithInvalidId_ReturnsNotFound()
        {
            //arrange & act
            var httpResponse = await AuthenticatedHttpClient.DeleteAsync($"/Projects/{Guid.NewGuid()}");

            //assert
            Assert.Equal(HttpStatusCode.NotFound, httpResponse.StatusCode);
        }

        private async Task<ProjectCreateResponse> CreateProjectAsync(ProjectCreateRequest request = null)
        {
            request ??= ProjectFaker.GetProjectCreateRequest();

            var httpResponse = await AuthenticatedHttpClient.PostAsync("/Projects", GetStringContent(request));

            Assert.Equal(HttpStatusCode.Created, httpResponse.StatusCode);

            return await GetResultContentAsync<ProjectCreateResponse>(httpResponse);
        }
    }
}
