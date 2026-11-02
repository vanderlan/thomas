using GeminiCode.Thomas.Api.Attributes;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GeminiCode.Thomas.Api.Configuration;
using GeminiCode.Thomas.Api.Workers;
using GeminiCode.Thomas.Application.Core;
using GeminiCode.Thomas.Application.Core.Configuration;
using GeminiCode.Thomas.Application.Core.UseCases.Users.Commands.Create;
using GeminiCode.Thomas.Crosscutting.Ioc.Dependencies;
using GeminiCode.Thomas.Domain.Core.Authentication;

namespace GeminiCode.Thomas.Api;

public static class Bootstrapper
{
    public static void ConfigureApp(this IApplicationBuilder app)
    {
        app.UseSwagger();
        
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("v1/swagger.json", "Thomas API");
        });

        app.UseCors(options => options.WithOrigins("*")
            .AllowAnyMethod()
            .AllowAnyOrigin()
            .AllowAnyHeader());

        app.ConfigureGlobalization();

        app.ConfigureHealthCheck();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
        });
    }

    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors();

        services.ConfigureAuthentication(configuration);

        services.AddControllers();

        services.AddValidatorsFromAssemblyContaining<UserCreateRequestValidator>();

        services.ConfigureApiVersioning();

        services.AddMvc(options =>
                        {
                            options.Filters.Add<CustomValidationAttribute>();
                        })
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();

        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.SuppressModelStateInvalidFilter = true;
        });

        services.AddLocalization(options => options.ResourcesPath = @"Resources");
        services.AddApplicationHealthChecks(configuration);

        services.ConfigureSwagger();

        services.AddDatabaseContext();
        services.AddUnitOfWork();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(IApplicationAssembly).Assembly));

        services.ConfigureErrorLogSync(configuration);
    }

    /// <summary>
    /// Registers the NewRelic integration and the job that keeps the errors knowledge base up to date.
    /// </summary>
    public static void ConfigureErrorLogSync(this IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.Configure<ErrorLogSyncOptions>(configuration.GetSection(ErrorLogSyncOptions.SectionName));

        services.AddNewRelicIntegration(configuration);

        services.AddHostedService<ErrorLogSyncWorker>();
    }
}
