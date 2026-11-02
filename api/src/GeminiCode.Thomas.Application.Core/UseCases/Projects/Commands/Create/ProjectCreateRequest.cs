using GeminiCode.Thomas.Domain.Core.Entities;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;

public class ProjectCreateRequest : IRequest<ProjectCreateResponse>
{
    public string Name { get; set; }
    public string Description { get; set; }
    public string NewRelicId { get; set; }
    public string GitHubRepository { get; set; }
    public GitHubKeys GitHubKeys { get; set; }
    public string PriorityBranch { get; set; }

    public static explicit operator Project(ProjectCreateRequest request)
    {
        if (request is null)
            return default;

        return new Project
        {
            Name = request.Name?.Trim(),
            Description = request.Description,
            NewRelicId = request.NewRelicId?.Trim(),
            GitHubRepository = request.GitHubRepository?.Trim(),
            GitHubKeys = request.GitHubKeys,
            PriorityBranch = request.PriorityBranch?.Trim()
        };
    }
}
