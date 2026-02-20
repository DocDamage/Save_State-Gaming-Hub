// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.SmartLauncher;

namespace SaveState.Infrastructure.SmartLauncher;

/// <summary>
/// Background service that monitors gaming sessions and performs cleanup.
/// </summary>
public sealed class SmartLauncherBackgroundService : BackgroundService
{
    private readonly ILogger<SmartLauncherBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ITimeProvider _timeProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30);

    public SmartLauncherBackgroundService(
        ILogger<SmartLauncherBackgroundService> logger,
        IServiceProvider serviceProvider,
        ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Smart Launcher background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckActiveSessionsAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Smart Launcher background service");
                await Task.Delay(_checkInterval, stoppingToken);
            }
        }

        _logger.LogInformation("Smart Launcher background service stopped");
    }

    private async Task CheckActiveSessionsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var sessionRepository = scope.ServiceProvider.GetRequiredService<ILaunchSessionRepository>();
        var launcherService = scope.ServiceProvider.GetRequiredService<ISmartLauncherService>();

        var activeSessionResult = await sessionRepository.GetActiveSessionAsync(ct);
        if (!activeSessionResult.IsSuccess)
            return;

        var activeSession = activeSessionResult.Value;

        // Check if session has been running too long (4 hours warning, 8 hours auto-cleanup)
        var sessionDuration = _timeProvider.UtcNow - activeSession.StartedAt;

        if (sessionDuration > TimeSpan.FromHours(8))
        {
            _logger.LogWarning(
                "Session {SessionId} has been running for {Duration}. Auto-ending session.",
                activeSession.Id, sessionDuration);

            await launcherService.EndSessionAsync(activeSession.Id, ct);
        }
        else if (sessionDuration > TimeSpan.FromHours(4))
        {
            _logger.LogInformation(
                "Session {SessionId} has been running for {Duration}. Consider taking a break!",
                activeSession.Id, sessionDuration);
        }
    }
}
