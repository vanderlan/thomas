using System;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Domain.Exceptions;

public class IntegrationExceptionTests
{
    [Fact]
    public void Constructor_WithMessageAndTitle_SetsProperties()
    {
        // Arrange
        var message = "Some message";
        var title = "Some title";

        // Act
        var exception = new IntegrationException(message, title);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(title, exception.Title);
        Assert.IsAssignableFrom<BusinessException>(exception);
    }

    [Fact]
    public void Constructor_WithMessage_SetsMessageAndTheDefaultTitle()
    {
        // Arrange
        var message = "The message test";

        // Act
        var exception = new IntegrationException(message);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(IntegrationException.DefaultTitle, exception.Title);
    }

    [Fact]
    public void Constructor_WithMessageAndInnerException_SetsProperties()
    {
        // Arrange
        var innerException = new Exception("Some message inner exception");
        var message = "Error message";

        // Act
        var exception = new IntegrationException(message, innerException);

        // Assert
        Assert.Equal(message, exception.Message);
        Assert.Equal(innerException, exception.InnerException);
        Assert.Equal(IntegrationException.DefaultTitle, exception.Title);
    }

    [Fact]
    public void Constructor_WithMessageTitleAndInnerException_SetsProperties()
    {
        // Arrange
        var innerException = new TimeoutException();

        // Act
        var exception = new IntegrationException("Error message", "GitHub Integration Error", innerException);

        // Assert
        Assert.Equal("Error message", exception.Message);
        Assert.Equal("GitHub Integration Error", exception.Title);
        Assert.Same(innerException, exception.InnerException);
    }
}
