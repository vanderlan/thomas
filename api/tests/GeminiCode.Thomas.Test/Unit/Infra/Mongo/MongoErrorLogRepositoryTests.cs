using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;
using GeminiCode.Thomas.Domain.Core.Filters;
using GeminiCode.Thomas.Infra.Data.Mongo.Repository.Implementations;
using GeminiCode.Thomas.Test.Shared.Faker;
using GeminiCode.Thomas.Test.Unit.Infra.Mongo.Setup;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Infra.Mongo;

public class MongoErrorLogRepositoryTests
{
    private readonly MongoMockContext _mongo = new();

    [Fact]
    public async Task FindByFingerprintsAsync_FiltersByProjectAndFingerprints()
    {
        //arrange
        var projectId = Guid.NewGuid().ToString();
        var errorLog = ErrorLogFaker.Get(projectId);
        var capture = MongoMockContext.SetupFind(_mongo.ErrorLogs, errorLog);
        var repository = new MongoErrorLogRepository(_mongo.CreateDataContext());

        //act
        var found = await repository.FindByFingerprintsAsync(projectId, [errorLog.Fingerprint, "other", errorLog.Fingerprint]);

        //assert
        Assert.Same(errorLog, found.Single());
        Assert.Equal(projectId, capture.RenderedFilter[nameof(ErrorLog.ProjectId)].AsString);

        var fingerprints = capture.RenderedFilter[nameof(ErrorLog.Fingerprint)].AsBsonDocument["$in"].AsBsonArray.Select(value => value.AsString).ToList();
        Assert.Equal(2, fingerprints.Count);
        Assert.Contains(errorLog.Fingerprint, fingerprints);
        Assert.Contains("other", fingerprints);
    }

    [Fact]
    public async Task FindByFingerprintsAsync_WithoutFingerprints_DoesNotQueryTheDatabase()
    {
        //arrange
        var repository = new MongoErrorLogRepository(_mongo.CreateDataContext());

        //act
        var found = await repository.FindByFingerprintsAsync(Guid.NewGuid().ToString(), []);

        //assert
        Assert.Empty(found);
        _mongo.ErrorLogs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteByProject_StagesADeleteManyByProjectExecutedOnSave()
    {
        //arrange
        var projectId = Guid.NewGuid().ToString();
        FilterDefinition<ErrorLog> capturedFilter = null;

        _mongo.ErrorLogs.Setup(collection => collection.DeleteManyAsync(It.IsAny<FilterDefinition<ErrorLog>>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<ErrorLog>, CancellationToken>((filter, _) => capturedFilter = filter)
            .ReturnsAsync(new DeleteResult.Acknowledged(3));

        var dataContext = _mongo.CreateDataContext();
        var repository = new MongoErrorLogRepository(dataContext);

        //act
        repository.DeleteByProject(projectId);

        //assert: nothing is written before the save
        Assert.True(dataContext.HasPendingWrites);
        _mongo.ErrorLogs.Verify(collection => collection.DeleteManyAsync(It.IsAny<FilterDefinition<ErrorLog>>(), It.IsAny<CancellationToken>()), Times.Never);

        var affected = await dataContext.SaveChangesAsync();

        Assert.Equal(3, affected);
        Assert.False(dataContext.HasPendingWrites);
        Assert.Equal(projectId, MongoMockContext.Render(capturedFilter)[nameof(ErrorLog.ProjectId)].AsString);
    }

    [Fact]
    public async Task ListPaginateAsync_FiltersByProjectStatusAndQueryOrderedByTheMostRecent()
    {
        //arrange
        const string query = "Null (ref)";
        var projectId = Guid.NewGuid().ToString();
        var capture = MongoMockContext.SetupFind(_mongo.ErrorLogs, ErrorLogFaker.Get(projectId));

        FilterDefinition<ErrorLog> countFilter = null;

        _mongo.ErrorLogs.Setup(collection => collection.CountDocumentsAsync(It.IsAny<FilterDefinition<ErrorLog>>(), It.IsAny<CountOptions>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<ErrorLog>, CountOptions, CancellationToken>((filter, _, _) => countFilter = filter)
            .ReturnsAsync(1);

        var repository = new MongoErrorLogRepository(_mongo.CreateDataContext());

        //act
        var result = await repository.ListPaginateAsync(new ErrorLogFilter
        {
            ProjectId = projectId,
            Status = ErrorLogStatus.Recurring,
            Query = query,
            Page = 3,
            Quantity = 20
        });

        //assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Count);
        Assert.Equal((int?)40, capture.Options.Skip);
        Assert.Equal((int?)20, capture.Options.Limit);
        Assert.Equal(-1, MongoMockContext.Render(capture.Options.Sort)[nameof(ErrorLog.LastSeenAt)].AsInt32);

        var rendered = capture.RenderedFilter;
        Assert.Equal(projectId, rendered[nameof(ErrorLog.ProjectId)].AsString);
        Assert.Equal((int)ErrorLogStatus.Recurring, rendered[nameof(ErrorLog.Status)].AsInt32);

        var orFilters = rendered["$or"].AsBsonArray.Select(value => value.AsBsonDocument).ToList();
        Assert.Equal(2, orFilters.Count);
        Assert.Contains(orFilters, filter => filter.Contains(nameof(ErrorLog.ErrorClass)) && filter[nameof(ErrorLog.ErrorClass)].AsBsonRegularExpression.Pattern == Regex.Escape(query));
        Assert.Contains(orFilters, filter => filter.Contains(nameof(ErrorLog.Message)) && filter[nameof(ErrorLog.Message)].AsBsonRegularExpression.Options == "i");

        //the count must use exactly the same filter of the list
        Assert.Equal(rendered, MongoMockContext.Render(countFilter));
    }

    [Fact]
    public async Task ListPaginateAsync_WithoutFilters_UsesAnEmptyFilter()
    {
        //arrange
        var capture = MongoMockContext.SetupFind(_mongo.ErrorLogs);

        _mongo.ErrorLogs.Setup(collection => collection.CountDocumentsAsync(It.IsAny<FilterDefinition<ErrorLog>>(), It.IsAny<CountOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var repository = new MongoErrorLogRepository(_mongo.CreateDataContext());

        //act
        var result = await repository.ListPaginateAsync(new ErrorLogFilter());

        //assert
        Assert.Empty(result.Items);
        Assert.Equal(new BsonDocument(), capture.RenderedFilter);
    }
}
