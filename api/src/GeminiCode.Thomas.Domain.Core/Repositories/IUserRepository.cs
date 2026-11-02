using System.Threading.Tasks;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Filters;
using GeminiCode.Thomas.Domain.Core.Repositories.Base;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;

namespace GeminiCode.Thomas.Domain.Core.Repositories;

public interface IUserRepository : IBaseEntityRepository<User>
{
    Task<User> LoginAsync(string email, string password);
    Task<PagedList<User>> ListPaginateAsync(UserFilter filter);
    Task<User> FindByEmailAsync(string email);
}
