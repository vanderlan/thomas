using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Queries.GetPaginated;

public class UserGetPaginatedRequestHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<UserGetPaginatedRequest, PagedList<UserGetPaginatedResponse>>
{
    public async Task<PagedList<UserGetPaginatedResponse>> Handle(UserGetPaginatedRequest request, CancellationToken cancellationToken)
    {
        var users = await unitOfWork.UserRepository.ListPaginateAsync(request);

        var usersPaginated = users.Items.Select(user => (UserGetPaginatedResponse)user).ToList();

        return new PagedList<UserGetPaginatedResponse>(usersPaginated, users.Count);
    }
}