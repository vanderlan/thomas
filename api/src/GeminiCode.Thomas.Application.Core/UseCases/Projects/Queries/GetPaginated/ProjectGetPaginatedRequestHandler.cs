using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetPaginated;

public class ProjectGetPaginatedRequestHandler(IUnitOfWork unitOfWork)
    : IRequestHandler<ProjectGetPaginatedRequest, PagedList<ProjectGetPaginatedResponse>>
{
    public async Task<PagedList<ProjectGetPaginatedResponse>> Handle(ProjectGetPaginatedRequest request, CancellationToken cancellationToken)
    {
        var projects = await unitOfWork.ProjectRepository.ListPaginateAsync(request);

        var projectsPaginated = projects.Items.Select(project => (ProjectGetPaginatedResponse)project).ToList();

        return new PagedList<ProjectGetPaginatedResponse>(projectsPaginated, projects.Count);
    }
}
