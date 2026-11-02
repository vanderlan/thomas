namespace GeminiCode.Thomas.Infra.NewRelic.Configuration;

/// <summary>
/// Section of appsettings that contains the NewRelic (NerdGraph API) configuration.
/// </summary>
public class NewRelicOptions
{
    public const string SectionName = "NewRelic";
    public const string DefaultGraphQlEndpoint = "https://api.newrelic.com/graphql";

    /// <summary>
    /// A NewRelic User API key with permission to run NRQL queries on the account.
    /// </summary>
    public string ApiKey { get; set; }

    /// <summary>
    /// The NewRelic account that owns the monitored applications.
    /// </summary>
    public long AccountId { get; set; }

    /// <summary>
    /// The NerdGraph endpoint. Use https://api.eu.newrelic.com/graphql for EU accounts.
    /// </summary>
    public string GraphQlEndpoint { get; set; } = DefaultGraphQlEndpoint;

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// The maximum amount of error occurrences returned by a single query (NRQL allows up to 2000).
    /// </summary>
    public int MaxResults { get; set; } = 2000;

    /// <summary>
    /// When false (default), the errors marked as expected on NewRelic are ignored.
    /// </summary>
    public bool IncludeExpectedErrors { get; set; }
}
