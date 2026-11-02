using GeminiCode.Thomas.Domain.Core.Entities;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetById;

public class ProjectGetByIdResponse
{
    public string PublicId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string NewRelicId { get; set; }
    public string GitHubRepository { get; set; }
    public string PriorityBranch { get; set; }

    /// <summary>
    /// Indicates whether the project has GitHub keys configured. The keys themselves are never returned.
    /// </summary>
    public bool HasGitHubKeys { get; set; }

    public DateTime? LastErrorSyncAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }

    public static explicit operator ProjectGetByIdResponse(Project project)
    {
        if (project is null)
            return default;

        return new ProjectGetByIdResponse
        {
            PublicId = project.PublicId,
            Name = project.Name,
            Description = project.Description,
            NewRelicId = project.NewRelicId,
            GitHubRepository = project.GitHubRepository,
            PriorityBranch = project.PriorityBranch,
            HasGitHubKeys = !string.IsNullOrWhiteSpace(project.GitHubKeys?.AccessToken),
            LastErrorSyncAt = project.LastErrorSyncAt,
            LastUpdated = project.LastUpdated,
            CreatedAt = project.CreatedAt
        };
    }
}
