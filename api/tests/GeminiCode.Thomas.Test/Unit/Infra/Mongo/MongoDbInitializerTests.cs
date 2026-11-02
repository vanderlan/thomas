using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;
using GeminiCode.Thomas.Domain.Core.Extensions;
using GeminiCode.Thomas.Infra.Data.Mongo.Initialization;
using GeminiCode.Thomas.Test.Unit.Infra.Mongo.Setup;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Infra.Mongo;

public class MongoDbInitializerTests
{
    private readonly MongoMockContext _mongo = new();
    private readonly Mock<IMongoIndexManager<User>> _userIndexes = new();
    private readonly Mock<IMongoIndexManager<RefreshToken>> _refreshTokenIndexes = new();
    private readonly Mock<IMongoIndexManager<Project>> _projectIndexes = new();
    private readonly Mock<IMongoIndexManager<ErrorLog>> _errorLogIndexes = new();

    private IEnumerable<CreateIndexModel<User>> _createdUserIndexes = [];
    private IEnumerable<CreateIndexModel<RefreshToken>> _createdRefreshTokenIndexes = [];
    private IEnumerable<CreateIndexModel<Project>> _createdProjectIndexes = [];
    private IEnumerable<CreateIndexModel<ErrorLog>> _createdErrorLogIndexes = [];

    private FilterDefinition<User> _defaultUserCountFilter;
    private CountOptions _defaultUserCountOptions;

