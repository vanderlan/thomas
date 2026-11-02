using System;
using Bogus;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Entities.Enuns;
using GeminiCode.Thomas.Domain.Core.Integrations.NewRelic;
using GeminiCode.Thomas.Domain.Core.Services;

namespace GeminiCode.Thomas.Test.Shared.Faker;

public static class ErrorLogFaker
{
    public static ErrorLog Get(string projectId = null, string errorClass = null, string message = null)
    {
        var faker = new Bogus.Faker();

        var resolvedClass = errorClass ?? $"System.{faker.Hacker.Noun()}Exception";
        var resolvedMessage = message ?? faker.Hacker.Phrase();
        var lastSeenAt = TruncateToMilliseconds(DateTime.UtcNow);

        return new ErrorLog
        {
            PublicId = Guid.NewGuid().ToString(),
            ProjectId = projectId ?? Guid.NewGuid().ToString(),
            Fingerprint = ErrorFingerprintGenerator.Compute(resolvedClass, resolvedMessage),
            ErrorClass = resolvedClass,
            Message = resolvedMessage,
            StackTrace = $"   at {faker.Hacker.Verb()}() in {faker.System.FilePath()}:line {faker.Random.Number(1, 500)}",
            TransactionName = $"WebTransaction/MVC/{faker.Hacker.Noun()}/Get",
            Status = ErrorLogStatus.New,
            Occurrences = faker.Random.Number(1, 50),
            FirstSeenAt = lastSeenAt.AddMinutes(-faker.Random.Number(1, 120)),
            LastSeenAt = lastSeenAt
        };
    }

    public static NewRelicErrorEvent GetNewRelicErrorEvent(string errorClass = null, string message = null, DateTime? timestamp = null)
    {
        var faker = new Bogus.Faker();

        return new NewRelicErrorEvent
        {
            Timestamp = timestamp ?? TruncateToMilliseconds(DateTime.UtcNow),
            ErrorClass = errorClass ?? $"System.{faker.Hacker.Noun()}Exception",
            Message = message ?? faker.Hacker.Phrase(),
            TransactionName = $"WebTransaction/MVC/{faker.Hacker.Noun()}/Get",
            StackTrace = $"   at {faker.Hacker.Verb()}() in {faker.System.FilePath()}:line {faker.Random.Number(1, 500)}",
            ErrorGroupId = faker.Random.AlphaNumeric(24)
        };
    }

    private static DateTime TruncateToMilliseconds(DateTime dateTime)
    {
        return new DateTime(dateTime.Ticks - (dateTime.Ticks % TimeSpan.TicksPerMillisecond), DateTimeKind.Utc);
    }
}
