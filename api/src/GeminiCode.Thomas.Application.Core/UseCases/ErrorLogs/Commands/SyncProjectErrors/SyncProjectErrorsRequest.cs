using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.SyncProjectErrors;

/// <summary>
/// Collects the errors of a project from NewRelic and updates its knowledge base.
/// </summary>
public record SyncProjectErrorsRequest(string ProjectId) : IRequest<SyncProjectErrorsResponse>;
