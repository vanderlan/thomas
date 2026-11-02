using GeminiCode.Thomas.Domain.Core.Entities;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetPaginated;

public class ProjectGetPaginatedResponse
{
    public string PublicId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string NewRelicId { get; set; }
    public string GitHubRepository { get; set; }
    public string PriorityBranch { get; set; }
    public DateTime? LastErrorSyncAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }

    public static implicit operator ProjectGetPaginatedResponse(Project project)
    {
        if (project is null)
            return default;

        return new ProjectGetPaginatedResponse
        {
            PublicId = project.PublicId,
            Name = project.Name,
            Description = project.Description,
            NewRelicId = project.NewRelicId,
            GitHubRepository = project.GitHubRepository,
            PriorityBranch = project.PriorityBranch,
            LastErrorSyncAt = project.LastErrorSyncAt,
            LastUpdated = project.LastUpdated,
            CreatedAt = project.CreatedAt
        };
    }
}
