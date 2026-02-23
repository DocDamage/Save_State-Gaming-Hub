using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SaveState.Application.RomManagement.RomValidation.Commands;
using SaveState.Core.Ai.Services;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.Interfaces;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.Input.Services;
using SaveState.Core.RgbSync.Services;
using SaveState.Core.RomManagement;
using SaveState.Core.RomManagement.RomValidation.Services;
using SaveState.Core.SaveStates.Services;
using SaveState.Core.Sync.Services;
using SaveState.Core.WebBrowser.Services;
// Note: Using fake services from SaveState.Tests.Fakes instead of infrastructure implementations
using SaveState.Core.Esports.Services;
using SaveState.Core.MobileCompanion.Services;
using SaveState.Core.Theme.Services;
using SaveState.Infrastructure.Input;
using SaveState.Infrastructure.RgbSync;
using SaveState.Infrastructure.RomManagement;
using SaveState.Infrastructure.Theme.Services;
using SaveState.Tests.Fakes;

namespace SaveState.IntegrationTests;

/// <summary>
/// Shared fixture for integration tests providing configured services and test data.
/// </summary>
public class IntegrationTestFixture : IDisposable, IAsyncLifetime
{
    public IServiceProvider ServiceProvider { get; }
    private readonly FakeCloudGamingManagerForTests _cloudGamingManager;

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
        // FileSystem service - using mock since IFileSystem interface location needs verification
        services.AddScoped<IFileSystem>(_ => new Mock<IFileSystem>().Object);
        services.AddScoped<IRomValidationService, RomValidationService>();
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(ValidateRomCommand).Assembly));

        // Register services for integration testing
        // Use fake metadata service
        services.AddScoped<IMetadataService, FakeMetadataService>();

        // Voice Command Service dependencies
        services.AddSingleton<FakeSpeechRecognitionService>();
        services.AddSingleton<ISpeechRecognitionService>(sp => sp.GetRequiredService<FakeSpeechRecognitionService>());

        // Mock complex dependencies
        services.AddSingleton(_ => new Mock<IGameRepository>().Object);
        services.AddSingleton(_ => new Mock<ILaunchExperienceManager>().Object);
        services.AddSingleton(_ => new Mock<ISaveStateManager>().Object);
        // Mock Core.Sync.Services.ICloudGamingManager for VoiceCommandService
        services.AddSingleton<SaveState.Core.Sync.Services.ICloudGamingManager>(_ => 
            new Mock<SaveState.Core.Sync.Services.ICloudGamingManager>().Object);
        services.AddSingleton<IVoiceCommandService, VoiceCommandService>();

        // RGB Sync Service dependencies
        services.AddSingleton<FakeRgbProvider>();
        services.AddSingleton<IEnumerable<IRgbProvider>>(sp => new[] { sp.GetRequiredService<FakeRgbProvider>() });
        services.AddSingleton<IRgbSyncService, RgbSyncService>();

        // Cloud Gaming Services - using fakes that match the test-defined interfaces
        // Note: These implement the test-defined ICloudGamingManager interface (not Core.Sync.Services.ICloudGamingManager)
        services.AddSingleton<FakeCloudGamingManagerForTests>();
        services.AddSingleton<ICloudGamingManager>(sp => sp.GetRequiredService<FakeCloudGamingManagerForTests>());
        services.AddSingleton<FakeNetworkQualityMonitor>();
        services.AddSingleton<INetworkQualityMonitor>(sp => sp.GetRequiredService<FakeNetworkQualityMonitor>());
        services.AddSingleton<FakeCloudCatalogService>(sp => new FakeCloudCatalogService(sp.GetRequiredService<ICloudGamingManager>()));
        services.AddSingleton<ICloudCatalogService>(sp => sp.GetRequiredService<FakeCloudCatalogService>());

        // Theme Service - use real implementation for integration tests
        services.AddSingleton<IThemeService, ThemeService>();

        // Tournament Service - use fake implementation for integration tests
        services.AddSingleton<FakeTournamentService>();
        services.AddSingleton<ITournamentService>(sp => sp.GetRequiredService<FakeTournamentService>());

        // Mobile Companion Services - use fake implementations for integration tests
        services.AddSingleton<FakeMobileCompanionService>();
        services.AddSingleton<IMobileCompanionService>(sp => sp.GetRequiredService<FakeMobileCompanionService>());
        services.AddSingleton<FakeQRCodeService>();
        services.AddSingleton<IQRCodeService>(sp => sp.GetRequiredService<FakeQRCodeService>());
        services.AddSingleton<FakePushNotificationService>();
        services.AddSingleton<IPushNotificationService>(sp => sp.GetRequiredService<FakePushNotificationService>());
        services.AddSingleton<FakeRemoteCommandExecutor>();
        services.AddSingleton<IRemoteCommandExecutor>(sp => sp.GetRequiredService<FakeRemoteCommandExecutor>());

        ServiceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public Task InitializeAsync()
    {
        // Reset the cloud gaming manager state before each test
        var cloudGamingManager = ServiceProvider.GetService<FakeCloudGamingManagerForTests>();
        cloudGamingManager?.ResetConnections();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
