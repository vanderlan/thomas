using Serilog;

namespace GeminiCode.Thomas.Api.Configuration;

public static class LoggingConfiguration
{
    public static void ConfigureLogging(this IConfiguration configuration)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .Enrich.WithEnvironmentName()
            .Enrich.WithCorrelationId()
            .Enrich.WithCorrelationIdHeader()
            .WriteTo.Console(outputTemplate: "[{CorrelationId} - {Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.Debug()
            .Enrich.WithProperty("AppName", configuration["Serilog:AppName"])
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
}