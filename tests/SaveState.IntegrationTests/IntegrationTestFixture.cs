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
        // Use fake metadata service for testing
        services.AddScoped<IMetadataService, FakeMetadataService>();

        // Wrap with resilient service manually (since we don't have Scrutor in tests)
        services.AddScoped<ResilientMetadataService>(sp =>
        {
            var inner = sp.GetRequiredService<IMetadataService>();
            var logger = sp.GetRequiredService<ILogger<ResilientMetadataService>>();
            return new ResilientMetadataService(inner, logger);
        });

        // Override the IMetadataService to return the resilient wrapper
        services.AddScoped<IMetadataService>(sp => sp.GetRequiredService<ResilientMetadataService>());

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
