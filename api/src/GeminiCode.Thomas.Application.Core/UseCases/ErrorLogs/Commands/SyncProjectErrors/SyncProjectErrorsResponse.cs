namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;

public class SyncProjectErrorsResponse
{
    public string ProjectId { get; set; }
    public string ProjectName { get; set; }

    /// <summary>
    /// The beginning (inclusive) of the NewRelic window that was collected.
    /// </summary>
    public DateTime Since { get; set; }

    /// <summary>
    /// The end (exclusive) of the NewRelic window that was collected.
    /// </summary>
    public DateTime Until { get; set; }

    /// <summary>
    /// The amount of error occurrences returned by NewRelic.
    /// </summary>
    public int EventsProcessed { get; set; }

    /// <summary>
    /// The amount of errors registered on the knowledge base for the first time.
    /// </summary>
    public int NewErrors { get; set; }

    /// <summary>
    /// The amount of errors that already existed on the knowledge base and were updated.
    /// </summary>
    public int UpdatedErrors { get; set; }
}
