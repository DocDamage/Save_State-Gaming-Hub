// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using SaveState.Core.SmartLauncher;

namespace SaveState.Application.Tests.SmartLauncher;

/// <summary>
/// Performance tests for Smart Launcher operations.
/// </summary>
public sealed class SmartLauncherPerformanceTests
{
    [Fact]
    public void CreateProfile_Balanced_Performance()
    {
        // Arrange & Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < 1000; i++)
        {
            var profile = LaunchProfile.CreateBalancedProfile();
        }
        
        sw.Stop();

        // Assert - Should complete in less than 100ms for 1000 iterations
        Assert.True(sw.ElapsedMilliseconds < 100, $"Creating 1000 profiles took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void CreateProfile_Performance_Performance()
    {
        // Arrange & Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < 1000; i++)
        {
            var profile = LaunchProfile.CreatePerformanceProfile();
        }
        
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 100, $"Creating 1000 performance profiles took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void LaunchResult_Success_Performance()
    {
        // Arrange & Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < 10000; i++)
        {
            var result = LaunchResult.Successful(1234, Guid.NewGuid(), new List<string> { "Priority High" }, 15);
        }
        
        sw.Stop();

        // Assert - Should complete in less than 50ms for 10000 iterations
        Assert.True(sw.ElapsedMilliseconds < 50, $"Creating 10000 launch results took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void CreateLaunchSession_Performance()
    {
        // Arrange & Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        for (int i = 0; i < 10000; i++)
        {
            var session = new LaunchSession
            {
                GameId = Guid.NewGuid(),
                GameName = "Test Game",
                StartedAt = DateTime.UtcNow,
                ProfileId = Guid.NewGuid()
            };
        }
        
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 50, $"Creating 10000 sessions took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void PreviewOptimizations_Performance()
    {
        // Arrange
        var profile = LaunchProfile.CreatePerformanceProfile();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // Act
        for (int i = 0; i < 1000; i++)
        {
            var optimizations = new List<string>();
            optimizations.Add($"Process priority: {profile.Priority}");

            if (profile.PerformanceSettings.EnableMemoryOptimization)
                optimizations.Add("Memory optimization");

            if (profile.PerformanceSettings.ClearStandbyList)
                optimizations.Add("Clear standby memory list");

            if (profile.ProcessesToSuspend.Any())
                optimizations.Add($"Suspend {profile.ProcessesToSuspend.Count} background processes");

            if (profile.ServicesToStop.Any())
                optimizations.Add($"Stop {profile.ServicesToStop.Count} services");

            if (!string.IsNullOrEmpty(profile.PowerPlanGuid))
                optimizations.Add("Switch to high-performance power plan");
        }
        
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 50, $"Previewing optimizations 1000 times took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void CloneProfile_Performance()
    {
        // Arrange
        var original = LaunchProfile.CreateBalancedProfile();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // Act
        for (int i = 0; i < 1000; i++)
        {
            var clone = new LaunchProfile
            {
                Name = $"{original.Name} (Copy)",
                Description = original.Description,
                Priority = original.Priority,
                DisableGameMode = original.DisableGameMode,
                DisableFullscreenOptimizations = original.DisableFullscreenOptimizations,
                RunAsAdministrator = original.RunAsAdministrator,
                ProcessesToSuspend = new List<string>(original.ProcessesToSuspend),
                ServicesToStop = new List<string>(original.ServicesToStop),
                PerformanceSettings = new PerformanceSettings
                {
                    EnableMemoryOptimization = original.PerformanceSettings.EnableMemoryOptimization,
                    EnableCPUParking = original.PerformanceSettings.EnableCPUParking,
                    DisableVisualEffects = original.PerformanceSettings.DisableVisualEffects,
                    ClearStandbyList = original.PerformanceSettings.ClearStandbyList,
                    TargetFPS = original.PerformanceSettings.TargetFPS,
                    EnableHardwareGPUScheduling = original.PerformanceSettings.EnableHardwareGPUScheduling
                }
            };
        }
        
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 100, $"Cloning 1000 profiles took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void ProfileComparison_Performance()
    {
        // Arrange
        var profiles = new[]
        {
            LaunchProfile.CreatePerformanceProfile(),
            LaunchProfile.CreateBalancedProfile(),
            LaunchProfile.CreatePowerSaverProfile()
        };
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // Act
        for (int i = 0; i < 10000; i++)
        {
            var bestPerformance = profiles.MaxBy(p => p.EstimatedPerformanceGain ?? 0);
        }
        
        sw.Stop();

        // Assert
        Assert.True(sw.ElapsedMilliseconds < 50, $"Comparing profiles 10000 times took {sw.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void MemoryAllocation_Profiles()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        
        // Act
        var profiles = new List<LaunchProfile>();
        for (int i = 0; i < 1000; i++)
        {
            profiles.Add(LaunchProfile.CreateBalancedProfile());
        }
        
        var finalMemory = GC.GetTotalMemory(false);
        var allocatedMemory = finalMemory - initialMemory;
        
        // Assert - Should allocate less than 10MB for 1000 profiles
        Assert.True(allocatedMemory < 10 * 1024 * 1024, $"Allocated {allocatedMemory / 1024 / 1024}MB for 1000 profiles");
    }

    [Fact]
    public void MemoryAllocation_Sessions()
    {
        // Arrange
        var initialMemory = GC.GetTotalMemory(true);
        
        // Act
        var sessions = new List<LaunchSession>();
        for (int i = 0; i < 1000; i++)
        {
            sessions.Add(new LaunchSession
            {
                GameId = Guid.NewGuid(),
                GameName = $"Game {i}",
                StartedAt = DateTime.UtcNow.AddDays(-i),
                EndedAt = DateTime.UtcNow.AddDays(-i).AddHours(1)
            });
        }
        
        var finalMemory = GC.GetTotalMemory(false);
        var allocatedMemory = finalMemory - initialMemory;
        
        // Assert - Should allocate less than 5MB for 1000 sessions
        Assert.True(allocatedMemory < 5 * 1024 * 1024, $"Allocated {allocatedMemory / 1024 / 1024}MB for 1000 sessions");
    }

    [Fact]
    public void CalculateSessionDuration_Performance()
    {
        // Arrange
        var session = new LaunchSession
        {
            StartedAt = DateTime.UtcNow.AddHours(-2),
            EndedAt = DateTime.UtcNow
        };
        
        var sw = System.Diagnostics.Stopwatch.StartNew();
        
        // Act
        for (int i = 0; i < 1000000; i++)
        {
            var duration = session.Duration;
        }
        
        sw.Stop();

        // Assert - Should complete in less than 100ms for 1M iterations
        Assert.True(sw.ElapsedMilliseconds < 100, $"Calculating duration 1M times took {sw.ElapsedMilliseconds}ms");
    }
}