    public MongoDbInitializerTests()
    {
        _mongo.Users.Setup(collection => collection.Indexes).Returns(_userIndexes.Object);
        _mongo.RefreshTokens.Setup(collection => collection.Indexes).Returns(_refreshTokenIndexes.Object);

        _userIndexes.Setup(indexes => indexes.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<User>>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CreateIndexModel<User>>, CancellationToken>((models, _) => _createdUserIndexes = models.ToList())
            .ReturnsAsync(Enumerable.Empty<string>());

        _refreshTokenIndexes.Setup(indexes => indexes.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<RefreshToken>>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CreateIndexModel<RefreshToken>>, CancellationToken>((models, _) => _createdRefreshTokenIndexes = models.ToList())
            .ReturnsAsync(Enumerable.Empty<string>());

        _mongo.Projects.Setup(collection => collection.Indexes).Returns(_projectIndexes.Object);
        _mongo.ErrorLogs.Setup(collection => collection.Indexes).Returns(_errorLogIndexes.Object);

        _projectIndexes.Setup(indexes => indexes.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<Project>>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CreateIndexModel<Project>>, CancellationToken>((models, _) => _createdProjectIndexes = models.ToList())
            .ReturnsAsync(Enumerable.Empty<string>());

        _errorLogIndexes.Setup(indexes => indexes.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<ErrorLog>>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<CreateIndexModel<ErrorLog>>, CancellationToken>((models, _) => _createdErrorLogIndexes = models.ToList())
            .ReturnsAsync(Enumerable.Empty<string>());

        _mongo.Users.Setup(collection => collection.InsertOneAsync(It.IsAny<User>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private MongoDbInitializer CreateInitializer()
    {
        return new MongoDbInitializer(_mongo.CreateDataContext(), new Mock<ILogger<MongoDbInitializer>>().Object);
    }

    private void SetupDefaultUserExists(bool exists)
    {
        _mongo.Users.Setup(collection => collection.CountDocumentsAsync(It.IsAny<FilterDefinition<User>>(), It.IsAny<CountOptions>(), It.IsAny<CancellationToken>()))
            .Callback<FilterDefinition<User>, CountOptions, CancellationToken>((filter, options, _) =>
            {
                _defaultUserCountFilter = filter;
                _defaultUserCountOptions = options;
            })
            .ReturnsAsync(exists ? 1L : 0L);
    }

    [Fact]
    public async Task InitializeAsync_CreatesTheUniqueIndexesOfTheEntities()
    {
        //arrange
        SetupDefaultUserExists(true);

        //act
        await CreateInitializer().InitializeAsync();

        //assert
        var userIndex = Assert.Single(_createdUserIndexes);
        Assert.True(userIndex.Options.Unique);
        Assert.Equal(1, MongoMockContext.Render(userIndex.Keys)[nameof(User.Email)].AsInt32);

        Assert.Equal(2, _createdRefreshTokenIndexes.Count());
        Assert.All(_createdRefreshTokenIndexes, index => Assert.True(index.Options.Unique));

        var indexedFields = _createdRefreshTokenIndexes
            .Select(index => MongoMockContext.Render(index.Keys).Names.Single())
            .ToList();

        Assert.Contains(nameof(RefreshToken.Email), indexedFields);
        Assert.Contains(nameof(RefreshToken.Refreshtoken), indexedFields);
    }

    [Fact]
    public async Task InitializeAsync_CreatesAUniqueIndexOnTheProjectName()
    {
        //arrange
        SetupDefaultUserExists(true);

        //act
        await CreateInitializer().InitializeAsync();

        //assert
        var projectIndex = Assert.Single(_createdProjectIndexes);
        Assert.True(projectIndex.Options.Unique);
        Assert.Equal(1, MongoMockContext.Render(projectIndex.Keys)[nameof(Project.Name)].AsInt32);
    }

    [Fact]
    public async Task InitializeAsync_CreatesTheErrorLogIndexesByProjectAndFingerprint()
    {
        //arrange
        SetupDefaultUserExists(true);

        //act
        await CreateInitializer().InitializeAsync();

        //assert
        Assert.Equal(2, _createdErrorLogIndexes.Count());

        //an error (fingerprint) must be registered only once per project
        var fingerprintIndex = Assert.Single(_createdErrorLogIndexes, index => index.Options.Unique == true);
        var fingerprintKeys = MongoMockContext.Render(fingerprintIndex.Keys);
        Assert.Equal(1, fingerprintKeys[nameof(ErrorLog.ProjectId)].AsInt32);
        Assert.Equal(1, fingerprintKeys[nameof(ErrorLog.Fingerprint)].AsInt32);

        //the listing is ordered by the most recent occurrences
        var listingIndex = Assert.Single(_createdErrorLogIndexes, index => index.Options.Unique != true);
        var listingKeys = MongoMockContext.Render(listingIndex.Keys);
        Assert.Equal(1, listingKeys[nameof(ErrorLog.ProjectId)].AsInt32);
        Assert.Equal(-1, listingKeys[nameof(ErrorLog.LastSeenAt)].AsInt32);
    }

    [Fact]
    public async Task InitializeAsync_WhenTheDefaultUserDoesNotExist_SeedsTheSameUserOfTheRelationalMigration()
    {
        //arrange
        SetupDefaultUserExists(false);
        User insertedUser = null;

        _mongo.Users.Setup(collection => collection.InsertOneAsync(It.IsAny<User>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
            .Callback<User, InsertOneOptions, CancellationToken>((user, _, _) => insertedUser = user)
            .Returns(Task.CompletedTask);

        //act
        await CreateInitializer().InitializeAsync();

        //assert
        Assert.Equal(MongoDbInitializer.DefaultSystemUserPublicId, MongoMockContext.Render(_defaultUserCountFilter)["_id"].AsString);
        Assert.Equal((long?)1, _defaultUserCountOptions.Limit);

        Assert.NotNull(insertedUser);
        Assert.Equal(MongoDbInitializer.DefaultSystemUserPublicId, insertedUser.PublicId);
        Assert.Equal("adm@thomas.com", insertedUser.Email);
        Assert.Equal("Thomas Admin User", insertedUser.Name);
        Assert.Equal(UserProfile.Admin, insertedUser.Profile);
        Assert.Equal("123".ToSha512(), insertedUser.Password);
        Assert.Equal(DateTimeKind.Utc, insertedUser.CreatedAt.Kind);
    }

    [Fact]
    public async Task InitializeAsync_WhenTheDefaultUserAlreadyExists_DoesNotInsertItAgain()
    {
        //arrange
        SetupDefaultUserExists(true);

        //act
        await CreateInitializer().InitializeAsync();

        //assert
        _mongo.Users.Verify(collection => collection.InsertOneAsync(It.IsAny<User>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent()
    {
        //arrange
        SetupDefaultUserExists(true);
        var initializer = CreateInitializer();

        //act
        await initializer.InitializeAsync();
        await initializer.InitializeAsync();

        //assert
        _userIndexes.Verify(indexes => indexes.CreateManyAsync(It.IsAny<IEnumerable<CreateIndexModel<User>>>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mongo.Users.Verify(collection => collection.InsertOneAsync(It.IsAny<User>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
