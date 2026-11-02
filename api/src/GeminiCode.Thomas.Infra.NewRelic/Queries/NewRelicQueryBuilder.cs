using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace GeminiCode.Thomas.Infra.NewRelic.Queries;

/// <summary>
/// Builds the NRQL and NerdGraph (GraphQL) queries used to collect the errors of an application.
/// </summary>
public static class NewRelicQueryBuilder
{
    public const string TimestampAttribute = "timestamp";
    public const string ErrorClassAttribute = "error.class";
    public const string ErrorMessageAttribute = "error.message";
    public const string TransactionNameAttribute = "transactionName";
    public const string StackTraceAttribute = "stack_trace";
    public const string ErrorGroupIdAttribute = "error.group.guid";

    private static readonly JsonSerializerOptions GraphQlStringOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>
    /// Builds the NRQL that returns the error occurrences (TransactionError events) of an application.
    /// The application can be identified by its entity GUID or by its numeric APM application id.
    /// </summary>
    public static string BuildTransactionErrorsQuery(string newRelicId, DateTime since, DateTime until, int maxResults, bool includeExpectedErrors = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newRelicId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxResults);

        var applicationFilter = long.TryParse(newRelicId.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var applicationId)
            ? $"appId = {applicationId}"
            : $"entityGuid = '{EscapeNrqlString(newRelicId.Trim())}'";

        var expectedErrorsFilter = includeExpectedErrors
            ? string.Empty
            : " AND (error.expected IS NULL OR error.expected = false)";

        return $"SELECT {TimestampAttribute}, {ErrorClassAttribute}, {ErrorMessageAttribute}, {TransactionNameAttribute}, {StackTraceAttribute}, {ErrorGroupIdAttribute} " +
               $"FROM TransactionError WHERE {applicationFilter}{expectedErrorsFilter} " +
               $"SINCE {ToEpochMilliseconds(since)} UNTIL {ToEpochMilliseconds(until)} LIMIT {maxResults}";
    }

    /// <summary>
    /// Wraps the NRQL in the NerdGraph query that executes it on the given account.
    /// </summary>
    public static string BuildNerdGraphQuery(long accountId, string nrql, int timeoutSeconds = 30)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nrql);

        return $"{{ actor {{ account(id: {accountId}) {{ nrql(query: {JsonSerializer.Serialize(nrql, GraphQlStringOptions)}, timeout: {timeoutSeconds}) {{ results }} }} }} }}";
    }

    public static long ToEpochMilliseconds(DateTime dateTime)
    {
        var utc = dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime();

        return new DateTimeOffset(utc).ToUnixTimeMilliseconds();
    }

    private static string EscapeNrqlString(string value)
    {
        return value.Replace("\\", "\\\\").Replace("'", "\\'");
    }
}
