using GeminiCode.Thomas.Domain.Core.Filters;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Queries.GetPaginated;

public class UserGetPaginatedRequest : UserFilter, IRequest<PagedList<UserGetPaginatedResponse>>;