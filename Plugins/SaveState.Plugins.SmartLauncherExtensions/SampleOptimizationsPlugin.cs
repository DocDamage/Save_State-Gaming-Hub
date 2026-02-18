// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.Logging;
using SaveState.Core.SmartLauncher;
using SaveState.Core.SmartLauncher.Plugins;

namespace SaveState.Plugins.SmartLauncherExtensions;

/// <summary>
/// Sample plugin demonstrating Smart Launcher plugin capabilities.
/// This plugin adds custom optimizations for Discord and streaming software.
/// </summary>
public class StreamerModePlugin : ISmartLauncherPlugin
{
    private ILogger<StreamerModePlugin>? _logger;

    public string Name => "Streamer Mode Plugin";
    public string Version => "1.0.0";
    public string Description => "Preserves streaming software while optimizing game performance";

    public Task OnLauncherInitializeAsync(ISmartLauncherContext context, CancellationToken ct = default)
    {
        _logger = context.LoggerFactory.CreateLogger<StreamerModePlugin>();
        _logger.LogInformation("Streamer Mode Plugin initialized");

        // Register custom optimization step
        context.RegisterOptimizationStep(new PreserveStreamingSoftwareStep(context.LoggerFactory));

        // Register custom profile provider
        context.RegisterProfileProvider(new StreamerModeProfileProvider());

        return Task.CompletedTask;
    }

    public Task OnBeforeLaunchAsync(GameLaunchContext context, CancellationToken ct = default)
    {
        _logger?.LogInformation("Streamer Mode: Preparing to launch {GameName}", context.GameName);
        
        // Add metadata for other plugins
        context.Metadata["StreamerMode"] = true;
        context.Metadata["PreservedProcesses"] = new[] { "obs64", "streamlabs", "discord" };

        return Task.CompletedTask;
    }

    public Task OnAfterLaunchAsync(GameLaunchContext context, CancellationToken ct = default)
    {
        _logger?.LogInformation("Streamer Mode: {GameName} launched successfully", context.GameName);
        return Task.CompletedTask;
    }

    public Task OnSessionEndAsync(SessionEndContext context, CancellationToken ct = default)
    {
        _logger?.LogInformation("Streamer Mode: Session ended for {GameName}", context.GameName);
        return Task.CompletedTask;
    }

    public Task OnProfileAppliedAsync(ProfileContext context, CancellationToken ct = default)
    {
        _logger?.LogInformation("Streamer Mode: Profile {ProfileName} applied", context.ProfileName);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Custom optimization step that preserves streaming software.
/// </summary>
public class PreserveStreamingSoftwareStep : IOptimizationStep
{
    private readonly ILogger<PreserveStreamingSoftwareStep> _logger;

    public string Name => "Preserve Streaming Software";
    public string Description => "Keeps OBS, Discord, and other streaming tools running";
    public int Priority => 100; // Run after standard process suspension

    public PreserveStreamingSoftwareStep(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<PreserveStreamingSoftwareStep>();
    }

    public Task<OptimizationResult> ApplyAsync(IOptimizationContext context, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Preserving streaming software processes");

            // List of streaming-related processes to preserve
            var streamingProcesses = new[]
            {
                "obs64", "obs32", "streamlabs", "streamlabs obs",
                "discord", "discordptb", "discordcanary",
                "xsplit", "xsplit.core",
                "nvcontainer", "nvidia share"
            };

            // Store in state for later restoration (though we don't actually suspend these)
            context.State["PreservedProcesses"] = streamingProcesses;

            // Remove these from the profile's suspension list if present
            var processesToSuspend = context.Profile.ProcessesToSuspend;
            var filteredList = processesToSuspend
                .Where(p => !streamingProcesses.Any(s => 
                    p.Contains(s, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // Update the profile's suspension list
            context.Profile.ProcessesToSuspend.Clear();
            context.Profile.ProcessesToSuspend.AddRange(filteredList);

            return Task.FromResult(OptimizationResult.Successful(
                $"Preserved {streamingProcesses.Length} streaming processes"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to preserve streaming software");
            return Task.FromResult(OptimizationResult.Failed(ex.Message));
        }
    }

    public Task<OptimizationResult> RevertAsync(IOptimizationContext context, CancellationToken ct = default)
    {
        // Nothing to revert as we didn't suspend these processes
        return Task.FromResult(OptimizationResult.Successful("No streaming processes to restore"));
    }
}

/// <summary>
/// Custom profile provider for streaming-optimized profiles.
/// </summary>
public class StreamerModeProfileProvider : IProfileProvider
{
    public string Name => "Streamer Mode Provider";

    public Task<IReadOnlyList<LaunchProfile>> GetProfilesForGameAsync(Guid gameId, CancellationToken ct = default)
    {
        var profiles = new List<LaunchProfile>
        {
            CreateStreamerProfile()
        };

        return Task.FromResult<IReadOnlyList<LaunchProfile>>(profiles);
    }

    public Task<LaunchProfile?> GetRecommendedProfileAsync(Guid gameId, CancellationToken ct = default)
    {
        // Could analyze game to determine if streamer mode is recommended
        return Task.FromResult<LaunchProfile?>(CreateStreamerProfile());
    }

    private static LaunchProfile CreateStreamerProfile()
    {
        var profile = LaunchProfile.CreateBalancedProfile();
        profile.Name = "Streamer Mode";
        profile.Description = "Optimized for streaming - preserves OBS, Discord, etc.";
        
        // Don't suspend streaming software
        profile.ProcessesToSuspend = profile.ProcessesToSuspend
            .Where(p => !IsStreamingProcess(p))
            .ToList();

        return profile;
    }

    private static bool IsStreamingProcess(string processName)
    {
        var streamingProcesses = new[] { "obs", "streamlabs", "discord", "xsplit" };
        return streamingProcesses.Any(s => 
            processName.Contains(s, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Another sample plugin for competitive gaming optimizations.
/// </summary>
public class CompetitiveModePlugin : ISmartLauncherPlugin
{
    private ILogger<CompetitiveModePlugin>? _logger;

    public string Name => "Competitive Mode";
    public string Version => "1.0.0";
    public string Description => "Maximum performance optimizations for competitive gaming";

    public Task OnLauncherInitializeAsync(ISmartLauncherContext context, CancellationToken ct = default)
    {
        _logger = context.LoggerFactory.CreateLogger<CompetitiveModePlugin>();
        _logger.LogInformation("Competitive Mode Plugin initialized");
        return Task.CompletedTask;
    }

    public Task OnBeforeLaunchAsync(GameLaunchContext context, CancellationToken ct = default)
    {
        _logger?.LogInformation("Competitive Mode: Maximum performance for {GameName}", context.GameName);
        
        // If using competitive mode, ensure maximum optimizations
        if (context.Profile?.Name.Contains("Competitive", StringComparison.OrdinalIgnoreCase) == true)
        {
            context.Metadata["CompetitiveMode"] = true;
            context.Metadata["PriorityBoost"] = true;
        }

        return Task.CompletedTask;
    }

    public Task OnAfterLaunchAsync(GameLaunchContext context, CancellationToken ct = default)
    {
        if (context.Metadata.TryGetValue("CompetitiveMode", out var value) && value is true)
        {
            _logger?.LogInformation("Competitive Mode: Applied maximum optimizations");
        }
        return Task.CompletedTask;
    }

    public Task OnSessionEndAsync(SessionEndContext context, CancellationToken ct = default)
    {
        _logger?.LogInformation("Competitive Mode: Session ended");
        return Task.CompletedTask;
    }

    public Task OnProfileAppliedAsync(ProfileContext context, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
