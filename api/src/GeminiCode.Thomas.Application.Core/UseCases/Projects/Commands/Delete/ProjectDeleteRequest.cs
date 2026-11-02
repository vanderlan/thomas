using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Commands.Delete;

public record ProjectDeleteRequest(string PublicId) : IRequest;
