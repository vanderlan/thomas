namespace GeminiCode.Thomas.Domain.Core.Entities;

/// <summary>
/// The GitHub credentials of a project. They are stored to allow the creation of Pull Requests
/// in the future and must never be exposed by the API responses.
/// </summary>
public class GitHubKeys
{
    /// <summary>
    /// Personal Access Token (or GitHub App installation token) with permission to read the repository and open Pull Requests.
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Optional private key (deploy key or GitHub App private key) used when the integration requires it.
    /// </summary>
    public string PrivateKey { get; set; }
}
