using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Notifications.ProjectErrorsSynchronized;

/// <summary>
/// Published after the knowledge base of a project is updated by the sync job.
/// </summary>
public class ProjectErrorsSynchronizedNotification(SyncProjectErrorsResponse result) : INotification
{
    public SyncProjectErrorsResponse Result { get; set; } = result;
}
