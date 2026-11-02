using GeminiCode.Thomas.Api;
using GeminiCode.Thomas.Api.Configuration;
using GeminiCode.Thomas.Api.Filters;
using GeminiCode.Thomas.Api.Middleware;
using FluentValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddValidatorsFromAssemblyContaining<Program>();

builder.Services.AddScoped<AutoValidationActionFilter>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<AutoValidationActionFilter>();
});

builder.Configuration.ConfigureLogging();

builder.Services.ConfigureServices(builder.Configuration);

var app = builder.Build();


app.UseMiddleware<ExceptionMiddleware>();

app.ConfigureApp();

app.RunMigrations();

await app.RunAsync();
