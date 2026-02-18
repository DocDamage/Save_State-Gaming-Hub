// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using SaveState.Core.SmartLauncher;

namespace SaveState.Application.Tests.SmartLauncher;

public sealed class LaunchResultTests
{
    [Fact]
    public void Successful_CreatesSuccessResult()
    {
        // Arrange
        var processId = 1234;
        var sessionId = Guid.NewGuid();
        var optimizations = new List<string> { "Priority High", "Memory optimized" };
        var performanceGain = 10;

        // Act
        var result = LaunchResult.Successful(processId, sessionId, optimizations, performanceGain);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(processId, result.ProcessId);
        Assert.Equal(sessionId, result.SessionId);
        Assert.Equal(optimizations, result.AppliedOptimizations);
        Assert.Equal(performanceGain, result.EstimatedPerformanceGain);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Failed_CreatesFailureResult()
    {
        // Arrange
        var errorMessage = "Game executable not found";

        // Act
        var result = LaunchResult.Failed(errorMessage);

        // Assert
        Assert.False(result.Success);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.Null(result.ProcessId);
        Assert.Null(result.SessionId);
        Assert.Empty(result.AppliedOptimizations);
    }

    [Fact]
    public void LaunchSession_IsActive_CalculatedCorrectly()
    {
        // Arrange
        var session = new LaunchSession
        {
            StartedAt = DateTime.UtcNow.AddHours(-1),
            EndedAt = null
        };

        // Assert
        Assert.True(session.IsActive);
        Assert.Null(session.Duration);
    }

    [Fact]
    public void LaunchSession_Duration_CalculatedCorrectly()
    {
        // Arrange
        var startedAt = DateTime.UtcNow.AddHours(-2);
        var endedAt = DateTime.UtcNow;
        var session = new LaunchSession
        {
            StartedAt = startedAt,
            EndedAt = endedAt
        };

        // Assert
        Assert.False(session.IsActive);
        Assert.NotNull(session.Duration);
        Assert.True(session.Duration.Value.TotalHours >= 1.9);
    }

    [Fact]
    public void SystemState_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var state = new SystemState();

        // Assert
        Assert.Null(state.PowerPlanGuid);
        Assert.NotNull(state.SuspendedProcesses);
        Assert.NotNull(state.StoppedServices);
        Assert.True(state.VisualEffectsEnabled);
    }

    [Fact]
    public void SessionPerformanceMetrics_DefaultValues_AreNull()
    {
        // Arrange & Act
        var metrics = new SessionPerformanceMetrics();

        // Assert
        Assert.Null(metrics.AverageFPS);
        Assert.Null(metrics.MinFPS);
        Assert.Null(metrics.MaxFPS);
        Assert.Null(metrics.AverageCPUUsage);
        Assert.Null(metrics.AverageGPUUsage);
        Assert.Null(metrics.PeakMemoryMB);
        Assert.Null(metrics.AverageTemperature);
    }

    [Fact]
    public void GameProcessExitedEventArgs_PropertiesSetCorrectly()
    {
        // Arrange
        var args = new GameProcessExitedEventArgs
        {
            SessionId = Guid.NewGuid(),
            ProcessId = 1234,
            ExitCode = 0,
            ExitTime = DateTime.UtcNow
        };

        // Assert
        Assert.NotEqual(Guid.Empty, args.SessionId);
        Assert.Equal(1234, args.ProcessId);
        Assert.Equal(0, args.ExitCode);
        Assert.True(args.ExitTime > DateTime.MinValue);
    }

    [Theory]
    [InlineData(ProcessPriority.Low)]
    [InlineData(ProcessPriority.BelowNormal)]
    [InlineData(ProcessPriority.Normal)]
    [InlineData(ProcessPriority.AboveNormal)]
    [InlineData(ProcessPriority.High)]
    [InlineData(ProcessPriority.RealTime)]
    public void ProcessPriority_AllValues_AreValid(ProcessPriority priority)
    {
        // Arrange
        var profile = new LaunchProfile { Priority = priority };

        // Assert
        Assert.Equal(priority, profile.Priority);
    }
}
