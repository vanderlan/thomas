using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetById;

public record ErrorLogGetByIdRequest(string PublicId) : IRequest<ErrorLogGetByIdResponse>;
