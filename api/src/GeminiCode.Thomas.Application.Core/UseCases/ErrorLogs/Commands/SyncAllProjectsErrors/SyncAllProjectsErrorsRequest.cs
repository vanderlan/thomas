using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncAllProjectsErrors;

/// <summary>
/// Synchronizes the errors of every project of the portfolio. It is the request sent by the background job.
/// </summary>
public record SyncAllProjectsErrorsRequest : IRequest<SyncAllProjectsErrorsResponse>;
