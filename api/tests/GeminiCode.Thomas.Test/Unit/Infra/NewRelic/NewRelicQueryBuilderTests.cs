using System;
using GeminiCode.Thomas.Infra.NewRelic.Queries;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Infra.NewRelic;

public class NewRelicQueryBuilderTests
{
    private static readonly DateTime Since = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Until = new(2025, 6, 1, 12, 5, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildTransactionErrorsQuery_WithAnEntityGuid_FiltersByEntityGuid()
    {
        //arrange & act
        var nrql = NewRelicQueryBuilder.BuildTransactionErrorsQuery("MTIzNDU2fEFQTXxBUFBMSUNBVElPTnw3ODk", Since, Until, 2000);

        //assert
        Assert.StartsWith("SELECT timestamp, error.class, error.message, transactionName, stack_trace, error.group.guid FROM TransactionError", nrql);
        Assert.Contains("WHERE entityGuid = 'MTIzNDU2fEFQTXxBUFBMSUNBVElPTnw3ODk'", nrql);
        Assert.Contains("error.expected IS NULL OR error.expected = false", nrql);
        Assert.EndsWith($"SINCE {NewRelicQueryBuilder.ToEpochMilliseconds(Since)} UNTIL {NewRelicQueryBuilder.ToEpochMilliseconds(Until)} LIMIT 2000", nrql);
    }

    [Fact]
    public void BuildTransactionErrorsQuery_WithANumericId_FiltersByApplicationId()
    {
        //arrange & act
        var nrql = NewRelicQueryBuilder.BuildTransactionErrorsQuery(" 123456 ", Since, Until, 500);

        //assert
        Assert.Contains("WHERE appId = 123456", nrql);
        Assert.DoesNotContain("entityGuid", nrql);
        Assert.EndsWith("LIMIT 500", nrql);
    }

    [Fact]
    public void BuildTransactionErrorsQuery_IncludingExpectedErrors_DoesNotFilterThem()
    {
        //arrange & act
        var nrql = NewRelicQueryBuilder.BuildTransactionErrorsQuery("abc", Since, Until, 2000, includeExpectedErrors: true);

        //assert
        Assert.DoesNotContain("error.expected", nrql);
    }

    [Fact]
    public void BuildTransactionErrorsQuery_EscapesTheQuotesOfTheIdentifier()
    {
        //arrange & act
        var nrql = NewRelicQueryBuilder.BuildTransactionErrorsQuery("abc' OR 1=1 --", Since, Until, 2000);

        //assert
        Assert.Contains(@"entityGuid = 'abc\' OR 1=1 --'", nrql);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void BuildTransactionErrorsQuery_WithABlankIdentifier_ThrowsArgumentException(string newRelicId)
    {
        //arrange & act & assert
        Assert.ThrowsAny<ArgumentException>(() => NewRelicQueryBuilder.BuildTransactionErrorsQuery(newRelicId, Since, Until, 2000));
    }

    [Fact]
    public void BuildNerdGraphQuery_WrapsTheNrqlOnTheAccountQueryEscapingTheQuotes()
    {
        //arrange
        const string nrql = "SELECT * FROM TransactionError WHERE entityGuid = 'abc' AND transactionName = \"x\"";

        //act
        var query = NewRelicQueryBuilder.BuildNerdGraphQuery(987654, nrql, 45);

        //assert
        Assert.Equal("{ actor { account(id: 987654) { nrql(query: \"SELECT * FROM TransactionError WHERE entityGuid = 'abc' AND transactionName = \\\"x\\\"\", timeout: 45) { results } } } }", query);
    }

    [Fact]
    public void ToEpochMilliseconds_ConvertsUtcDates()
    {
        //arrange & act & assert
        Assert.Equal(0, NewRelicQueryBuilder.ToEpochMilliseconds(DateTime.UnixEpoch));
        Assert.Equal(1748779200000, NewRelicQueryBuilder.ToEpochMilliseconds(Since));
    }

    [Fact]
    public void ToEpochMilliseconds_TreatsUnspecifiedDatesAsUtc()
    {
        //arrange
        var unspecified = DateTime.SpecifyKind(Since, DateTimeKind.Unspecified);

        //act & assert
        Assert.Equal(NewRelicQueryBuilder.ToEpochMilliseconds(Since), NewRelicQueryBuilder.ToEpochMilliseconds(unspecified));
    }
}
