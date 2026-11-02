using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Logging;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncAllProjectsErrors;

public class SyncAllProjectsErrorsRequestHandler(
    IUnitOfWork unitOfWork,
    IMediator mediator,
    ILogger<SyncAllProjectsErrorsRequestHandler> logger) : IRequestHandler<SyncAllProjectsErrorsRequest, SyncAllProjectsErrorsResponse>
{
    public async Task<SyncAllProjectsErrorsResponse> Handle(SyncAllProjectsErrorsRequest request, CancellationToken cancellationToken)
    {
        var response = new SyncAllProjectsErrorsResponse();

        var projects = (await unitOfWork.ProjectRepository.ListAllAsync()).ToList();

        logger.LogInformation("Starting the errors synchronization of {ProjectsCount} project(s)", projects.Count);

        foreach (var project in projects)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                //Every project is synchronized by its own request (and unit of work), so a failure does not affect the others.
                response.SynchronizedProjects.Add(await mediator.Send(new SyncProjectErrorsRequest(project.PublicId), cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error when synchronizing the errors of the project {ProjectName} ({ProjectId}). Error message: {ErrorMessage}",
                    project.Name, project.PublicId, exception.Message);

                response.FailedProjects.Add(project.PublicId);
            }
        }

        logger.LogInformation("The errors synchronization finished: {SynchronizedCount} project(s) synchronized and {FailedCount} failure(s)",
            response.SynchronizedProjects.Count, response.FailedProjects.Count);

        return response;
    }
}
