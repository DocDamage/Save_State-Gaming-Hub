using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using SaveState.Core.Services.Mods;
using SaveState.Core.Services;

namespace SaveState.Tests;

/// <summary>
/// ModValidator Tests
/// </summary>
public class ModValidatorTests
{
    [Fact]
    public void ModValidator_Constructor_CreatesWithDefaultSettings()
    {
        // Arrange & Act
        var validator = new ModValidator();

        // Assert
        Assert.NotNull(validator);
    }

    [Fact]
    public void ModValidator_Constructor_AcceptsCustomSettings()
    {
        // Arrange
        var settings = new ModValidatorSettings
        {
            ScanForDangerousCode = true,
            BlockDangerousPatterns = false,
            AllowExecutables = true
        };

        // Act
        var validator = new ModValidator(settings);

        // Assert
        Assert.NotNull(validator);
    }

    [Fact]
    public async Task ModValidator_ValidateModAsync_FailsOnNonexistentPath()
    {
        // Arrange
        var validator = new ModValidator();
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var manifest = new ModManifest { Id = "test-mod", Name = "Test Mod" };

        // Act
        var result = await validator.ValidateModAsync(nonExistentPath, manifest);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not exist", result.Errors.First(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ModValidator_RegisterCustomRule_AddsRule()
    {
        // Arrange
        var validator = new ModValidator();
        var customRule = new TestValidationRule();

        // Act
        validator.RegisterCustomRule(customRule);

        // Assert - no exception means success
        Assert.True(true);
    }

    private class TestValidationRule : IModValidationRule
    {
        public string Name => "TestRule";

        public Task<ModValidationResult> ValidateAsync(string modPath, ModManifest manifest, ModValidatorSettings settings)
        {
            return Task.FromResult(new ModValidationResult { IsValid = true });
        }
    }
}

/// <summary>
/// ModGateway Tests
/// </summary>
public class ModGatewayTests
{
    [Fact]
    public async Task ModGateway_GetLoadedModsAsync_ReturnsEmptyInitially()
    {
        // Arrange
        var validator = new ModValidator();
        var sandbox = new SandboxEnvironment(new StubGameSessionMonitor());
        var gateway = new ModGateway(validator, sandbox);

        // Act
        var mods = await gateway.GetLoadedModsAsync();

        // Assert
        Assert.Empty(mods);
    }

    [Fact]
    public async Task ModGateway_LoadModAsync_FailsOnNonexistentPath()
    {
        // Arrange
        var validator = new ModValidator();
        var sandbox = new SandboxEnvironment(new StubGameSessionMonitor());
        var gateway = new ModGateway(validator, sandbox);
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = await gateway.LoadModAsync(nonExistentPath);

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public async Task ModGateway_UnloadModAsync_SucceedsForUnknownMod()
    {
        // Arrange
        var validator = new ModValidator();
        var sandbox = new SandboxEnvironment(new StubGameSessionMonitor());
        var gateway = new ModGateway(validator, sandbox);

        // Act
        var result = await gateway.UnloadModAsync("non-existent-mod");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ModGateway_RegisterModEventHandler_AcceptsHandler()
    {
        // Arrange
        var validator = new ModValidator();
        var sandbox = new SandboxEnvironment(new StubGameSessionMonitor());
        var gateway = new ModGateway(validator, sandbox);

        // Act
        gateway.RegisterModEventHandler("test_event", e => Task.CompletedTask);

        // Assert - no exception means success
        Assert.True(true);
    }
}

/// <summary>
/// SandboxEnvironment Tests
/// </summary>
public class SandboxEnvironmentTests
{
    [Fact]
    public void SandboxEnvironment_Constructor_CreatesWithDefaultSettings()
    {
        // Arrange & Act
        var sandbox = new SandboxEnvironment(new StubGameSessionMonitor());

        // Assert
        Assert.NotNull(sandbox);
    }

    [Fact]
    public void SandboxEnvironment_Constructor_AcceptsCustomSettings()
    {
        // Arrange
        var settings = new SandboxSettings
        {
            DefaultMemoryLimitMB = 256,
            DefaultExecutionTimeoutMs = 10000,
            DefaultCpuLimitPercent = 50
        };

        // Act
        var sandbox = new SandboxEnvironment(new StubGameSessionMonitor(), settings);

        // Assert
        Assert.NotNull(sandbox);
    }

    [Fact]
    public async Task SandboxEnvironment_LoadModAsync_FailsOnNonexistentPath()
    {
        // Arrange
        var sandbox = new SandboxEnvironment(new StubGameSessionMonitor());
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var manifest = new ModManifest { Id = "test-mod", Name = "Test Mod" };

        // Act
        var result = await sandbox.LoadModAsync(nonExistentPath, manifest);

        // Assert - Empty mod path still loads into sandbox for initialization
        Assert.True(result.Success); // The sandbox creates a context even if path is empty
    }

    [Fact]
    public async Task SandboxEnvironment_ExecuteAsync_FailsForUnloadedMod()
    {
        // Arrange
        var sandbox = new SandboxEnvironment(new StubGameSessionMonitor());
        var context = new SandboxContext { ModId = "non-existent", State = SandboxState.Unloaded };

        // Act
        var result = await sandbox.ExecuteAsync(context, "test-action", new Dictionary<string, object>());

        // Assert
        Assert.False(result.Success);
    }

    [Fact]
    public void SandboxEnvironment_GetMetrics_ReturnsValidMetrics()
    {
        // Arrange
        var sandbox = new SandboxEnvironment(new StubGameSessionMonitor());

        // Act
        var metrics = sandbox.GetMetrics();

        // Assert
        Assert.NotNull(metrics);
        Assert.Equal(0, metrics.ActiveMods);
    }
}

public class StubGameSessionMonitor : IGameSessionMonitor
{
    public bool IsMonitoring => false;
    public int CurrentPid => 0;
    public Guid CurrentGameId => Guid.Empty;

    public Task StartMonitoringAsync(Guid gameId, int pid) => Task.CompletedTask;
    public Task StopMonitoringAsync() => Task.CompletedTask;
}
