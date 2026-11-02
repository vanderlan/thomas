using System.Text.RegularExpressions;
using GeminiCode.Thomas.Domain.Core.Extensions;

namespace GeminiCode.Thomas.Domain.Core.Services;

/// <summary>
/// Computes the fingerprint that identifies an error on the knowledge base. Two occurrences
/// with the same class and the same message (ignoring the volatile parts of it, like ids,
/// numbers and whitespace) are considered the same error.
/// </summary>
public static partial class ErrorFingerprintGenerator
{
    public const string UnknownErrorClass = "UnknownError";

    public static string Compute(string errorClass, string message)
    {
        var normalizedClass = string.IsNullOrWhiteSpace(errorClass) ? UnknownErrorClass : errorClass.Trim();

        return $"{normalizedClass}|{NormalizeMessage(message)}".ToSha512();
    }

    /// <summary>
    /// Removes the volatile parts of the message (GUIDs, hexadecimal values, numbers and
    /// repeated whitespaces) so different occurrences of the same error produce the same fingerprint.
    /// </summary>
    public static string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        var normalized = GuidPattern().Replace(message, "{guid}");
        normalized = HexadecimalPattern().Replace(normalized, "{hex}");
        normalized = NumberPattern().Replace(normalized, "#");
        normalized = WhitespacePattern().Replace(normalized, " ");

        return normalized.Trim().ToLowerInvariant();
    }

    [GeneratedRegex("[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidPattern();

    [GeneratedRegex(@"\b0x[0-9a-fA-F]+\b")]
    private static partial Regex HexadecimalPattern();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
