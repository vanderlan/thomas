using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncAllProjectsErrors;

public class SyncAllProjectsErrorsResponse
{
    public IList<SyncProjectErrorsResponse> SynchronizedProjects { get; } = [];

    /// <summary>
    /// The ids of the projects whose synchronization failed. The failures are logged and do not stop the other projects.
    /// </summary>
    public IList<string> FailedProjects { get; } = [];
}
