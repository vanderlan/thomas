using System;
using System.Threading;
using Testcontainers.MongoDb;

namespace GeminiCode.Thomas.Test.Integration.Setup;

/// <summary>
/// Starts a single throwaway database container for the whole test run.
/// The container is removed automatically when the test process exits.
/// </summary>
public static class TestDatabaseContainer
{
    private static readonly Lazy<string> LazyConnectionString =
        new(StartContainer, LazyThreadSafetyMode.ExecutionAndPublication);

    public static string ConnectionString => LazyConnectionString.Value;

    private static string StartContainer()
    {
        var container = new MongoDbBuilder("mongo:7.0").Build();

        container.StartAsync().GetAwaiter().GetResult();

        return container.GetConnectionString();
    }
}
