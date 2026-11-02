using MediatR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Notifications.ProjectErrorsSynchronized;

/// <summary>
/// Extension point of the error log pipeline: the decision of creating a Pull Request to fix
/// the errors found (GitHub integration) will be plugged here in the future.
/// </summary>
public class ProjectErrorsSynchronizedNotificationHandler(ILogger<ProjectErrorsSynchronizedNotificationHandler> logger)
    : INotificationHandler<ProjectErrorsSynchronizedNotification>
{
    public async Task Handle(ProjectErrorsSynchronizedNotification notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("A notification {NotificationType} has been received. Notification details: {Notification}",
            nameof(ProjectErrorsSynchronizedNotification),
            JsonConvert.SerializeObject(notification, Formatting.Indented));

        await Task.CompletedTask;
    }
}
