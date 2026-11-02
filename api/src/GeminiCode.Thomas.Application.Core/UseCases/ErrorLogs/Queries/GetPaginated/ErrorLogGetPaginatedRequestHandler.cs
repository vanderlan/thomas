using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetPaginated;

public class ErrorLogGetPaginatedRequestHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ErrorLogGetPaginatedRequest, PagedList<ErrorLogGetPaginatedResponse>>
{
    public async Task<PagedList<ErrorLogGetPaginatedResponse>> Handle(ErrorLogGetPaginatedRequest request, CancellationToken cancellationToken)
    {
        var errorLogs = await unitOfWork.ErrorLogRepository.ListPaginateAsync(request);

        var errorLogsPaginated = errorLogs.Items.Select(errorLog => (ErrorLogGetPaginatedResponse)errorLog).ToList();

        return new PagedList<ErrorLogGetPaginatedResponse>(errorLogsPaginated, errorLogs.Count);
    }
}
