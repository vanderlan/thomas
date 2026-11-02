using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;
using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Crosscutting.Resources.Messages;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Update;

public class ProjectUpdateRequestValidator : AbstractValidator<ProjectUpdateRequest>
{
    public ProjectUpdateRequestValidator(IStringLocalizer<ThomasResources> stringLocalizer)
    {
        RuleFor(c => c)
            .NotNull()
            .WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.NullEntity]);

        RuleFor(c => c.Name)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.EmptyName]);

        RuleFor(c => c.NewRelicId)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.EmptyNewRelicId]);

        RuleFor(c => c.GitHubRepository)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.EmptyGitHubRepository])
            .Matches(ProjectCreateRequestValidator.GitHubRepositoryPattern).WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.InvalidGitHubRepository]);

        When(c => c.GitHubKeys is not null, () =>
        {
            RuleFor(c => c.GitHubKeys.AccessToken)
                .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.EmptyGitHubAccessToken]);
        });

        RuleFor(c => c.PriorityBranch)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.EmptyPriorityBranch]);
    }
}
