using GeminiCode.Thomas.Application.Core.Configuration;
using GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncAllProjectsErrors;
using MediatR;
using Microsoft.Extensions.Options;

namespace GeminiCode.Thomas.Api.Workers;

/// <summary>
/// The job that keeps the errors knowledge base up to date: at a fixed interval (5 minutes by
/// default) it collects the errors of every project from NewRelic and compares them with the
/// ones already registered. The executions never overlap: a new one only starts after the
/// previous one finishes and the next tick of the timer elapses.
/// </summary>
public sealed class ErrorLogSyncWorker(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<ErrorLogSyncOptions> options,
    ILogger<ErrorLogSyncWorker> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return RunAsync(stoppingToken);
    }

    /// <summary>
    /// The job loop. It is separated from <see cref="ExecuteAsync"/> so it can be exercised
    /// deterministically by the tests, regardless of how the host schedules the hosted services.
    /// </summary>
    internal async Task RunAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;

        if (!settings.Enabled)
        {
            logger.LogWarning("The error log sync job is disabled ('{SectionName}:{PropertyName}')", ErrorLogSyncOptions.SectionName, nameof(ErrorLogSyncOptions.Enabled));
            return;
        }

        var interval = TimeSpan.FromMinutes(Math.Max(settings.IntervalMinutes, 1));

        logger.LogInformation("The error log sync job is running every {IntervalMinutes} minute(s)", interval.TotalMinutes);

        using var timer = new PeriodicTimer(interval);

        try
        {
            do
            {
                await SyncAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("The error log sync job is stopping");
        }
    }

    /// <summary>
    /// Every execution runs on its own scope, so the scoped/transient dependencies (unit of work,
    /// data context...) are created and disposed per execution.
    /// </summary>
    internal async Task SyncAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = serviceScopeFactory.CreateScope();

            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            await mediator.Send(new SyncAllProjectsErrorsRequest(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            //The job must survive to try again on the next tick.
            logger.LogError(exception, "Error when executing the error log sync job. Error message: {ErrorMessage}", exception.Message);
        }
    }
}
