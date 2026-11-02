using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncAllProjectsErrors;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Repositories;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using GeminiCode.Thomas.Test.Shared.Faker;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Application.ErrorLogs;

public class SyncAllProjectsErrorsRequestHandlerTest
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly SyncAllProjectsErrorsRequestHandler _handler;

    public SyncAllProjectsErrorsRequestHandlerTest()
    {
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.ProjectRepository).Returns(_projectRepositoryMock.Object);

        _handler = new SyncAllProjectsErrorsRequestHandler(
            _unitOfWorkMock.Object,
            _mediatorMock.Object,
            new Mock<ILogger<SyncAllProjectsErrorsRequestHandler>>().Object);
    }

    [Fact]
    public async Task Handle_SynchronizesEveryProjectOfThePortfolio()
    {
        //arrange
        var projects = new List<Project> { ProjectFaker.Get(), ProjectFaker.Get(), ProjectFaker.Get() };

        _projectRepositoryMock.Setup(repository => repository.ListAllAsync()).ReturnsAsync(projects);

        _mediatorMock.Setup(mediator => mediator.Send(It.IsAny<SyncProjectErrorsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IRequest<SyncProjectErrorsResponse> request, CancellationToken _) => new SyncProjectErrorsResponse { ProjectId = ((SyncProjectErrorsRequest)request).ProjectId });

        //act
        var response = await _handler.Handle(new SyncAllProjectsErrorsRequest(), CancellationToken.None);

        //assert
        Assert.Equal(3, response.SynchronizedProjects.Count);
        Assert.Empty(response.FailedProjects);

        foreach (var project in projects)
        {
            _mediatorMock.Verify(mediator => mediator.Send(It.Is<SyncProjectErrorsRequest>(request => request.ProjectId == project.PublicId), It.IsAny<CancellationToken>()), Times.Once);
            Assert.Contains(response.SynchronizedProjects, result => result.ProjectId == project.PublicId);
        }
    }

    [Fact]
    public async Task Handle_WhenAProjectFails_ContinuesWithTheOthers()
    {
        //arrange
        var failing = ProjectFaker.Get();
        var healthy = ProjectFaker.Get();

        _projectRepositoryMock.Setup(repository => repository.ListAllAsync()).ReturnsAsync([failing, healthy]);

        _mediatorMock.Setup(mediator => mediator.Send(It.Is<SyncProjectErrorsRequest>(request => request.ProjectId == failing.PublicId), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("NewRelic is unavailable"));

        _mediatorMock.Setup(mediator => mediator.Send(It.Is<SyncProjectErrorsRequest>(request => request.ProjectId == healthy.PublicId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncProjectErrorsResponse { ProjectId = healthy.PublicId });

        //act
        var response = await _handler.Handle(new SyncAllProjectsErrorsRequest(), CancellationToken.None);

        //assert
        var synchronized = Assert.Single(response.SynchronizedProjects);
        Assert.Equal(healthy.PublicId, synchronized.ProjectId);
        Assert.Equal(failing.PublicId, Assert.Single(response.FailedProjects));
    }

    [Fact]
    public async Task Handle_WithoutProjects_DoesNothing()
    {
        //arrange
        _projectRepositoryMock.Setup(repository => repository.ListAllAsync()).ReturnsAsync([]);

        //act
        var response = await _handler.Handle(new SyncAllProjectsErrorsRequest(), CancellationToken.None);

        //assert
        Assert.Empty(response.SynchronizedProjects);
        Assert.Empty(response.FailedProjects);
        _mediatorMock.Verify(mediator => mediator.Send(It.IsAny<SyncProjectErrorsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCancelled_StopsAndPropagatesTheCancellation()
    {
        //arrange
        using var cancellationTokenSource = new CancellationTokenSource();

        _projectRepositoryMock.Setup(repository => repository.ListAllAsync()).ReturnsAsync([ProjectFaker.Get(), ProjectFaker.Get()]);

        _mediatorMock.Setup(mediator => mediator.Send(It.IsAny<SyncProjectErrorsRequest>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest<SyncProjectErrorsResponse> _, CancellationToken _) =>
            {
                cancellationTokenSource.Cancel();
                return Task.FromCanceled<SyncProjectErrorsResponse>(cancellationTokenSource.Token);
            });

        //act & assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _handler.Handle(new SyncAllProjectsErrorsRequest(), cancellationTokenSource.Token));
        _mediatorMock.Verify(mediator => mediator.Send(It.IsAny<SyncProjectErrorsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
