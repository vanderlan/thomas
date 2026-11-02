namespace GeminiCode.Thomas.Application.Core.Configuration;

/// <summary>
/// Section of appsettings that configures the job that collects the errors of the projects from NewRelic.
/// </summary>
public class ErrorLogSyncOptions
{
    public const string SectionName = "ErrorLogSync";

    /// <summary>
    /// Enables or disables the background job. The synchronization can still be triggered manually through the API.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// The interval between two executions of the job.
    /// </summary>
    public int IntervalMinutes { get; set; } = 5;

    /// <summary>
    /// The size of the window collected when a project is synchronized for the first time.
    /// After that, every execution collects the errors that happened since the previous one.
    /// </summary>
    public int InitialLookbackMinutes { get; set; } = 5;
}
