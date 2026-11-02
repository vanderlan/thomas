using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Filters;
using GeminiCode.Thomas.Domain.Core.Repositories;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using GeminiCode.Thomas.Infra.Data.Mongo.Context;
using GeminiCode.Thomas.Infra.Data.Mongo.Repository.Generic;
using MongoDB.Bson;
using MongoDB.Driver;

namespace GeminiCode.Thomas.Infra.Data.Mongo.Repository.Implementations;

public sealed class MongoProjectRepository(MongoDataContext context) : MongoBaseEntityRepository<Project>(context), IProjectRepository
{
    public async Task<Project> FindByNameAsync(string name)
    {
        return await Collection.Find(project => project.Name == name).FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<Project>> ListAllAsync()
    {
        return await Collection.Find(Builders<Project>.Filter.Empty)
            .SortBy(project => project.CreatedAt)
            .ToListAsync();
    }

    public async Task<PagedList<Project>> ListPaginateAsync(ProjectFilter filter)
    {
        var filterDefinition = ApplyFilters(filter);

        var pagination = (filter.Page * filter.Quantity) - filter.Quantity;

        var entityList = await Collection.Find(filterDefinition)
            .SortBy(project => project.CreatedAt)
            .Skip(pagination)
            .Limit(filter.Quantity)
            .ToListAsync();

        var count = await Collection.CountDocumentsAsync(filterDefinition);

        return new PagedList<Project>(entityList, (int)count);
    }

    private static FilterDefinition<Project> ApplyFilters(ProjectFilter filter)
    {
        var filterBuilder = Builders<Project>.Filter;
        var filterDefinition = filterBuilder.Empty;

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            //Case insensitive "contains" on the project name.
            filterDefinition &= filterBuilder.Regex(project => project.Name,
                new BsonRegularExpression(Regex.Escape(filter.Query), "i"));
        }

        return filterDefinition;
    }
}
