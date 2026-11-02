using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Users.Queries.GetById;

public record UserGetByIdRequest(string PublicId) : IRequest<UserGetByIdResponse>;
