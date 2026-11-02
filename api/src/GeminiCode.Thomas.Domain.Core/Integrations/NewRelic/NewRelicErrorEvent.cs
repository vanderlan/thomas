using System;

namespace GeminiCode.Thomas.Domain.Core.Integrations.NewRelic;

/// <summary>
/// A single error occurrence collected from NewRelic.
/// </summary>
public sealed class NewRelicErrorEvent
{
    public required DateTime Timestamp { get; init; }
    public required string ErrorClass { get; init; }
    public string Message { get; init; }
    public string TransactionName { get; init; }
    public string StackTrace { get; init; }
    public string ErrorGroupId { get; init; }
}
