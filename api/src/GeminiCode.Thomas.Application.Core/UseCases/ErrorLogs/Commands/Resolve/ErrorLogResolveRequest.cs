using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Commands.Resolve;

/// <summary>
/// Marks an error of the knowledge base as resolved. If it happens again the sync job reopens it.
/// </summary>
public record ErrorLogResolveRequest(string PublicId) : IRequest;
