using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Api.Controllers.V1;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Create;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetById;
using GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetPaginated;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using GeminiCode.Thomas.Test.Shared.Faker;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Api.Controllers.V1;

public class ProjectsControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ProjectsController _projectsController;
    private readonly Project _validProject = ProjectFaker.Get();

    public ProjectsControllerTest()
    {
        var projectList = new List<ProjectGetPaginatedResponse>(3)
        {
            _validProject,
            ProjectFaker.Get(),
            ProjectFaker.Get()
        };

        _mediatorMock.Setup(x => x.Send(It.IsAny<ProjectGetPaginatedRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<ProjectGetPaginatedResponse>(projectList, 3));

        _mediatorMock.Setup(x => x.Send(It.IsAny<ProjectGetByIdRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectGetByIdResponse)_validProject);

        _mediatorMock.Setup(x => x.Send(It.IsAny<ProjectCreateRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectCreateResponse)_validProject);

        _mediatorMock.Setup(x => x.Send(It.IsAny<SyncProjectErrorsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncProjectErrorsResponse { ProjectId = _validProject.PublicId, NewErrors = 2 });

        _projectsController = new ProjectsController(_mediatorMock.Object);
    }

    [Fact]
    public async Task GetProjectById_WithValidId_ReturnsTheProjectWithoutItsKeys()
    {
        //arrange & act
        var result = await _projectsController.Get(_validProject.PublicId);

        var contentResult = (OkObjectResult)result;
        var project = (ProjectGetByIdResponse)contentResult.Value;

        //assert
        Assert.Equal(200, contentResult.StatusCode);
        Assert.Equal(_validProject.Name, project.Name);
        Assert.Equal(_validProject.NewRelicId, project.NewRelicId);
        Assert.Equal(_validProject.GitHubRepository, project.GitHubRepository);
        Assert.True(project.HasGitHubKeys);
        Assert.Null(project.GetType().GetProperty(nameof(Project.GitHubKeys)));
    }

    [Fact]
    public async Task GetProjects_WithValidFilter_ReturnsAListOfProjects()
    {
        //arrange & act
        var result = await _projectsController.Get(new ProjectGetPaginatedRequest());

        var contentResult = (OkObjectResult)result;
        var projectPagedList = (PagedList<ProjectGetPaginatedResponse>)contentResult.Value;

        //assert
        Assert.Equal(3, projectPagedList.Count);
        Assert.Equal(200, contentResult.StatusCode);
    }

    [Fact]
    public async Task PostProject_WithValidData_CreatesAProject()
    {
        //arrange & act
        var result = await _projectsController.Post(ProjectFaker.GetProjectCreateRequest());

        var contentResult = (CreatedResult)result;

        //assert
        Assert.Equal(201, contentResult.StatusCode);
        Assert.IsType<ProjectCreateResponse>(contentResult.Value);
    }

    [Fact]
    public async Task PutProject_WithValidData_UsesTheRouteIdAndReturnsAccepted()
    {
        //arrange
        var request = ProjectFaker.GetProjectUpdateRequest();

        //act
        var result = await _projectsController.Put(_validProject.PublicId, request);

        var contentResult = (AcceptedResult)result;

        //assert
        Assert.Equal(202, contentResult.StatusCode);
        Assert.Equal(_validProject.PublicId, request.PublicId);
    }

    [Fact]
    public async Task DeleteProject_WithExistingId_ReturnsNoContent()
    {
        //arrange & act
        var result = await _projectsController.Delete(_validProject.PublicId);

        var contentResult = (NoContentResult)result;

        //assert
        Assert.Equal(204, contentResult.StatusCode);
    }

    [Fact]
    public async Task PostSyncErrors_WithExistingId_ReturnsTheSynchronizationSummary()
    {
        //arrange & act
        var result = await _projectsController.PostSyncErrors(_validProject.PublicId);

        var contentResult = (OkObjectResult)result;
        var summary = (SyncProjectErrorsResponse)contentResult.Value;

        //assert
        Assert.Equal(200, contentResult.StatusCode);
        Assert.Equal(_validProject.PublicId, summary.ProjectId);
        Assert.Equal(2, summary.NewErrors);
        _mediatorMock.Verify(x => x.Send(It.Is<SyncProjectErrorsRequest>(request => request.ProjectId == _validProject.PublicId), It.IsAny<CancellationToken>()), Times.Once);
    }
}
