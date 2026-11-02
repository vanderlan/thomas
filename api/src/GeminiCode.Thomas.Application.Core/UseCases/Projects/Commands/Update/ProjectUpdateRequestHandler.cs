using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Localization;
using static GeminiCode.Thomas.Crosscutting.Resources.Messages.MessagesKeys;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Update;

public class ProjectUpdateRequestHandler(
    IUnitOfWork unitOfWork,
    IStringLocalizer<ThomasResources> resourceMessages) : IRequestHandler<ProjectUpdateRequest>
{
    public async Task Handle(ProjectUpdateRequest request, CancellationToken cancellationToken)
    {
        var project = await unitOfWork.ProjectRepository.GetByIdAsync(request.PublicId)
            ?? throw new NotFoundException(request.PublicId);

        await ValidateProject(request);

        project.Name = request.Name.Trim();
        project.Description = request.Description;
        project.NewRelicId = request.NewRelicId.Trim();
        project.GitHubRepository = request.GitHubRepository.Trim();
        project.PriorityBranch = request.PriorityBranch.Trim();

        //The keys are only replaced when informed, so the clients do not need to resend the secrets on every update.
        if (request.GitHubKeys is not null)
            project.GitHubKeys = request.GitHubKeys;

        unitOfWork.ProjectRepository.Update(project);

        await unitOfWork.CommitAsync();
    }

    private async Task ValidateProject(ProjectUpdateRequest request)
    {
        var projectFound = await unitOfWork.ProjectRepository.FindByNameAsync(request.Name.Trim());

        if (projectFound != null && projectFound.PublicId != request.PublicId)
            throw new ConflictException(resourceMessages[ProjectMessages.NameExists], resourceMessages[ExceptionsTitles.ValidationError]);
    }
}
