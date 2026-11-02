using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;

namespace GeminiCode.Thomas.Application.Core.UseCases.ErrorLogs.Queries.GetById;

public class ErrorLogGetByIdResponse
{
    public string PublicId { get; set; }
    public string ProjectId { get; set; }
    public string Fingerprint { get; set; }
    public string ErrorClass { get; set; }
    public string Message { get; set; }
    public string StackTrace { get; set; }
    public string TransactionName { get; set; }
    public string NewRelicErrorGroupId { get; set; }
    public ErrorLogStatus Status { get; set; }
    public int Occurrences { get; set; }
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public DateTime CreatedAt { get; set; }

    public static explicit operator ErrorLogGetByIdResponse(ErrorLog errorLog)
    {
        if (errorLog is null)
            return default;

        return new ErrorLogGetByIdResponse
        {
            PublicId = errorLog.PublicId,
            ProjectId = errorLog.ProjectId,
            Fingerprint = errorLog.Fingerprint,
            ErrorClass = errorLog.ErrorClass,
            Message = errorLog.Message,
            StackTrace = errorLog.StackTrace,
            TransactionName = errorLog.TransactionName,
            NewRelicErrorGroupId = errorLog.NewRelicErrorGroupId,
            Status = errorLog.Status,
            Occurrences = errorLog.Occurrences,
            FirstSeenAt = errorLog.FirstSeenAt,
            LastSeenAt = errorLog.LastSeenAt,
            ResolvedAt = errorLog.ResolvedAt,
            LastUpdated = errorLog.LastUpdated,
            CreatedAt = errorLog.CreatedAt
        };
    }
}
