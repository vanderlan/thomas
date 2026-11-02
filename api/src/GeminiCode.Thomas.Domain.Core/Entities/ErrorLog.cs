using System;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;

namespace GeminiCode.Thomas.Domain.Core.Entities;

/// <summary>
/// An entry of the errors knowledge base. Every entry groups all the occurrences of the same
/// error (same <see cref="Fingerprint"/>) of a project, keeping its history up to date
/// so it can be used later to decide whether a Pull Request must be created to fix it.
/// </summary>
public class ErrorLog : BaseEntity
{
    public required string ProjectId { get; set; }

    /// <summary>
    /// The hash that identifies the error, computed from the error class and the normalized message.
    /// </summary>
    public required string Fingerprint { get; set; }

    public required string ErrorClass { get; set; }

    /// <summary>
    /// The message of the most recent occurrence.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// The stack trace of the most recent occurrence.
    /// </summary>
    public string StackTrace { get; set; }

    /// <summary>
    /// The transaction (endpoint, job...) of the most recent occurrence.
    /// </summary>
    public string TransactionName { get; set; }

    /// <summary>
    /// The identifier of the error group on NewRelic (Errors Inbox), when available.
    /// </summary>
    public string NewRelicErrorGroupId { get; set; }

    public ErrorLogStatus Status { get; set; } = ErrorLogStatus.New;

    /// <summary>
    /// The total amount of occurrences collected from NewRelic.
    /// </summary>
    public int Occurrences { get; set; }

    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    /// <summary>
    /// Registers new occurrences of the error collected by the sync job, updating the
    /// counters, the most recent sample and the status of the entry.
    /// </summary>
    public void RegisterOccurrences(int occurrences, DateTime seenAt, string message, string stackTrace, string transactionName, string newRelicErrorGroupId = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(occurrences);

        Occurrences += occurrences;

        if (seenAt >= LastSeenAt)
        {
            LastSeenAt = seenAt;
            Message = message;
            StackTrace = stackTrace;
            TransactionName = transactionName;
            NewRelicErrorGroupId = newRelicErrorGroupId ?? NewRelicErrorGroupId;
        }

        if (seenAt < FirstSeenAt || FirstSeenAt == default)
        {
            FirstSeenAt = seenAt;
        }

        Status = Status is ErrorLogStatus.Resolved or ErrorLogStatus.Reopened
            ? ErrorLogStatus.Reopened
            : ErrorLogStatus.Recurring;
    }

    public void Resolve(DateTime resolvedAt)
    {
        Status = ErrorLogStatus.Resolved;
        ResolvedAt = resolvedAt;
    }
}
