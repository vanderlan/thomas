using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Commands.Delete;

public record UserDeleteRequest(string PublicId) : IRequest;
