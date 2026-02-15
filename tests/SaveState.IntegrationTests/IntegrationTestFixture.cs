using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Infrastructure.GameLibrary.Services;
using SaveState.Tests.Fakes;

namespace SaveState.IntegrationTests;

public class IntegrationTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public IntegrationTestFixture()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging();

        // Register services for integration testing
        // Use fake metadata service and wrap it with resilience behavior.
        services.AddScoped<FakeMetadataService>();
        services.AddScoped<IMetadataService>(sp =>
        {
            var inner = sp.GetRequiredService<FakeMetadataService>();
            var logger = sp.GetRequiredService<ILogger<ResilientMetadataService>>();
            return new ResilientMetadataService(inner, logger);
        });

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
