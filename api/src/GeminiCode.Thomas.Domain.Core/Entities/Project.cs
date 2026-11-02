using System;

namespace GeminiCode.Thomas.Domain.Core.Entities;

/// <summary>
/// A project of the portfolio. Its errors are collected from NewRelic by the error log sync job
/// and, in the future, fixed through Pull Requests opened on its GitHub repository.
/// </summary>
public class Project : BaseEntity
{
    public required string Name { get; set; }
    public string Description { get; set; }

    /// <summary>
    /// The NewRelic identifier of the monitored application: either the entity GUID or the numeric APM application id.
    /// </summary>
    public required string NewRelicId { get; set; }

    /// <summary>
    /// The GitHub repository in the "owner/repository" format.
    /// </summary>
    public required string GitHubRepository { get; set; }

    public required GitHubKeys GitHubKeys { get; set; }

    /// <summary>
    /// The branch used as the base (target) of the Pull Requests.
    /// </summary>
    public required string PriorityBranch { get; set; }

    /// <summary>
    /// The upper bound of the last NewRelic window synchronized by the error log job.
    /// It is null while the project has never been synchronized.
    /// </summary>
    public DateTime? LastErrorSyncAt { get; set; }
}
