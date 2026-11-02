using GeminiCode.Thomas.Domain.Core.Filters;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetPaginated;

public class ErrorLogGetPaginatedRequest : ErrorLogFilter, IRequest<PagedList<ErrorLogGetPaginatedResponse>>;
