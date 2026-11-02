using GeminiCode.Thomas.Domain.Core.Entities.Enuns;

namespace GeminiCode.Thomas.Domain.Core.Filters;

public class ErrorLogFilter : BaseFilter
{
    public string ProjectId { get; init; }
    public ErrorLogStatus? Status { get; init; }
}
