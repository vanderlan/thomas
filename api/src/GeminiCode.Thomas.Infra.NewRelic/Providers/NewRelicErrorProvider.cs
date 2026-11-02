using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using GeminiCode.Thomas.Domain.Core.Integrations.NewRelic;
using GeminiCode.Thomas.Infra.NewRelic.Configuration;
using GeminiCode.Thomas.Infra.NewRelic.Exceptions;
using GeminiCode.Thomas.Infra.NewRelic.Queries;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeminiCode.Thomas.Infra.NewRelic.Providers;

/// <summary>
/// Collects the errors of an application through the NewRelic NerdGraph API, running a NRQL
/// query over the TransactionError events.
/// </summary>
public sealed class NewRelicErrorProvider(
    HttpClient httpClient,
    IOptions<NewRelicOptions> options,
    ILogger<NewRelicErrorProvider> logger) : INewRelicErrorProvider
{
    public const string ApiKeyHeader = "API-Key";

    public async Task<IReadOnlyList<NewRelicErrorEvent>> GetErrorsAsync(string newRelicId, DateTime since, DateTime until, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(newRelicId);

        var settings = options.Value;

        ValidateSettings(settings);

        var nrql = NewRelicQueryBuilder.BuildTransactionErrorsQuery(newRelicId, since, until, settings.MaxResults, settings.IncludeExpectedErrors);
        var graphQlQuery = NewRelicQueryBuilder.BuildNerdGraphQuery(settings.AccountId, nrql, settings.TimeoutSeconds);

        logger.LogDebug("Querying NewRelic errors with the NRQL: {Nrql}", nrql);

        using var request = new HttpRequestMessage(HttpMethod.Post, settings.GraphQlEndpoint);
        request.Headers.TryAddWithoutValidation(ApiKeyHeader, settings.ApiKey);
        request.Content = JsonContent.Create(new { query = graphQlQuery });

        string body;
        HttpResponseMessage response;

        try
        {
            response = await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            throw new NewRelicApiException($"It was not possible to reach the NewRelic API: {exception.Message}", exception);
        }

        using (response)
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new NewRelicApiException($"The NewRelic API returned the status code {(int)response.StatusCode} ({response.StatusCode}). Response: {body}");
            }
        }

        var events = ParseResults(body);

        logger.LogInformation("NewRelic returned {EventsCount} error occurrence(s) for the application {NewRelicId} between {Since} and {Until}",
            events.Count, newRelicId, since, until);

        return events;
    }

    private static void ValidateSettings(NewRelicOptions settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new NewRelicApiException($"The NewRelic API key is not configured. Set it on the '{NewRelicOptions.SectionName}:{nameof(NewRelicOptions.ApiKey)}' configuration.");

        if (settings.AccountId <= 0)
            throw new NewRelicApiException($"The NewRelic account id is not configured. Set it on the '{NewRelicOptions.SectionName}:{nameof(NewRelicOptions.AccountId)}' configuration.");

        if (string.IsNullOrWhiteSpace(settings.GraphQlEndpoint))
            throw new NewRelicApiException($"The NewRelic GraphQL endpoint is not configured. Set it on the '{NewRelicOptions.SectionName}:{nameof(NewRelicOptions.GraphQlEndpoint)}' configuration.");
    }

    /// <summary>
    /// Parses the NerdGraph response (data.actor.account.nrql.results) into error events.
    /// </summary>
    internal static IReadOnlyList<NewRelicErrorEvent> ParseResults(string body)
    {
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException exception)
        {
            throw new NewRelicApiException("The NewRelic API returned an invalid JSON response.", exception);
        }

        using (document)
        {
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array && errors.GetArrayLength() > 0)
            {
                var messages = errors.EnumerateArray()
                    .Select(error => error.TryGetProperty("message", out var message) ? message.GetString() : error.GetRawText());

                throw new NewRelicApiException($"The NewRelic API returned errors: {string.Join("; ", messages)}");
            }

            if (!TryGetPath(root, out var results, "data", "actor", "account", "nrql", "results") || results.ValueKind != JsonValueKind.Array)
            {
                throw new NewRelicApiException("The NewRelic API returned an unexpected response: the NRQL results were not found.");
            }

            return results.EnumerateArray()
                .Where(result => result.ValueKind == JsonValueKind.Object)
                .Select(ParseEvent)
                .ToList();
        }
    }

    private static NewRelicErrorEvent ParseEvent(JsonElement result)
    {
        return new NewRelicErrorEvent
        {
            Timestamp = GetTimestamp(result),
            ErrorClass = GetString(result, NewRelicQueryBuilder.ErrorClassAttribute),
            Message = GetString(result, NewRelicQueryBuilder.ErrorMessageAttribute),
            TransactionName = GetString(result, NewRelicQueryBuilder.TransactionNameAttribute),
            StackTrace = GetString(result, NewRelicQueryBuilder.StackTraceAttribute),
            ErrorGroupId = GetString(result, NewRelicQueryBuilder.ErrorGroupIdAttribute)
        };
    }

    private static DateTime GetTimestamp(JsonElement result)
    {
        if (!result.TryGetProperty(NewRelicQueryBuilder.TimestampAttribute, out var timestamp))
            return DateTime.UtcNow;

        return timestamp.ValueKind switch
        {
            JsonValueKind.Number when timestamp.TryGetInt64(out var epochMilliseconds) => DateTimeOffset.FromUnixTimeMilliseconds(epochMilliseconds).UtcDateTime,
            JsonValueKind.Number => DateTimeOffset.FromUnixTimeMilliseconds((long)timestamp.GetDouble()).UtcDateTime,
            JsonValueKind.String when DateTime.TryParse(timestamp.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var parsed) => parsed,
            _ => DateTime.UtcNow
        };
    }

    /// <summary>
    /// Reads an attribute as string. Arrays (e.g. stack traces returned as a list of frames) are joined by line breaks.
    /// </summary>
    private static string GetString(JsonElement result, string attribute)
    {
        if (!result.TryGetProperty(attribute, out var value))
            return null;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number or JsonValueKind.True or JsonValueKind.False => value.GetRawText(),
            JsonValueKind.Array => string.Join(Environment.NewLine, value.EnumerateArray().Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() : item.GetRawText())),
            JsonValueKind.Object => value.GetRawText(),
            _ => null
        };
    }

    private static bool TryGetPath(JsonElement element, out JsonElement found, params string[] path)
    {
        var current = element;

        foreach (var segment in path)
        {
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(segment, out var next))
            {
                found = default;
                return false;
            }

            current = next;
        }

        found = current;
        return true;
    }
}
