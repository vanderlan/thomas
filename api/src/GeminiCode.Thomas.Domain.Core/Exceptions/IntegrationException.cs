using System;

namespace GeminiCode.Thomas.Domain.Core.Exceptions;

/// <summary>
/// Thrown when an external integration (NewRelic, GitHub...) cannot be reached, rejects the
/// request or returns an unexpected payload. It is mapped to 502 (Bad Gateway) by the
/// exception middleware, since the failure belongs to an upstream service and not to the request.
/// </summary>
[Serializable]
public class IntegrationException : BusinessException
{
    public const string DefaultTitle = "Integration Error";

    public IntegrationException(string message, string title) : base(message, title)
    {
    }

    public IntegrationException(string message) : base(message, DefaultTitle)
    {
    }

    public IntegrationException(string message, Exception innerException) : base(message, innerException)
    {
        Title = DefaultTitle;
    }

    public IntegrationException(string message, string title, Exception innerException) : base(message, innerException)
    {
        Title = title;
    }
}
