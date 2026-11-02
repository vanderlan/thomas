using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetById;

public record ProjectGetByIdRequest(string PublicId) : IRequest<ProjectGetByIdResponse>;
