using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Filters;
using GeminiCode.Thomas.Infra.Data.Mongo.Repository.Implementations;
using GeminiCode.Thomas.Test.Shared.Faker;
using GeminiCode.Thomas.Test.Unit.Infra.Mongo.Setup;
using MongoDB.Bson;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Infra.Mongo;

public class MongoProjectRepositoryTests
{
    private readonly MongoMockContext _mongo = new();

    [Fact]
    public async Task FindByNameAsync_FiltersByNameOnly()
    {
        //arrange
        var project = ProjectFaker.Get();
        var capture = MongoMockContext.SetupFind(_mongo.Projects, project);
        var repository = new MongoProjectRepository(_mongo.CreateDataContext());

        //act
        var found = await repository.FindByNameAsync(project.Name);

        //assert
        Assert.Same(project, found);
        Assert.Equal(project.Name, capture.RenderedFilter[nameof(Project.Name)].AsString);
        Assert.Single(capture.FilterFields);
    }

    [Fact]
    public async Task ListAllAsync_ReturnsEveryProjectOrderedByCreation()
    {
        //arrange
        var projects = new[] { ProjectFaker.Get(), ProjectFaker.Get() };
        var capture = MongoMockContext.SetupFind(_mongo.Projects, projects);
        var repository = new MongoProjectRepository(_mongo.CreateDataContext());

        //act
        var result = await repository.ListAllAsync();

        //assert
        Assert.Equal(2, result.Count());
        Assert.Equal(new BsonDocument(), capture.RenderedFilter);
        Assert.Null(capture.Options.Limit);
        Assert.Equal(1, MongoMockContext.Render(capture.Options.Sort)[nameof(Project.CreatedAt)].AsInt32);
    }

    [Fact]
    public async Task ListPaginateAsync_WithQuery_FiltersTheNameWithACaseInsensitiveEscapedRegex()
    {
        //arrange
        const string query = "Billing (API)";
        var capture = MongoMockContext.SetupFind(_mongo.Projects, ProjectFaker.Get());

        _mongo.Projects.Setup(collection => collection.CountDocumentsAsync(It.IsAny<FilterDefinition<Project>>(), It.IsAny<CountOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var repository = new MongoProjectRepository(_mongo.CreateDataContext());

        //act
        var result = await repository.ListPaginateAsync(new ProjectFilter { Query = query, Page = 2, Quantity = 5 });

        //assert
        Assert.Single(result.Items);
        Assert.Equal(1, result.Count);
        Assert.Equal((int?)5, capture.Options.Skip);
        Assert.Equal((int?)5, capture.Options.Limit);

        var regex = capture.RenderedFilter[nameof(Project.Name)].AsBsonRegularExpression;
        Assert.Equal(Regex.Escape(query), regex.Pattern);
        Assert.Equal("i", regex.Options);
    }

    [Fact]
    public async Task ListPaginateAsync_WithoutQuery_UsesAnEmptyFilter()
    {
        //arrange
        var capture = MongoMockContext.SetupFind(_mongo.Projects);

        _mongo.Projects.Setup(collection => collection.CountDocumentsAsync(It.IsAny<FilterDefinition<Project>>(), It.IsAny<CountOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var repository = new MongoProjectRepository(_mongo.CreateDataContext());

        //act
        var result = await repository.ListPaginateAsync(new ProjectFilter());

        //assert
        Assert.Empty(result.Items);
        Assert.Equal(new BsonDocument(), capture.RenderedFilter);
    }
}
