using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GeminiCode.Thomas.Domain.Core.Integrations.NewRelic;

/// <summary>
/// Reads the errors of an application monitored by NewRelic.
/// </summary>
public interface INewRelicErrorProvider
{
    /// <summary>
    /// Returns the error occurrences of the application identified by <paramref name="newRelicId"/>
    /// that happened between <paramref name="since"/> (inclusive) and <paramref name="until"/> (exclusive), both in UTC.
    /// </summary>
    Task<IReadOnlyList<NewRelicErrorEvent>> GetErrorsAsync(string newRelicId, DateTime since, DateTime until, CancellationToken cancellationToken = default);
}
