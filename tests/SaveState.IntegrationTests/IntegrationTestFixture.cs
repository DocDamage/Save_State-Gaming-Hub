using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Application.RomManagement.RomValidation.Commands;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary.DTOs;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.RomValidation.Services;
using SaveState.Infrastructure.GameLibrary.Services;
using SaveState.Infrastructure.RomManagement;
using SaveState.Infrastructure.Services;
using SaveState.Tests.Fakes;

namespace SaveState.IntegrationTests;

/// <summary>
/// Shared fixture for integration tests providing configured services and test data.
/// </summary>
public class IntegrationTestFixture : IDisposable
{
    public IServiceProvider ServiceProvider { get; }

    public IntegrationTestFixture()
    {
        var services = new ServiceCollection();

        // Add logging
        services.AddLogging();
        services.AddSingleton<ITimeProvider>(_ => SystemTimeProvider.Instance);

        // ROM validation stack for integration tests (in-memory repositories to
        // avoid bringing the entire EF model into this fixture).
        services.AddSingleton<InMemoryRomFileRepository>();
        services.AddSingleton<IRomFileRepository>(sp => sp.GetRequiredService<InMemoryRomFileRepository>());
        services.AddSingleton<IRomHashInfoRepository, InMemoryRomHashInfoRepository>();
        services.AddSingleton<IRomValidationReportRepository, InMemoryRomValidationReportRepository>();
        services.AddScoped<IFileSystem, FileSystem>();
        services.AddScoped<IRomValidationService, RomValidationService>();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ValidateRomCommand).Assembly));

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
