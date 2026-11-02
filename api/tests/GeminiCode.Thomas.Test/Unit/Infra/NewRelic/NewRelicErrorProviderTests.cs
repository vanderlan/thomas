using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Infra.NewRelic.Configuration;
using GeminiCode.Thomas.Infra.NewRelic.Exceptions;
using GeminiCode.Thomas.Infra.NewRelic.Providers;
using GeminiCode.Thomas.Infra.NewRelic.Queries;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Infra.NewRelic;

public class NewRelicErrorProviderTests
{
    private static readonly DateTime Since = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Until = new(2025, 6, 1, 12, 5, 0, DateTimeKind.Utc);

    private readonly Mock<HttpMessageHandler> _handlerMock = new();
    private readonly NewRelicOptions _options = new()
    {
        ApiKey = "NRAK-TEST",
        AccountId = 123456,
        GraphQlEndpoint = "https://api.newrelic.com/graphql",
        MaxResults = 2000
    };

    private HttpRequestMessage _sentRequest;
    private string _sentBody;

    private NewRelicErrorProvider CreateProvider()
    {
        return new NewRelicErrorProvider(
            new HttpClient(_handlerMock.Object),
            Options.Create(_options),
            new Mock<ILogger<NewRelicErrorProvider>>().Object);
    }

