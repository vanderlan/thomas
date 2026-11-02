using GeminiCode.Thomas.Domain.Core.Exceptions;

namespace GeminiCode.Thomas.Infra.NewRelic.Exceptions;

/// <summary>
/// Thrown when the NewRelic API cannot be reached, rejects the request or returns an unexpected payload.
/// Being an <see cref="IntegrationException"/>, the API answers it with 502 (Bad Gateway).
/// </summary>
[Serializable]
public class NewRelicApiException : IntegrationException
{
    public const string NewRelicTitle = "NewRelic Integration Error";

    public NewRelicApiException(string message) : base(message, NewRelicTitle)
    {
    }

    public NewRelicApiException(string message, Exception innerException) : base(message, NewRelicTitle, innerException)
    {
    }
}
