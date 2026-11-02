namespace GeminiCode.Thomas.Domain.Core.Entities.Enuns;

public enum ErrorLogStatus
{
    /// <summary>
    /// The error was seen by the sync job for the first time.
    /// </summary>
    New = 1,

    /// <summary>
    /// The error was seen again by the sync job after its first registration.
    /// </summary>
    Recurring = 2,

    /// <summary>
    /// The error was marked as resolved.
    /// </summary>
    Resolved = 3,

    /// <summary>
    /// The error happened again after being marked as resolved.
    /// </summary>
    Reopened = 4
}
