using System.Collections.Generic;
using System.Threading.Tasks;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Filters;
using GeminiCode.Thomas.Domain.Core.Repositories.Base;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;

namespace GeminiCode.Thomas.Domain.Core.Repositories;

public interface IErrorLogRepository : IBaseEntityRepository<ErrorLog>
{
    Task<PagedList<ErrorLog>> ListPaginateAsync(ErrorLogFilter filter);

    /// <summary>
    /// Finds the knowledge base entries of a project that match any of the given fingerprints.
    /// </summary>
    Task<IEnumerable<ErrorLog>> FindByFingerprintsAsync(string projectId, IEnumerable<string> fingerprints);

    /// <summary>
    /// Stages the removal of every entry of the given project. The removal is only applied on commit.
    /// </summary>
    void DeleteByProject(string projectId);
}
