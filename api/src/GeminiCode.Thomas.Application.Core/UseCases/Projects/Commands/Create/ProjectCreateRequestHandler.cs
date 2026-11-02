using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using MediatR;
using Microsoft.Extensions.Localization;
using static GeminiCode.Thomas.Crosscutting.Resources.Messages.MessagesKeys;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;

public class ProjectCreateRequestHandler(
    IUnitOfWork unitOfWork,
    IStringLocalizer<ThomasResources> resourceMessages) : IRequestHandler<ProjectCreateRequest, ProjectCreateResponse>
{
    public async Task<ProjectCreateResponse> Handle(ProjectCreateRequest request, CancellationToken cancellationToken)
    {
        await ValidateProject(request);

        var added = await unitOfWork.ProjectRepository.AddAsync((Project)request);
        await unitOfWork.CommitAsync();

        return (ProjectCreateResponse)added;
    }

    private async Task ValidateProject(ProjectCreateRequest request)
    {
        var projectFound = await unitOfWork.ProjectRepository.FindByNameAsync(request.Name?.Trim());

        if (projectFound != null)
            throw new ConflictException(resourceMessages[ProjectMessages.NameExists], resourceMessages[ExceptionsTitles.ValidationError]);
    }
}
