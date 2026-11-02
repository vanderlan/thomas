using GeminiCode.Thomas.Domain.Core.Filters;
using GeminiCode.Thomas.Domain.Core.ValueObjects.Pagination;
using MediatR;

namespace GeminiCode.Thomas.Application.Core.UseCases.Projects.Queries.GetPaginated;

public class ProjectGetPaginatedRequest : ProjectFilter, IRequest<PagedList<ProjectGetPaginatedResponse>>;
