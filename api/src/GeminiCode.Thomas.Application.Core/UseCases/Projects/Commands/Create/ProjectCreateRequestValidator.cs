using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Crosscutting.Resources.Messages;
using FluentValidation;
using Microsoft.Extensions.Localization;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;

public class ProjectCreateRequestValidator : AbstractValidator<ProjectCreateRequest>
{
    /// <summary>
    /// The repository must be informed as "owner/repository".
    /// </summary>
    public const string GitHubRepositoryPattern = @"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$";

    public ProjectCreateRequestValidator(IStringLocalizer<ThomasResources> stringLocalizer)
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
            .Matches(GitHubRepositoryPattern).WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.InvalidGitHubRepository]);

        RuleFor(c => c.GitHubKeys)
            .NotNull().WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.EmptyGitHubKeys]);

        When(c => c.GitHubKeys is not null, () =>
        {
            RuleFor(c => c.GitHubKeys.AccessToken)
                .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.EmptyGitHubAccessToken]);
        });

        RuleFor(c => c.PriorityBranch)
            .NotEmpty().WithMessage(stringLocalizer[MessagesKeys.ProjectMessages.EmptyPriorityBranch]);
    }
}
