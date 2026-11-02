using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using GeminiCode.Thomas.Domain.Core.Integrations.NewRelic;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using GeminiCode.Thomas.Infra.Data.Mongo.Configuration;
using GeminiCode.Thomas.Infra.Data.Mongo.Context;
using GeminiCode.Thomas.Infra.Data.Mongo.Initialization;
using GeminiCode.Thomas.Infra.Data.Mongo.UnitOfWork;
using GeminiCode.Thomas.Infra.NewRelic.Configuration;
using GeminiCode.Thomas.Infra.NewRelic.Providers;
using MongoDB.Driver;

namespace GeminiCode.Thomas.Crosscutting.Ioc.Dependencies;

public static class DependenciesInjectionConfiguration
{
    public static void AddDatabaseContext(this IServiceCollection services)
    {
        services.AddSingleton(serviceProvider =>
            MongoDbOptions.FromConfiguration(serviceProvider.GetRequiredService<IConfiguration>()));

        //The MongoClient handles the connection pool and is designed to be shared by the whole application.
        services.AddSingleton<IMongoClient>(serviceProvider =>
            new MongoClient(serviceProvider.GetRequiredService<MongoDbOptions>().ConnectionString));

        services.AddScoped<MongoDataContext>();
        services.AddScoped<MongoDbInitializer>();
    }

    public static void AddUnitOfWork(this IServiceCollection services)
    {
        services.AddTransient<IUnitOfWork, MongoUnitOfWork>();
    }

    /// <summary>
    /// Registers the NewRelic (NerdGraph API) client used to collect the errors of the projects.
    /// </summary>
    public static void AddNewRelicIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NewRelicOptions>(configuration.GetSection(NewRelicOptions.SectionName));

        services.AddHttpClient<INewRelicErrorProvider, NewRelicErrorProvider>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<NewRelicOptions>>().Value;

            client.Timeout = TimeSpan.FromSeconds(Math.Max(options.TimeoutSeconds, 1));
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        });
    }
}
