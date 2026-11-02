using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Application.Core.Configuration;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Notifications.ProjectErrorsSynchronized;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;
using GeminiCode.Thomas.Domain.Core.Exceptions;
using GeminiCode.Thomas.Domain.Core.Integrations.NewRelic;
using GeminiCode.Thomas.Domain.Core.Repositories;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using GeminiCode.Thomas.Domain.Core.Services;
using GeminiCode.Thomas.Test.Shared.Faker;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Application.ErrorLogs;

public class SyncProjectErrorsRequestHandlerTest
{
    private static readonly DateTime Now = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IProjectRepository> _projectRepositoryMock = new();
    private readonly Mock<IErrorLogRepository> _errorLogRepositoryMock = new();
    private readonly Mock<INewRelicErrorProvider> _newRelicMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ErrorLogSyncOptions _options = new() { InitialLookbackMinutes = 5 };
    private readonly List<ErrorLog> _addedErrorLogs = [];
    private readonly List<ErrorLog> _updatedErrorLogs = [];
    private readonly Project _project = ProjectFaker.Get();
    private readonly SyncProjectErrorsRequestHandler _handler;

    public SyncProjectErrorsRequestHandlerTest()
    {
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.ProjectRepository).Returns(_projectRepositoryMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.ErrorLogRepository).Returns(_errorLogRepositoryMock.Object);
        _unitOfWorkMock.Setup(unitOfWork => unitOfWork.CommitAsync()).Returns(Task.CompletedTask);

        _projectRepositoryMock.Setup(repository => repository.GetByIdAsync(_project.PublicId)).ReturnsAsync(_project);

        _errorLogRepositoryMock.Setup(repository => repository.FindByFingerprintsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([]);

        _errorLogRepositoryMock.Setup(repository => repository.AddAsync(It.IsAny<ErrorLog>()))
            .Callback((ErrorLog errorLog) => _addedErrorLogs.Add(errorLog))
            .Returns((ErrorLog errorLog) => Task.FromResult(errorLog));

        _errorLogRepositoryMock.Setup(repository => repository.Update(It.IsAny<ErrorLog>()))
            .Callback((ErrorLog errorLog) => _updatedErrorLogs.Add(errorLog));

        _handler = new SyncProjectErrorsRequestHandler(
            _unitOfWorkMock.Object,
            _newRelicMock.Object,
            _mediatorMock.Object,
            Options.Create(_options),
            new FixedTimeProvider(Now),
            new Mock<ILogger<SyncProjectErrorsRequestHandler>>().Object);
    }

