using System.Collections.Generic;
using System.Linq;
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

public sealed class MongoErrorLogRepository(MongoDataContext context) : MongoBaseEntityRepository<ErrorLog>(context), IErrorLogRepository
{
    public async Task<IEnumerable<ErrorLog>> FindByFingerprintsAsync(string projectId, IEnumerable<string> fingerprints)
    {
        var fingerprintList = fingerprints?.Distinct().ToList() ?? [];

        if (fingerprintList.Count == 0)
            return [];

        var filterBuilder = Builders<ErrorLog>.Filter;

        var filterDefinition = filterBuilder.Eq(errorLog => errorLog.ProjectId, projectId)
            & filterBuilder.In(errorLog => errorLog.Fingerprint, fingerprintList);

        return await Collection.Find(filterDefinition).ToListAsync();
    }

    public void DeleteByProject(string projectId)
    {
        DataContext.DeleteMany<ErrorLog>(errorLog => errorLog.ProjectId == projectId);
    }

    public async Task<PagedList<ErrorLog>> ListPaginateAsync(ErrorLogFilter filter)
    {
        var filterDefinition = ApplyFilters(filter);

        var pagination = (filter.Page * filter.Quantity) - filter.Quantity;

        var entityList = await Collection.Find(filterDefinition)
            .SortByDescending(errorLog => errorLog.LastSeenAt)
            .Skip(pagination)
            .Limit(filter.Quantity)
            .ToListAsync();

        var count = await Collection.CountDocumentsAsync(filterDefinition);

        return new PagedList<ErrorLog>(entityList, (int)count);
    }

    private static FilterDefinition<ErrorLog> ApplyFilters(ErrorLogFilter filter)
    {
        var filterBuilder = Builders<ErrorLog>.Filter;
        var filterDefinition = filterBuilder.Empty;

        if (!string.IsNullOrWhiteSpace(filter.ProjectId))
        {
            filterDefinition &= filterBuilder.Eq(errorLog => errorLog.ProjectId, filter.ProjectId);
        }

        if (filter.Status.HasValue)
        {
            filterDefinition &= filterBuilder.Eq(errorLog => errorLog.Status, filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            //Case insensitive "contains" on the error class or on the message.
            var queryRegex = new BsonRegularExpression(Regex.Escape(filter.Query), "i");

            filterDefinition &= filterBuilder.Or(
                filterBuilder.Regex(errorLog => errorLog.ErrorClass, queryRegex),
                filterBuilder.Regex(errorLog => errorLog.Message, queryRegex));
        }

        return filterDefinition;
    }
}
