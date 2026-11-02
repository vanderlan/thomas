using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Delete;

public class ProjectDeleteRequestHandler(IUnitOfWork unitOfWork) : IRequestHandler<ProjectDeleteRequest>
{
    public async Task Handle(ProjectDeleteRequest request, CancellationToken cancellationToken)
    {
        var project = await unitOfWork.ProjectRepository.GetByIdAsync(request.PublicId)
            ?? throw new NotFoundException(request.PublicId);

        //The knowledge base of the project is removed together with it.
        unitOfWork.ErrorLogRepository.DeleteByProject(project.PublicId);
        await unitOfWork.ProjectRepository.DeleteAsync(project.PublicId);

        await unitOfWork.CommitAsync();
    }
}