    [Fact]
    public async Task Handle_WithAnUnknownProject_ThrowsNotFoundException()
    {
        //arrange
        var unknownId = Guid.NewGuid().ToString();

        //act & assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(new SyncProjectErrorsRequest(unknownId), CancellationToken.None));
        _newRelicMock.Verify(newRelic => newRelic.GetErrorsAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OnTheFirstSync_CollectsTheInitialLookbackWindow()
    {
        //arrange
        _project.LastErrorSyncAt = null;
        SetupNewRelicEvents();

        //act
        var response = await _handler.Handle(new SyncProjectErrorsRequest(_project.PublicId), CancellationToken.None);

        //assert
        Assert.Equal(Now.AddMinutes(-5), response.Since);
        Assert.Equal(Now, response.Until);
        _newRelicMock.Verify(newRelic => newRelic.GetErrorsAsync(_project.NewRelicId, Now.AddMinutes(-5), Now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OnTheNextSyncs_CollectsSinceThePreviousSync()
    {
        //arrange
        var lastSync = Now.AddMinutes(-17);
        _project.LastErrorSyncAt = lastSync;
        SetupNewRelicEvents();

        //act
        var response = await _handler.Handle(new SyncProjectErrorsRequest(_project.PublicId), CancellationToken.None);

        //assert
        Assert.Equal(lastSync, response.Since);
        Assert.Equal(Now, response.Until);
        _newRelicMock.Verify(newRelic => newRelic.GetErrorsAsync(_project.NewRelicId, lastSync, Now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AlwaysUpdatesTheLastSyncOfTheProjectAndCommits()
    {
        //arrange
        SetupNewRelicEvents();

        //act
        await _handler.Handle(new SyncProjectErrorsRequest(_project.PublicId), CancellationToken.None);

        //assert
        Assert.Equal(Now, _project.LastErrorSyncAt);
        _projectRepositoryMock.Verify(repository => repository.Update(_project), Times.Once);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutEvents_DoesNotTouchTheKnowledgeBase()
    {
        //arrange
        SetupNewRelicEvents();

        //act
        var response = await _handler.Handle(new SyncProjectErrorsRequest(_project.PublicId), CancellationToken.None);

        //assert
        Assert.Equal(0, response.EventsProcessed);
        Assert.Equal(0, response.NewErrors);
        Assert.Equal(0, response.UpdatedErrors);
        Assert.Empty(_addedErrorLogs);
        Assert.Empty(_updatedErrorLogs);
        _errorLogRepositoryMock.Verify(repository => repository.FindByFingerprintsAsync(It.IsAny<string>(), It.IsAny<IEnumerable<string>>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnknownErrors_RegistersThemGroupedByFingerprint()
    {
        //arrange
        var firstOccurrence = ErrorLogFaker.GetNewRelicErrorEvent("System.NullReferenceException", "Object reference not set", Now.AddMinutes(-4));
        var secondOccurrence = ErrorLogFaker.GetNewRelicErrorEvent("System.NullReferenceException", "Object reference not set", Now.AddMinutes(-1));
        var otherError = ErrorLogFaker.GetNewRelicErrorEvent("System.TimeoutException", "The request timed out", Now.AddMinutes(-2));

        SetupNewRelicEvents(secondOccurrence, otherError, firstOccurrence);

        //act
        var response = await _handler.Handle(new SyncProjectErrorsRequest(_project.PublicId), CancellationToken.None);

        //assert
        Assert.Equal(3, response.EventsProcessed);
        Assert.Equal(2, response.NewErrors);
        Assert.Equal(0, response.UpdatedErrors);
        Assert.Equal(2, _addedErrorLogs.Count);
        Assert.Empty(_updatedErrorLogs);

        var nullReference = Assert.Single(_addedErrorLogs, errorLog => errorLog.ErrorClass == "System.NullReferenceException");
        Assert.Equal(_project.PublicId, nullReference.ProjectId);
        Assert.Equal(ErrorFingerprintGenerator.Compute("System.NullReferenceException", "Object reference not set"), nullReference.Fingerprint);
        Assert.Equal(ErrorLogStatus.New, nullReference.Status);
        Assert.Equal(2, nullReference.Occurrences);
        Assert.Equal(firstOccurrence.Timestamp, nullReference.FirstSeenAt);
        Assert.Equal(secondOccurrence.Timestamp, nullReference.LastSeenAt);

        //the sample kept is always the most recent occurrence
        Assert.Equal(secondOccurrence.StackTrace, nullReference.StackTrace);
        Assert.Equal(secondOccurrence.TransactionName, nullReference.TransactionName);
        Assert.Equal(secondOccurrence.ErrorGroupId, nullReference.NewRelicErrorGroupId);

        var timeout = Assert.Single(_addedErrorLogs, errorLog => errorLog.ErrorClass == "System.TimeoutException");
        Assert.Equal(1, timeout.Occurrences);
        Assert.Equal(otherError.Timestamp, timeout.FirstSeenAt);
        Assert.Equal(otherError.Timestamp, timeout.LastSeenAt);
    }

    [Fact]
    public async Task Handle_WithKnownErrors_UpdatesTheExistingEntriesInsteadOfDuplicatingThem()
    {
        //arrange
        var known = ErrorLogFaker.Get(_project.PublicId, "System.InvalidOperationException", "Sequence contains no elements");
        known.Status = ErrorLogStatus.New;
        known.Occurrences = 7;
        known.LastSeenAt = Now.AddHours(-1);

        var occurrence = ErrorLogFaker.GetNewRelicErrorEvent("System.InvalidOperationException", "Sequence contains no elements", Now.AddMinutes(-1));
        var unknown = ErrorLogFaker.GetNewRelicErrorEvent("System.ArgumentException", "Value cannot be null", Now.AddMinutes(-3));

        SetupNewRelicEvents(occurrence, unknown);

        IEnumerable<string> requestedFingerprints = null;

        _errorLogRepositoryMock.Setup(repository => repository.FindByFingerprintsAsync(_project.PublicId, It.IsAny<IEnumerable<string>>()))
            .Callback((string _, IEnumerable<string> fingerprints) => requestedFingerprints = fingerprints.ToList())
            .ReturnsAsync([known]);

        //act
        var response = await _handler.Handle(new SyncProjectErrorsRequest(_project.PublicId), CancellationToken.None);

        //assert
        Assert.Equal(1, response.NewErrors);
        Assert.Equal(1, response.UpdatedErrors);

        Assert.Contains(known.Fingerprint, requestedFingerprints);
        Assert.Contains(ErrorFingerprintGenerator.Compute(unknown.ErrorClass, unknown.Message), requestedFingerprints);

        var updated = Assert.Single(_updatedErrorLogs);
        Assert.Same(known, updated);
        Assert.Equal(8, updated.Occurrences);
        Assert.Equal(ErrorLogStatus.Recurring, updated.Status);
        Assert.Equal(occurrence.Timestamp, updated.LastSeenAt);
        Assert.Equal(occurrence.Message, updated.Message);

        var added = Assert.Single(_addedErrorLogs);
        Assert.Equal("System.ArgumentException", added.ErrorClass);
    }

    [Fact]
    public async Task Handle_WithAResolvedErrorThatHappensAgain_ReopensIt()
    {
        //arrange
        var resolved = ErrorLogFaker.Get(_project.PublicId, "System.IO.IOException", "Disk full");
        resolved.Resolve(Now.AddDays(-1));

        SetupNewRelicEvents(ErrorLogFaker.GetNewRelicErrorEvent("System.IO.IOException", "Disk full", Now.AddMinutes(-1)));

        _errorLogRepositoryMock.Setup(repository => repository.FindByFingerprintsAsync(_project.PublicId, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([resolved]);

        //act
        await _handler.Handle(new SyncProjectErrorsRequest(_project.PublicId), CancellationToken.None);

        //assert
        Assert.Equal(ErrorLogStatus.Reopened, resolved.Status);
        Assert.Empty(_addedErrorLogs);
    }

    [Fact]
    public async Task Handle_GroupsTheOccurrencesWithVolatileMessageParts()
    {
        //arrange
        SetupNewRelicEvents(
            ErrorLogFaker.GetNewRelicErrorEvent("System.Exception", "Order 123 not found", Now.AddMinutes(-3)),
            ErrorLogFaker.GetNewRelicErrorEvent("System.Exception", "Order 456 not found", Now.AddMinutes(-2)),
            ErrorLogFaker.GetNewRelicErrorEvent("System.Exception", "Order 789 not found", Now.AddMinutes(-1)));

        //act
        var response = await _handler.Handle(new SyncProjectErrorsRequest(_project.PublicId), CancellationToken.None);

        //assert
        Assert.Equal(1, response.NewErrors);
        var added = Assert.Single(_addedErrorLogs);
        Assert.Equal(3, added.Occurrences);
        Assert.Equal("Order 789 not found", added.Message);
    }

    [Fact]
    public async Task Handle_PublishesTheSynchronizedNotificationAfterTheCommit()
    {
        //arrange
        SetupNewRelicEvents(ErrorLogFaker.GetNewRelicErrorEvent());

        ProjectErrorsSynchronizedNotification published = null;

        _mediatorMock.Setup(mediator => mediator.Publish(It.IsAny<ProjectErrorsSynchronizedNotification>(), It.IsAny<CancellationToken>()))
            .Callback((ProjectErrorsSynchronizedNotification notification, CancellationToken _) => published = notification)
            .Returns(Task.CompletedTask);

        //act
        var response = await _handler.Handle(new SyncProjectErrorsRequest(_project.PublicId), CancellationToken.None);

        //assert
        Assert.NotNull(published);
        Assert.Same(response, published.Result);
        Assert.Equal(_project.PublicId, response.ProjectId);
        Assert.Equal(_project.Name, response.ProjectName);
    }

    [Fact]
    public async Task Handle_WhenNewRelicFails_DoesNotCommitNorUpdateTheLastSync()
    {
        //arrange
        var originalLastSync = _project.LastErrorSyncAt;

        _newRelicMock.Setup(newRelic => newRelic.GetErrorsAsync(It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("NewRelic is unavailable"));

        //act
        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(new SyncProjectErrorsRequest(_project.PublicId), CancellationToken.None));

        //assert
        Assert.Equal(originalLastSync, _project.LastErrorSyncAt);
        _unitOfWorkMock.Verify(unitOfWork => unitOfWork.CommitAsync(), Times.Never);
        _mediatorMock.Verify(mediator => mediator.Publish(It.IsAny<ProjectErrorsSynchronizedNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupNewRelicEvents(params NewRelicErrorEvent[] events)
    {
        _newRelicMock.Setup(newRelic => newRelic.GetErrorsAsync(_project.NewRelicId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<NewRelicErrorEvent>)events);
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
