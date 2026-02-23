// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.SmartLauncher;
using SaveState.Infrastructure.Persistence;
using SaveState.Infrastructure.Repositories;
using SaveState.Infrastructure.SmartLauncher;

namespace SaveState.IntegrationTests.SmartLauncher;

public sealed class SmartLauncherIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SaveStateDbContext _dbContext;
    private readonly ISmartLauncherService _launcherService;
    private readonly ILaunchProfileRepository _profileRepository;
    private readonly ILaunchSessionRepository _sessionRepository;

    public SmartLauncherIntegrationTests()
    {
        var services = new ServiceCollection();

        // Database
        services.AddDbContext<SaveStateDbContext>(options =>
            options.UseInMemoryDatabase($"SmartLauncherTest_{Guid.NewGuid()}"));

        // Logging
        services.AddLogging();
        services.AddSingleton<ITimeProvider>(_ => SystemTimeProvider.Instance);

        // Services
        services.AddScoped<ILaunchProfileRepository, LaunchProfileRepository>();
        services.AddScoped<ILaunchSessionRepository, LaunchSessionRepository>();
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddSingleton<IGameProcessMonitor, GameProcessMonitor>();

        _serviceProvider = services.BuildServiceProvider();

        _dbContext = _serviceProvider.GetRequiredService<SaveStateDbContext>();
        _profileRepository = _serviceProvider.GetRequiredService<ILaunchProfileRepository>();
        _sessionRepository = _serviceProvider.GetRequiredService<ILaunchSessionRepository>();

        // Note: SmartLauncherService would need mocks for full integration testing
        // This is a simplified integration test setup
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task CreateAndRetrieveProfile_Integration()
    {
        // Arrange
        var profile = LaunchProfile.CreateBalancedProfile();
        profile.Name = "Test Profile";

        // Act
        await _profileRepository.SaveProfileAsync(profile);
        var result = await _profileRepository.GetProfileAsync(profile.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Test Profile", result.Value.Name);
        Assert.Equal(ProcessPriority.High, result.Value.Priority);
    }

    [Fact]
    public async Task CreateAndRetrieveSession_Integration()
    {
        // Arrange
        var session = new LaunchSession
        {
            GameId = Guid.NewGuid(),
            GameName = "Test Game",
            StartedAt = DateTime.UtcNow
        };

        // Act
        await _sessionRepository.CreateSessionAsync(session);
        var result = await _sessionRepository.GetSessionAsync(session.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Test Game", result.Value.GameName);
        Assert.True(result.Value.IsActive);
    }

    [Fact]
    public async Task GetActiveSession_ReturnsOnlyActive()
    {
        // Arrange
        var activeSession = new LaunchSession
        {
            GameId = Guid.NewGuid(),
            GameName = "Active Game",
            StartedAt = DateTime.UtcNow
        };

        var endedSession = new LaunchSession
        {
            GameId = Guid.NewGuid(),
            GameName = "Ended Game",
            StartedAt = DateTime.UtcNow.AddHours(-2),
            EndedAt = DateTime.UtcNow.AddHours(-1)
        };

        await _sessionRepository.CreateSessionAsync(activeSession);
        await _sessionRepository.CreateSessionAsync(endedSession);

        // Act
        var result = await _sessionRepository.GetActiveSessionAsync();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Active Game", result.Value.GameName);
    }

    [Fact]
    public async Task GetLaunchHistory_ReturnsCompletedSessions()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        
        for (int i = 0; i < 5; i++)
        {
            var session = new LaunchSession
            {
                GameId = gameId,
                GameName = "Test Game",
                StartedAt = DateTime.UtcNow.AddDays(-i),
                EndedAt = DateTime.UtcNow.AddDays(-i).AddHours(1)
            };
            await _sessionRepository.CreateSessionAsync(session);
        }

        // Act
        var history = await _sessionRepository.GetLaunchHistoryAsync(gameId, 10);

        // Assert
        Assert.Equal(5, history.Count);
        Assert.All(history, s => Assert.NotNull(s.EndedAt));
    }

    [Fact]
    public async Task UpdateProfile_ModifiesExisting()
    {
        // Arrange
        var profile = LaunchProfile.CreateBalancedProfile();
        profile.Name = "Original Name";
        await _profileRepository.SaveProfileAsync(profile);

        // Act
        profile.Name = "Updated Name";
        profile.Priority = ProcessPriority.RealTime;
        await _profileRepository.SaveProfileAsync(profile);

        var result = await _profileRepository.GetProfileAsync(profile.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Name", result.Value.Name);
        Assert.Equal(ProcessPriority.RealTime, result.Value.Priority);
    }

    [Fact]
    public async Task DeleteProfile_SoftDeletes()
    {
        // Arrange
        var profile = LaunchProfile.CreateBalancedProfile();
        await _profileRepository.SaveProfileAsync(profile);

        // Act
        await _profileRepository.DeleteProfileAsync(profile.Id);
        var result = await _profileRepository.GetProfileAsync(profile.Id);

        // Assert
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task SetDefaultProfile_UpdatesDefault()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var profile1 = LaunchProfile.CreateBalancedProfile();
        var profile2 = LaunchProfile.CreatePerformanceProfile();
        
        await _profileRepository.SaveProfileAsync(profile1);
        await _profileRepository.SaveProfileAsync(profile2);

        // Act
        await _profileRepository.SetDefaultProfileAsync(gameId, profile2.Id);
        var result = await _profileRepository.GetDefaultProfileAsync(gameId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(profile2.Id, result.Value.Id);
    }

    [Fact]
    public async Task EndSession_SetsEndTimeAndDuration()
    {
        // Arrange
        var session = new LaunchSession
        {
            GameId = Guid.NewGuid(),
            GameName = "Test Game",
            StartedAt = DateTime.UtcNow.AddHours(-1)
        };
        await _sessionRepository.CreateSessionAsync(session);

        // Act
        await _sessionRepository.EndSessionAsync(session.Id, 0, null);
        var result = await _sessionRepository.GetSessionAsync(session.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.EndedAt);
        Assert.NotNull(result.Value.Duration);
        Assert.Equal(0, result.Value.ExitCode);
        Assert.False(result.Value.IsActive);
    }

    [Fact]
    public async Task GetProfiles_FiltersByGameId()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var globalProfile = LaunchProfile.CreateBalancedProfile();
        var gameProfile = LaunchProfile.CreatePerformanceProfile();
        gameProfile.GameId = gameId;

        await _profileRepository.SaveProfileAsync(globalProfile);
        await _profileRepository.SaveProfileAsync(gameProfile);

        // Act
        var profiles = await _profileRepository.GetProfilesAsync(gameId);

        // Assert
        Assert.Equal(2, profiles.Count);
        Assert.Contains(profiles, p => p.GameId == null); // Global
        Assert.Contains(profiles, p => p.GameId == gameId);
    }

    [Fact]
    public async Task ProfilePerformanceSettings_Persisted()
    {
        // Arrange
        var profile = new LaunchProfile
        {
            Name = "Test Profile",
            PerformanceSettings = new PerformanceSettings
            {
                EnableMemoryOptimization = true,
                ClearStandbyList = true,
                DisableVisualEffects = true,
                TargetFPS = 60
            }
        };

        // Act
        await _profileRepository.SaveProfileAsync(profile);
        var result = await _profileRepository.GetProfileAsync(profile.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.PerformanceSettings.EnableMemoryOptimization);
        Assert.True(result.Value.PerformanceSettings.ClearStandbyList);
        Assert.True(result.Value.PerformanceSettings.DisableVisualEffects);
        Assert.Equal(60, result.Value.PerformanceSettings.TargetFPS);
    }

    [Fact]
    public async Task SessionPerformanceMetrics_Persisted()
    {
        // Arrange
        var session = new LaunchSession
        {
            GameId = Guid.NewGuid(),
            GameName = "Test Game",
            StartedAt = DateTime.UtcNow,
            PerformanceMetrics = new SessionPerformanceMetrics
            {
                AverageFPS = 60.5,
                MinFPS = 30.0,
                MaxFPS = 144.0,
                AverageCPUUsage = 45.5,
                PeakMemoryMB = 4096
            }
        };

        // Act
        await _sessionRepository.CreateSessionAsync(session);
        var result = await _sessionRepository.GetSessionAsync(session.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.PerformanceMetrics);
        Assert.Equal(60.5, result.Value.PerformanceMetrics.AverageFPS);
        Assert.Equal(30.0, result.Value.PerformanceMetrics.MinFPS);
        Assert.Equal(144.0, result.Value.PerformanceMetrics.MaxFPS);
        Assert.Equal(45.5, result.Value.PerformanceMetrics.AverageCPUUsage);
        Assert.Equal(4096, result.Value.PerformanceMetrics.PeakMemoryMB);
    }
}
