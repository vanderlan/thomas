using System;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Infra.NewRelic.Exceptions;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Infra.NewRelic;

public class NewRelicApiExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_SetsTheNewRelicTitle()
    {
        // Act
        var exception = new NewRelicApiException("The NewRelic API returned errors");

        // Assert
        Assert.Equal("The NewRelic API returned errors", exception.Message);
        Assert.Equal(NewRelicApiException.NewRelicTitle, exception.Title);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsProperties()
    {
        // Arrange
        var innerException = new Exception("No such host is known");

        // Act
        var exception = new NewRelicApiException("It was not possible to reach the NewRelic API", innerException);

        // Assert
        Assert.Equal("It was not possible to reach the NewRelic API", exception.Message);
        Assert.Equal(NewRelicApiException.NewRelicTitle, exception.Title);
        Assert.Same(innerException, exception.InnerException);
    }

    [Fact]
    public void IsAnIntegrationException_SoTheMiddlewareAnswersBadGateway()
    {
        // Act
        var exception = new NewRelicApiException("failure");

        // Assert
        Assert.IsAssignableFrom<IntegrationException>(exception);
        Assert.IsAssignableFrom<BusinessException>(exception);
    }
}