    private void SetupResponse(HttpStatusCode statusCode, string body)
    {
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken _) =>
            {
                _sentRequest = request;
                _sentBody = request.Content is null ? null : await request.Content.ReadAsStringAsync();

                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
                };
            });
    }

    private static string SuccessResponse(params string[] results)
    {
        return "{\"data\":{\"actor\":{\"account\":{\"nrql\":{\"results\":[" + string.Join(",", results) + "]}}}}}";
    }

    [Fact]
    public async Task GetErrorsAsync_PostsTheNerdGraphQueryWithTheApiKey()
    {
        //arrange
        SetupResponse(HttpStatusCode.OK, SuccessResponse());

        //act
        await CreateProvider().GetErrorsAsync("MY-ENTITY-GUID", Since, Until);

        //assert
        Assert.NotNull(_sentRequest);
        Assert.Equal(HttpMethod.Post, _sentRequest.Method);
        Assert.Equal(_options.GraphQlEndpoint, _sentRequest.RequestUri.ToString());
        Assert.Equal("NRAK-TEST", _sentRequest.Headers.GetValues(NewRelicErrorProvider.ApiKeyHeader).Single());

        using var body = JsonDocument.Parse(_sentBody);
        var query = body.RootElement.GetProperty("query").GetString();

        var expectedNrql = NewRelicQueryBuilder.BuildTransactionErrorsQuery("MY-ENTITY-GUID", Since, Until, 2000);
        Assert.Equal(NewRelicQueryBuilder.BuildNerdGraphQuery(123456, expectedNrql, _options.TimeoutSeconds), query);
    }

    [Fact]
    public async Task GetErrorsAsync_ParsesTheTransactionErrorEvents()
    {
        //arrange
        SetupResponse(HttpStatusCode.OK, SuccessResponse(
            "{\"timestamp\":1748779260000,\"error.class\":\"System.NullReferenceException\",\"error.message\":\"Object reference not set\",\"transactionName\":\"WebTransaction/MVC/Orders/Get\",\"stack_trace\":\"   at Orders.Get()\",\"error.group.guid\":\"group-1\"}",
            "{\"timestamp\":1748779320000,\"error.class\":\"System.TimeoutException\",\"error.message\":null,\"transactionName\":\"WebTransaction/MVC/Orders/Post\",\"stack_trace\":[\"   at A()\",\"   at B()\"]}"));

        //act
        var events = await CreateProvider().GetErrorsAsync("MY-ENTITY-GUID", Since, Until);

        //assert
        Assert.Equal(2, events.Count);

        var first = events[0];
        Assert.Equal(DateTimeOffset.FromUnixTimeMilliseconds(1748779260000).UtcDateTime, first.Timestamp);
        Assert.Equal(DateTimeKind.Utc, first.Timestamp.Kind);
        Assert.Equal("System.NullReferenceException", first.ErrorClass);
        Assert.Equal("Object reference not set", first.Message);
        Assert.Equal("WebTransaction/MVC/Orders/Get", first.TransactionName);
        Assert.Equal("   at Orders.Get()", first.StackTrace);
        Assert.Equal("group-1", first.ErrorGroupId);

        var second = events[1];
        Assert.Equal("System.TimeoutException", second.ErrorClass);
        Assert.Null(second.Message);
        Assert.Null(second.ErrorGroupId);
        Assert.Equal("   at A()" + Environment.NewLine + "   at B()", second.StackTrace);
    }

    [Fact]
    public async Task GetErrorsAsync_WithoutResults_ReturnsAnEmptyList()
    {
        //arrange
        SetupResponse(HttpStatusCode.OK, SuccessResponse());

        //act
        var events = await CreateProvider().GetErrorsAsync("MY-ENTITY-GUID", Since, Until);

        //assert
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetErrorsAsync_WithGraphQlErrors_ThrowsNewRelicApiException()
    {
        //arrange
        SetupResponse(HttpStatusCode.OK, "{\"data\":{\"actor\":{\"account\":null}},\"errors\":[{\"message\":\"Not authorized\"},{\"message\":\"Bad NRQL\"}]}");

        //act
        var exception = await Assert.ThrowsAsync<NewRelicApiException>(() => CreateProvider().GetErrorsAsync("MY-ENTITY-GUID", Since, Until));

        //assert
        Assert.Contains("Not authorized", exception.Message);
        Assert.Contains("Bad NRQL", exception.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task GetErrorsAsync_WithAnErrorStatusCode_ThrowsNewRelicApiException(HttpStatusCode statusCode)
    {
        //arrange
        SetupResponse(statusCode, "{\"message\":\"failure\"}");

        //act
        var exception = await Assert.ThrowsAsync<NewRelicApiException>(() => CreateProvider().GetErrorsAsync("MY-ENTITY-GUID", Since, Until));

        //assert
        Assert.Contains(((int)statusCode).ToString(), exception.Message);
    }

    [Fact]
    public async Task GetErrorsAsync_WithAnUnexpectedPayload_ThrowsNewRelicApiException()
    {
        //arrange
        SetupResponse(HttpStatusCode.OK, "{\"data\":{\"actor\":{}}}");

        //act & assert
        await Assert.ThrowsAsync<NewRelicApiException>(() => CreateProvider().GetErrorsAsync("MY-ENTITY-GUID", Since, Until));
    }

    [Fact]
    public async Task GetErrorsAsync_WithAnInvalidJson_ThrowsNewRelicApiException()
    {
        //arrange
        SetupResponse(HttpStatusCode.OK, "<html>not json</html>");

        //act & assert
        await Assert.ThrowsAsync<NewRelicApiException>(() => CreateProvider().GetErrorsAsync("MY-ENTITY-GUID", Since, Until));
    }

    [Fact]
    public async Task GetErrorsAsync_WhenTheApiIsUnreachable_ThrowsNewRelicApiException()
    {
        //arrange
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("No such host is known"));

        //act
        var exception = await Assert.ThrowsAsync<NewRelicApiException>(() => CreateProvider().GetErrorsAsync("MY-ENTITY-GUID", Since, Until));

        //assert
        Assert.IsType<HttpRequestException>(exception.InnerException);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task GetErrorsAsync_WithoutApiKey_ThrowsNewRelicApiExceptionWithoutCallingTheApi(string apiKey)
    {
        //arrange
        _options.ApiKey = apiKey;
        SetupResponse(HttpStatusCode.OK, SuccessResponse());

        //act
        var exception = await Assert.ThrowsAsync<NewRelicApiException>(() => CreateProvider().GetErrorsAsync("MY-ENTITY-GUID", Since, Until));

        //assert
        Assert.Contains("NewRelic:ApiKey", exception.Message);
        Assert.Null(_sentRequest);
    }

    [Fact]
    public async Task GetErrorsAsync_WithoutAccountId_ThrowsNewRelicApiExceptionWithoutCallingTheApi()
    {
        //arrange
        _options.AccountId = 0;
        SetupResponse(HttpStatusCode.OK, SuccessResponse());

        //act
        var exception = await Assert.ThrowsAsync<NewRelicApiException>(() => CreateProvider().GetErrorsAsync("MY-ENTITY-GUID", Since, Until));

        //assert
        Assert.Contains("NewRelic:AccountId", exception.Message);
        Assert.Null(_sentRequest);
    }

    [Fact]
    public async Task GetErrorsAsync_WithABlankIdentifier_ThrowsArgumentException()
    {
        //arrange
        SetupResponse(HttpStatusCode.OK, SuccessResponse());

        //act & assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateProvider().GetErrorsAsync(" ", Since, Until));
    }
}
