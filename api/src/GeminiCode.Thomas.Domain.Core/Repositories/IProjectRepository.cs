using System.Collections.Generic;
using System.Threading.Tasks;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Filters;
using GeminiCode.Thomas.Domain.Core.Repositories.Base;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;

namespace GeminiCode.Thomas.Domain.Core.Repositories;

public interface IProjectRepository : IBaseEntityRepository<Project>
{
    Task<PagedList<Project>> ListPaginateAsync(ProjectFilter filter);
    Task<IEnumerable<Project>> ListAllAsync();
    Task<Project> FindByNameAsync(string name);
}
