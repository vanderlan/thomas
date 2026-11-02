using System.Linq;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;
using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Test.Shared.Faker;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;
using static GeminiCode.Thomas.Crosscutting.Resources.Messages.MessagesKeys;

namespace GeminiCode.Thomas.Test.Unit.Application.Projects;

public class ProjectCreateRequestValidatorTest
{
    private readonly ProjectCreateRequestValidator _validator;

    public ProjectCreateRequestValidatorTest()
    {
        var localizerMock = new Mock<IStringLocalizer<ThomasResources>>();
        localizerMock.Setup(localizer => localizer[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        _validator = new ProjectCreateRequestValidator(localizerMock.Object);
    }

    [Fact]
    public void Validate_WithAValidRequest_IsValid()
    {
        //arrange & act
        var result = _validator.Validate(ProjectFaker.GetProjectCreateRequest());

        //assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithAnEmptyRequest_ReturnsAllTheRequiredFieldsErrors()
    {
        //arrange & act
        var result = _validator.Validate(new ProjectCreateRequest());

        var messages = result.Errors.Select(error => error.ErrorMessage).ToList();

        //assert
        Assert.False(result.IsValid);
        Assert.Contains(ProjectMessages.EmptyName, messages);
        Assert.Contains(ProjectMessages.EmptyNewRelicId, messages);
        Assert.Contains(ProjectMessages.EmptyGitHubRepository, messages);
        Assert.Contains(ProjectMessages.EmptyGitHubKeys, messages);
        Assert.Contains(ProjectMessages.EmptyPriorityBranch, messages);
        Assert.DoesNotContain(ProjectMessages.EmptyGitHubAccessToken, messages);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("owner/")]
    [InlineData("/repository")]
    [InlineData("owner/repo/extra")]
    [InlineData("https://github.com/owner/repository")]
    [InlineData("owner/my repo")]
    public void Validate_WithAnInvalidRepository_ReturnsTheFormatError(string repository)
    {
        //arrange
        var request = ProjectFaker.GetProjectCreateRequest();
        request.GitHubRepository = repository;

        //act
        var result = _validator.Validate(request);

        //assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == ProjectMessages.InvalidGitHubRepository);
    }

    [Theory]
    [InlineData("owner/repository")]
    [InlineData("my-org/my.repo_name")]
    [InlineData("vanderlan/Thomas-Api")]
    public void Validate_WithAValidRepository_IsValid(string repository)
    {
        //arrange
        var request = ProjectFaker.GetProjectCreateRequest();
        request.GitHubRepository = repository;

        //act
        var result = _validator.Validate(request);

        //assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Validate_WithKeysWithoutAccessToken_ReturnsTheAccessTokenError(string accessToken)
    {
        //arrange
        var request = ProjectFaker.GetProjectCreateRequest();
        request.GitHubKeys = new GitHubKeys { AccessToken = accessToken };

        //act
        var result = _validator.Validate(request);

        //assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.ErrorMessage == ProjectMessages.EmptyGitHubAccessToken);
    }
}
