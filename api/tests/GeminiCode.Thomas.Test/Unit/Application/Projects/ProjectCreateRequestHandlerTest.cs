using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;
using GeminiCode.Thomas.Crosscutting.Resources;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Repositories;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using GeminiCode.Thomas.Test.Shared.Faker;
using Microsoft.Extensions.Localization;
using Moq;
using Xunit;
using static GeminiCode.Thomas.Crosscutting.Resources.Messages.MessagesKeys;

namespace GeminiCode.Thomas.Test.Unit.Application.Projects;

public class ProjectCreateRequestHandlerTest
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly ProjectCreateRequestHandler _handler;

    public ProjectCreateRequestHandlerTest()
    {
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.ProjectRepository).Returns(_projectRepositoryMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.CommitAsync()).Returns(Task.CompletedTask);

        _projectRepositoryMock.Setup(repository => repository.AddAsync(It.IsAny<Project>()))
            .Returns((Project project) => Task.FromResult(project));

        var localizerMock = new Mock<IStringLocalizer<ThomasResources>>();
        localizerMock.Setup(localizer => localizer[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));

        _handler = new ProjectCreateRequestHandler(_unitOfWorkMock.Object, localizerMock.Object);
    }

    [Fact]
    public async Task Handle_WithANewName_PersistsTheProjectWithItsKeys()
    {
        //arrange
        var request = ProjectFaker.GetProjectCreateRequest();
        request.Name = $"  {request.Name}  ";

        _projectRepositoryMock.Setup(repository => repository.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((Project)null);

        Project addedProject = null;

        _projectRepositoryMock.Setup(repository => repository.AddAsync(It.IsAny<Project>()))
            .Callback((Project project) => addedProject = project)
            .Returns((Project project) => Task.FromResult(project));

        //act
        var response = await _handler.Handle(request, CancellationToken.None);

        //assert
        Assert.NotNull(addedProject);
        Assert.Equal(request.Name.Trim(), addedProject.Name);
        Assert.Equal(request.Description, addedProject.Description);
        Assert.Equal(request.NewRelicId, addedProject.NewRelicId);
        Assert.Equal(request.GitHubRepository, addedProject.GitHubRepository);
        Assert.Equal(request.PriorityBranch, addedProject.PriorityBranch);
        Assert.Same(request.GitHubKeys, addedProject.GitHubKeys);
        Assert.Null(addedProject.LastErrorSyncAt);

        Assert.Equal(request.Name.Trim(), response.Name);
        Assert.Equal(request.NewRelicId, response.NewRelicId);

        _projectRepositoryMock.Verify(repository => repository.FindByNameAsync(request.Name.Trim()), Times.Once);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAnExistingName_ThrowsConflictException()
    {
        //arrange
        var request = ProjectFaker.GetProjectCreateRequest();

        _projectRepositoryMock.Setup(repository => repository.FindByNameAsync(request.Name))
            .ReturnsAsync(ProjectFaker.Get());

        //act
        var exception = await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(request, CancellationToken.None));

        //assert
        Assert.Equal(ProjectMessages.NameExists, exception.Message);
        Assert.Equal(ExceptionsTitles.ValidationError, exception.Title);
        _projectRepositoryMock.Verify(repository => repository.AddAsync(It.IsAny<Project>()), Times.Never);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.CommitAsync(), Times.Never);
    }
}
