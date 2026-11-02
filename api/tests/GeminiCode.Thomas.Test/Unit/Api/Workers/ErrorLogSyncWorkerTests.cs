using System;
using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Api.Workers;
using GeminiCode.Thomas.Application.Core.Configuration;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncAllProjectsErrors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GeminiCode.Thomas.Test.Unit.Api.Workers;

public class ErrorLogSyncWorkerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<ErrorLogSyncWorker>> _loggerMock = new();
    private int _syncCount;

    private ErrorLogSyncWorker CreateWorker(ErrorLogSyncOptions options)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => _mediatorMock.Object);

        return new ErrorLogSyncWorker(
            services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            Options.Create(options),
            _loggerMock.Object);
    }

    private void SetupSuccessfulSync()
    {
        _mediatorMock.Setup(mediator => mediator.Send(It.IsAny<SyncAllProjectsErrorsRequest>(), It.IsAny<CancellationToken>()))
            .Callback(() => Interlocked.Increment(ref _syncCount))
            .ReturnsAsync(new SyncAllProjectsErrorsResponse());
    }

    [Fact]
    public async Task SyncAsync_SendsTheSyncAllProjectsRequestOnItsOwnScope()
    {
        //arrange
        SetupSuccessfulSync();
        var worker = CreateWorker(new ErrorLogSyncOptions());

        //act
        await worker.SyncAsync(CancellationToken.None);

        //assert
        _mediatorMock.Verify(mediator => mediator.Send(It.IsAny<SyncAllProjectsErrorsRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SyncAsync_WhenTheSynchronizationFails_LogsTheErrorAndDoesNotThrow()
    {
        //arrange
        _mediatorMock.Setup(mediator => mediator.Send(It.IsAny<SyncAllProjectsErrorsRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MongoDb is unavailable"));

        var worker = CreateWorker(new ErrorLogSyncOptions());

        //act
        var exception = await Record.ExceptionAsync(() => worker.SyncAsync(CancellationToken.None));

        //assert
        Assert.Null(exception);
        VerifyLog(LogLevel.Error, Times.Once());
    }

    [Fact]
    public async Task SyncAsync_WhenCancelled_PropagatesTheCancellation()
    {
        //arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        _mediatorMock.Setup(mediator => mediator.Send(It.IsAny<SyncAllProjectsErrorsRequest>(), It.IsAny<CancellationToken>()))
            .Returns((IRequest<SyncAllProjectsErrorsResponse> _, CancellationToken token) => Task.FromCanceled<SyncAllProjectsErrorsResponse>(token));

        var worker = CreateWorker(new ErrorLogSyncOptions());

        //act & assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => worker.SyncAsync(cancellationTokenSource.Token));
    }

    [Fact]
    public async Task RunAsync_WhenDisabled_LogsAWarningAndDoesNotSynchronize()
    {
        //arrange
        SetupSuccessfulSync();
        var worker = CreateWorker(new ErrorLogSyncOptions { Enabled = false });

        //act
        await worker.RunAsync(CancellationToken.None);

        //assert
        _mediatorMock.Verify(mediator => mediator.Send(It.IsAny<SyncAllProjectsErrorsRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyLog(LogLevel.Warning, Times.Once());
    }

    [Fact]
    public async Task RunAsync_WhenEnabled_SynchronizesImmediatelyAndStopsGracefullyWhenCancelled()
    {
        //arrange
        SetupSuccessfulSync();
        using var cancellationTokenSource = new CancellationTokenSource();
        var worker = CreateWorker(new ErrorLogSyncOptions { Enabled = true, IntervalMinutes = 5 });

        //act
        var run = worker.RunAsync(cancellationTokenSource.Token);

        //the first synchronization happens right away, the next one only after the 5 minutes tick
        Assert.True(SpinWait.SpinUntil(() => Volatile.Read(ref _syncCount) == 1, TimeSpan.FromSeconds(10)));

        cancellationTokenSource.Cancel();

        var exception = await Record.ExceptionAsync(() => run);

        //assert
        Assert.Null(exception);
        Assert.Equal(1, _syncCount);
    }

    [Fact]
    public async Task StartAsync_AndStopAsync_CompleteTheHostedServiceLifecycle()
    {
        //arrange
        SetupSuccessfulSync();
        var worker = CreateWorker(new ErrorLogSyncOptions { Enabled = true, IntervalMinutes = 5 });

        //act
        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        //assert: the execution finished without faulting, regardless of how the host scheduled it
        Assert.NotNull(worker.ExecuteTask);
        var exception = await Record.ExceptionAsync(() => worker.ExecuteTask);
        Assert.Null(exception);
        Assert.True(worker.ExecuteTask.IsCompletedSuccessfully);
    }

    private void VerifyLog(LogLevel logLevel, Times times)
    {
        _loggerMock.Verify(logger => logger.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, type) => true),
                It.IsAny<Exception>(),
                (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()),
            times);
    }
}
