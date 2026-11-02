using GeminiCode.Thomas.Domain.Core.Services;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Domain.Services;

public class ErrorFingerprintGeneratorTests
{
    [Fact]
    public void Compute_WithTheSameClassAndMessage_ReturnsTheSameFingerprint()
    {
        //arrange & act
        var first = ErrorFingerprintGenerator.Compute("System.NullReferenceException", "Object reference not set to an instance of an object.");
        var second = ErrorFingerprintGenerator.Compute("System.NullReferenceException", "Object reference not set to an instance of an object.");

        //assert
        Assert.Equal(first, second);
        Assert.Equal(128, first.Length);
    }

    [Fact]
    public void Compute_WithDifferentClasses_ReturnsDifferentFingerprints()
    {
        //arrange & act
        var first = ErrorFingerprintGenerator.Compute("System.NullReferenceException", "Something failed");
        var second = ErrorFingerprintGenerator.Compute("System.InvalidOperationException", "Something failed");

        //assert
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Compute_WithDifferentMessages_ReturnsDifferentFingerprints()
    {
        //arrange & act
        var first = ErrorFingerprintGenerator.Compute("System.InvalidOperationException", "The user was not found");
        var second = ErrorFingerprintGenerator.Compute("System.InvalidOperationException", "The order was not found");

        //assert
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("Order 123 not found", "Order 456 not found")]
    [InlineData("User 9f8c6b2a-5d1e-4c3b-9a7f-1e2d3c4b5a60 not found", "User 1a2b3c4d-5e6f-4a7b-8c9d-0e1f2a3b4c5d not found")]
    [InlineData("Pointer 0x7FFE1234 is invalid", "Pointer 0xDEADBEEF is invalid")]
    [InlineData("Timeout   after\t30 seconds", "Timeout after 60 seconds")]
    [InlineData("CONNECTION refused", "connection refused")]
    [InlineData("  padded message  ", "padded message")]
    public void Compute_IgnoresTheVolatilePartsOfTheMessage(string firstMessage, string secondMessage)
    {
        //arrange & act
        var first = ErrorFingerprintGenerator.Compute("System.Exception", firstMessage);
        var second = ErrorFingerprintGenerator.Compute("System.Exception", secondMessage);

        //assert
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Compute_WithABlankClass_UsesTheUnknownErrorClass(string errorClass)
    {
        //arrange & act
        var fingerprint = ErrorFingerprintGenerator.Compute(errorClass, "message");

        //assert
        Assert.Equal(ErrorFingerprintGenerator.Compute(ErrorFingerprintGenerator.UnknownErrorClass, "message"), fingerprint);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Compute_WithABlankMessage_DoesNotThrow(string message)
    {
        //arrange & act
        var fingerprint = ErrorFingerprintGenerator.Compute("System.Exception", message);

        //assert
        Assert.Equal(ErrorFingerprintGenerator.Compute("System.Exception", null), fingerprint);
    }

    [Fact]
    public void NormalizeMessage_ReplacesGuidsHexadecimalsAndNumbers()
    {
        //arrange & act
        var normalized = ErrorFingerprintGenerator.NormalizeMessage("Item 9f8c6b2a-5d1e-4c3b-9a7f-1e2d3c4b5a60 at 0xFF failed 3 times");

        //assert
        Assert.Equal("item {guid} at {hex} failed # times", normalized);
    }
}
