using System;
using Bogus;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Update;
using GeminiCode.Thomas.Domain.Core.Entities;

namespace GeminiCode.Thomas.Test.Shared.Faker;

public static class ProjectFaker
{
    public static Project Get()
    {
        return new Faker<Project>()
            .CustomInstantiator(f => new Project
            {
                Name = $"{f.Commerce.ProductName()} {Guid.NewGuid():N}",
                NewRelicId = f.Random.Guid().ToString(),
                GitHubRepository = $"{f.Random.AlphaNumeric(8)}/{f.Lorem.Word()}",
                GitHubKeys = GetGitHubKeys(),
                PriorityBranch = "main"
            })
            .RuleFor(o => o.Description, f => f.Lorem.Sentence())
            .RuleFor(o => o.PublicId, f => Guid.NewGuid().ToString())
            .Generate();
    }

    public static GitHubKeys GetGitHubKeys()
    {
        return new Faker<GitHubKeys>()
            .RuleFor(o => o.AccessToken, f => $"ghp_{f.Random.AlphaNumeric(36)}")
            .RuleFor(o => o.PrivateKey, f => f.Random.AlphaNumeric(64))
            .Generate();
    }

    public static ProjectCreateRequest GetProjectCreateRequest()
    {
        return new Faker<ProjectCreateRequest>()
            .RuleFor(o => o.Name, f => $"{f.Commerce.ProductName()} {Guid.NewGuid():N}")
            .RuleFor(o => o.Description, f => f.Lorem.Sentence())
            .RuleFor(o => o.NewRelicId, f => f.Random.Guid().ToString())
            .RuleFor(o => o.GitHubRepository, f => $"{f.Random.AlphaNumeric(8)}/{f.Lorem.Word()}")
            .RuleFor(o => o.GitHubKeys, f => GetGitHubKeys())
            .RuleFor(o => o.PriorityBranch, f => "main")
            .Generate();
    }

    public static ProjectUpdateRequest GetProjectUpdateRequest()
    {
        return new Faker<ProjectUpdateRequest>()
            .RuleFor(o => o.Name, f => $"{f.Commerce.ProductName()} {Guid.NewGuid():N}")
            .RuleFor(o => o.Description, f => f.Lorem.Sentence())
            .RuleFor(o => o.NewRelicId, f => f.Random.Number(1000, 999999).ToString())
            .RuleFor(o => o.GitHubRepository, f => $"{f.Random.AlphaNumeric(8)}/{f.Lorem.Word()}")
            .RuleFor(o => o.GitHubKeys, f => GetGitHubKeys())
            .RuleFor(o => o.PriorityBranch, f => "develop")
            .Generate();
    }
}
