using GeminiCode.Thomas.Infra.Data.Mongo.Initialization;

namespace GeminiCode.Thomas.Api.Configuration;

public static class MigrationsConfiguration
{
    /// <summary>
    /// MongoDb is schemaless and has no migrations, so the startup only guarantees
    /// that the indexes and the default data exist.
    /// </summary>
    public static WebApplication RunMigrations(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initializer = scope.ServiceProvider.GetRequiredService<MongoDbInitializer>();

        initializer.InitializeAsync().GetAwaiter().GetResult();

        return app;
    }
}
