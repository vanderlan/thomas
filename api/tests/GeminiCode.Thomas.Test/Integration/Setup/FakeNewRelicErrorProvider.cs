using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GeminiCode.Thomas.Domain.Core.Integrations.NewRelic;

namespace GeminiCode.Thomas.Test.Integration.Setup;

/// <summary>
/// Replaces the NewRelic API on the API tests. The events returned by application can be
/// configured by the tests, and every call is recorded so the requested windows can be asserted.
/// </summary>
public sealed class FakeNewRelicErrorProvider : INewRelicErrorProvider
{
    private readonly Dictionary<string, List<NewRelicErrorEvent>> _eventsByApplication = new();
    private readonly List<(string NewRelicId, DateTime Since, DateTime Until)> _calls = [];
    private readonly object _syncRoot = new();

    public IReadOnlyList<(string NewRelicId, DateTime Since, DateTime Until)> Calls
    {
        get
        {
            lock (_syncRoot)
            {
                return _calls.ToList();
            }
        }
    }

    public void SetEvents(string newRelicId, params NewRelicErrorEvent[] events)
    {
        lock (_syncRoot)
        {
            _eventsByApplication[newRelicId] = events.ToList();
        }
    }

    public Task<IReadOnlyList<NewRelicErrorEvent>> GetErrorsAsync(string newRelicId, DateTime since, DateTime until, CancellationToken cancellationToken = default)
    {
        lock (_syncRoot)
        {
            _calls.Add((newRelicId, since, until));

            IReadOnlyList<NewRelicErrorEvent> events = _eventsByApplication.TryGetValue(newRelicId, out var configured)
                ? configured.ToList()
                : new List<NewRelicErrorEvent>();

            return Task.FromResult(events);
        }
    }
}
