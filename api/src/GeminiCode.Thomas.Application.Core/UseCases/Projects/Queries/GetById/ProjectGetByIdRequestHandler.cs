using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetById;

public class ProjectGetByIdRequestHandler(IUnitOfWork unitOfWork) : IRequestHandler<ProjectGetByIdRequest, ProjectGetByIdResponse>
{
    public async Task<ProjectGetByIdResponse> Handle(ProjectGetByIdRequest request, CancellationToken cancellationToken)
    {
        var project = await unitOfWork.ProjectRepository.GetByIdAsync(request.PublicId)
            ?? throw new NotFoundException(request.PublicId);

        return (ProjectGetByIdResponse)project;
    }
}
