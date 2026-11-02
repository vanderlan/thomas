using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GeminiCode.Thomas.Domain.Core.Entities;
using GeminiCode.Thomas.Domain.Core.Integrations.NewRelic;
using GeminiCode.Thomas.Domain.Core.Repositories.UnitOfWork;
using System;
using System.Net.Http;
using GeminiCode.Thomas.Test.Shared.Faker;

namespace GeminiCode.Thomas.Test.Integration.Setup
{
    public class IntegrationTestsFixture
    {
        public readonly HttpClient HttpClient;
        public readonly HttpClient AuthenticatedHttpClient;
        public readonly User DefaultSystemUser;
        public readonly IServiceProvider ServiceProvider;

        /// <summary>
        /// The NewRelic API is replaced by a fake, so the error log synchronization can be tested without external calls.
        /// </summary>
        public readonly FakeNewRelicErrorProvider NewRelicErrorProvider = new();
        private readonly IUnitOfWork _unitOfWork;
        private readonly WebApplicationFactory<Program> _appFactory;
        private string _databaseName;
        private string _connectionString;

        public IntegrationTestsFixture()
        {
            var appFactory = new WebApplicationFactory<Program>()
                                .WithWebHostBuilder(builder =>
                                {
                                    var config = new ConfigurationBuilder()
                                        .AddJsonFile("appsettings.Test.json", optional: false, reloadOnChange: true)
                                        .Build();

                                    SetupMongoDbDatabase(config);

                                    builder.UseConfiguration(config);

                                    builder.ConfigureTestServices(services =>
                                    {
                                        services.RemoveAll<INewRelicErrorProvider>();
                                        services.AddSingleton<INewRelicErrorProvider>(NewRelicErrorProvider);
                                    });
                                });

            ServiceProvider = appFactory.Services;

            _unitOfWork = appFactory.Services.GetRequiredService<IUnitOfWork>();

            HttpClient = appFactory.CreateClient();
            AuthenticatedHttpClient = appFactory.CreateClient();

            DefaultSystemUser = UserFaker.GetDefaultSystemUser();

            _appFactory = appFactory;

            _unitOfWork.UserRepository.AddAsync(DefaultSystemUser).GetAwaiter().GetResult();
            _unitOfWork.CommitAsync().GetAwaiter().GetResult();
        }

        private void SetupMongoDbDatabase(IConfiguration config)
        {
            const string connectionPath = "ConnectionStrings:MongoDb";
            const string databaseNamePath = "MongoDb:DatabaseName";

            //Each fixture runs on its own database of the shared MongoDb container.
            _databaseName = $"ThomasTests{DateTime.UtcNow.Ticks}";

            _connectionString = TestDatabaseContainer.ConnectionString;

            config[connectionPath] = _connectionString;
            config[databaseNamePath] = _databaseName;
        }

        public HttpClient GetNewHttpClient()
        {
            return _appFactory.CreateClient();
        }
    }
}
