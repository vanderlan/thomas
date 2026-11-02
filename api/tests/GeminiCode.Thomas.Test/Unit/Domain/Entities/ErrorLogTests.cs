using System;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;
using GeminiCode.Thomas.Test.Shared.Faker;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Domain.Entities;

public class ErrorLogTests
{
    [Fact]
    public void RegisterOccurrences_IncrementsTheCounterAndUpdatesTheLatestSample()
    {
        //arrange
        var errorLog = ErrorLogFaker.Get();
        errorLog.Occurrences = 3;
        var seenAt = errorLog.LastSeenAt.AddMinutes(5);

        //act
        errorLog.RegisterOccurrences(2, seenAt, "new message", "new stack trace", "new transaction", "group-id");

        //assert
        Assert.Equal(5, errorLog.Occurrences);
        Assert.Equal(seenAt, errorLog.LastSeenAt);
        Assert.Equal("new message", errorLog.Message);
        Assert.Equal("new stack trace", errorLog.StackTrace);
        Assert.Equal("new transaction", errorLog.TransactionName);
        Assert.Equal("group-id", errorLog.NewRelicErrorGroupId);
        Assert.Equal(ErrorLogStatus.Recurring, errorLog.Status);
    }

    [Fact]
    public void RegisterOccurrences_WithAnOlderOccurrence_KeepsTheLatestSampleAndMovesFirstSeenBack()
    {
        //arrange
        var errorLog = ErrorLogFaker.Get();
        var originalMessage = errorLog.Message;
        var originalLastSeenAt = errorLog.LastSeenAt;
        var olderThanFirst = errorLog.FirstSeenAt.AddHours(-1);

        //act
        errorLog.RegisterOccurrences(1, olderThanFirst, "old message", "old stack trace", "old transaction");

        //assert
        Assert.Equal(originalMessage, errorLog.Message);
        Assert.Equal(originalLastSeenAt, errorLog.LastSeenAt);
        Assert.Equal(olderThanFirst, errorLog.FirstSeenAt);
    }

    [Theory]
    [InlineData(ErrorLogStatus.New, ErrorLogStatus.Recurring)]
    [InlineData(ErrorLogStatus.Recurring, ErrorLogStatus.Recurring)]
    [InlineData(ErrorLogStatus.Resolved, ErrorLogStatus.Reopened)]
    [InlineData(ErrorLogStatus.Reopened, ErrorLogStatus.Reopened)]
    public void RegisterOccurrences_UpdatesTheStatus(ErrorLogStatus current, ErrorLogStatus expected)
    {
        //arrange
        var errorLog = ErrorLogFaker.Get();
        errorLog.Status = current;

        //act
        errorLog.RegisterOccurrences(1, DateTime.UtcNow, "message", null, null);

        //assert
        Assert.Equal(expected, errorLog.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RegisterOccurrences_WithoutOccurrences_ThrowsArgumentOutOfRangeException(int occurrences)
    {
        //arrange
        var errorLog = ErrorLogFaker.Get();

        //act & assert
        Assert.Throws<ArgumentOutOfRangeException>(() => errorLog.RegisterOccurrences(occurrences, DateTime.UtcNow, "message", null, null));
    }

    [Fact]
    public void Resolve_MarksTheErrorAsResolved()
    {
        //arrange
        var errorLog = ErrorLogFaker.Get();
        var resolvedAt = new DateTime(2025, 3, 4, 5, 6, 7, DateTimeKind.Utc);

        //act
        errorLog.Resolve(resolvedAt);

        //assert
        Assert.Equal(ErrorLogStatus.Resolved, errorLog.Status);
        Assert.Equal(resolvedAt, errorLog.ResolvedAt);
    }
}
