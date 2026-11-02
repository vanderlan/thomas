using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace GeminiCode.Thomas.Api.Configuration;

public static class HealthCheckConfiguration
{
    private static readonly string[] DatabaseTags =
            [
                "database"
            ];

    public static void AddApplicationHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {

        //The MongoDb health check uses the IMongoClient registered by AddDatabaseContext.
        services.AddHealthChecks().AddMongoDb(tags: DatabaseTags);

    }

    public static void ConfigureHealthCheck(this IApplicationBuilder app) 
    {
        app.UseHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
        });
    }
}
