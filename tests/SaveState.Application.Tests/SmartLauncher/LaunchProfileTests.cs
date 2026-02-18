// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using SaveState.Core.SmartLauncher;

namespace SaveState.Application.Tests.SmartLauncher;

public sealed class LaunchProfileTests
{
    [Fact]
    public void CreatePerformanceProfile_SetsCorrectDefaults()
    {
        // Act
        var profile = LaunchProfile.CreatePerformanceProfile();

        // Assert
        Assert.Equal("Maximum Performance", profile.Name);
        Assert.Equal(ProcessPriority.RealTime, profile.Priority);
        Assert.True(profile.DisableFullscreenOptimizations);
        Assert.True(profile.PerformanceSettings.EnableMemoryOptimization);
        Assert.True(profile.PerformanceSettings.ClearStandbyList);
        Assert.True(profile.PerformanceSettings.DisableVisualEffects);
        Assert.NotEmpty(profile.ProcessesToSuspend);
        Assert.Equal(15, profile.EstimatedPerformanceGain);
    }

    [Fact]
    public void CreateBalancedProfile_SetsCorrectDefaults()
    {
        // Act
        var profile = LaunchProfile.CreateBalancedProfile();

        // Assert
        Assert.Equal("Balanced", profile.Name);
        Assert.Equal(ProcessPriority.High, profile.Priority);
        Assert.True(profile.PerformanceSettings.EnableMemoryOptimization);
        Assert.False(profile.PerformanceSettings.ClearStandbyList);
        Assert.False(profile.PerformanceSettings.DisableVisualEffects);
        Assert.NotEmpty(profile.ProcessesToSuspend);
        Assert.Equal(5, profile.EstimatedPerformanceGain);
    }

    [Fact]
    public void CreatePowerSaverProfile_SetsCorrectDefaults()
    {
        // Act
        var profile = LaunchProfile.CreatePowerSaverProfile();

        // Assert
        Assert.Equal("Power Saver", profile.Name);
        Assert.Equal(ProcessPriority.AboveNormal, profile.Priority);
        Assert.Equal(30, profile.PerformanceSettings.TargetFPS);
        Assert.Equal(-10, profile.EstimatedPerformanceGain);
    }

    [Fact]
    public void LaunchProfile_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var profile = new LaunchProfile();

        // Assert
        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.Equal(ProcessPriority.High, profile.Priority);
        Assert.False(profile.DisableGameMode);
        Assert.True(profile.DisableFullscreenOptimizations);
        Assert.False(profile.RunAsAdministrator);
        Assert.False(profile.DisableWindowsDefender);
        Assert.NotNull(profile.ProcessesToSuspend);
        Assert.NotNull(profile.ServicesToStop);
        Assert.NotNull(profile.PerformanceSettings);
        Assert.True(profile.IsActive);
        Assert.False(profile.IsDefault);
    }

    [Fact]
    public void PerformanceSettings_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new PerformanceSettings();

        // Assert
        Assert.True(settings.EnableMemoryOptimization);
        Assert.True(settings.EnableCPUParking);
        Assert.False(settings.DisableVisualEffects);
        Assert.False(settings.ClearStandbyList);
        Assert.Null(settings.TargetFPS);
        Assert.True(settings.EnableHardwareGPUScheduling);
    }

    [Fact]
    public void DisplaySettings_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var settings = new DisplaySettings();

        // Assert
        Assert.Null(settings.ResolutionWidth);
        Assert.Null(settings.ResolutionHeight);
        Assert.Null(settings.RefreshRate);
        Assert.Null(settings.EnableHDR);
        Assert.True(settings.DisableFullscreenOptimizations);
        Assert.Null(settings.OverrideDPIScaling);
    }
}
