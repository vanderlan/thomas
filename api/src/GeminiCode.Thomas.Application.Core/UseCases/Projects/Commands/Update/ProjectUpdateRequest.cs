using GeminiCode.Thomas.Domain.Core.Entities;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Update;

public class ProjectUpdateRequest : IRequest
{
    public string PublicId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string NewRelicId { get; set; }
    public string GitHubRepository { get; set; }

    /// <summary>
    /// Optional: when omitted the current keys of the project are kept.
    /// </summary>
    public GitHubKeys GitHubKeys { get; set; }

    public string PriorityBranch { get; set; }
}
