using GeminiCode.Thomas.Application.Core.Configuration;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Notifications.ProjectErrorsSynchronized;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Integrations.NewRelic;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using GeminiCode.Thomas.Domain.Core.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;

/// <summary>
/// Collects the errors of a project from NewRelic since its last synchronization, groups them by
/// fingerprint and compares them with the knowledge base: known errors have their counters, last
/// sample and status updated, unknown errors are registered as new entries.
/// </summary>
public class SyncProjectErrorsRequestHandler(
    IUnitOfWork unitOfWork,
    INewRelicErrorProvider newRelicErrorProvider,
    IMediator mediator,
    IOptions<ErrorLogSyncOptions> options,
    TimeProvider timeProvider,
    ILogger<SyncProjectErrorsRequestHandler> logger) : IRequestHandler<SyncProjectErrorsRequest, SyncProjectErrorsResponse>
{
    public async Task<SyncProjectErrorsResponse> Handle(SyncProjectErrorsRequest request, CancellationToken cancellationToken)
    {
        var project = await unitOfWork.ProjectRepository.GetByIdAsync(request.ProjectId)
            ?? throw new NotFoundException(request.ProjectId);

        var (since, until) = GetSyncWindow(project);

        logger.LogInformation("Synchronizing the errors of the project {ProjectName} ({ProjectId}) from {Since} to {Until}",
            project.Name, project.PublicId, since, until);

        var events = await newRelicErrorProvider.GetErrorsAsync(project.NewRelicId, since, until, cancellationToken);

        var response = new SyncProjectErrorsResponse
        {
            ProjectId = project.PublicId,
            ProjectName = project.Name,
            Since = since,
            Until = until,
            EventsProcessed = events.Count
        };

        await UpdateKnowledgeBaseAsync(project, events, response);

        project.LastErrorSyncAt = until;
        unitOfWork.ProjectRepository.Update(project);

        await unitOfWork.CommitAsync();

        logger.LogInformation("The errors of the project {ProjectName} ({ProjectId}) were synchronized: {EventsProcessed} occurrences, {NewErrors} new errors and {UpdatedErrors} updated errors",
            project.Name, project.PublicId, response.EventsProcessed, response.NewErrors, response.UpdatedErrors);

        await mediator.Publish(new ProjectErrorsSynchronizedNotification(response), cancellationToken);

        return response;
    }

    /// <summary>
    /// Every execution collects the errors that happened since the previous one. The first
    /// execution of a project collects the configured initial lookback window.
    /// </summary>
    private (DateTime Since, DateTime Until) GetSyncWindow(Project project)
    {
        var until = TruncateToMilliseconds(timeProvider.GetUtcNow().UtcDateTime);

        var since = project.LastErrorSyncAt.HasValue
            ? DateTime.SpecifyKind(project.LastErrorSyncAt.Value, DateTimeKind.Utc)
            : until.AddMinutes(-options.Value.InitialLookbackMinutes);

        if (since > until)
            since = until;

        return (since, until);
    }

    /// <summary>
    /// The window bounds are persisted as BSON dates (millisecond precision), so they are truncated
    /// to guarantee that the next execution starts exactly where the previous one stopped.
    /// </summary>
    private static DateTime TruncateToMilliseconds(DateTime dateTime)
    {
        return new DateTime(dateTime.Ticks - (dateTime.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
    }

    private async Task UpdateKnowledgeBaseAsync(Project project, IReadOnlyList<NewRelicErrorEvent> events, SyncProjectErrorsResponse response)
    {
        var groups = events
            .GroupBy(errorEvent => ErrorFingerprintGenerator.Compute(errorEvent.ErrorClass, errorEvent.Message))
            .ToList();

        if (groups.Count == 0)
            return;

        var knownErrors = (await unitOfWork.ErrorLogRepository.FindByFingerprintsAsync(project.PublicId, groups.Select(group => group.Key)))
            .ToDictionary(errorLog => errorLog.Fingerprint);

        foreach (var group in groups)
        {
            var occurrences = group.OrderBy(errorEvent => errorEvent.Timestamp).ToList();
            var first = occurrences[0];
            var latest = occurrences[^1];

            if (knownErrors.TryGetValue(group.Key, out var errorLog))
            {
                errorLog.RegisterOccurrences(occurrences.Count, latest.Timestamp, latest.Message, latest.StackTrace, latest.TransactionName, latest.ErrorGroupId);

                unitOfWork.ErrorLogRepository.Update(errorLog);
                response.UpdatedErrors++;
            }
            else
            {
                await unitOfWork.ErrorLogRepository.AddAsync(new ErrorLog
                {
                    ProjectId = project.PublicId,
                    Fingerprint = group.Key,
                    ErrorClass = string.IsNullOrWhiteSpace(latest.ErrorClass) ? ErrorFingerprintGenerator.UnknownErrorClass : latest.ErrorClass.Trim(),
                    Message = latest.Message,
                    StackTrace = latest.StackTrace,
                    TransactionName = latest.TransactionName,
                    NewRelicErrorGroupId = latest.ErrorGroupId,
                    Status = ErrorLogStatus.New,
                    Occurrences = occurrences.Count,
                    FirstSeenAt = first.Timestamp,
                    LastSeenAt = latest.Timestamp
                });

                response.NewErrors++;
            }
        }
    }
}
