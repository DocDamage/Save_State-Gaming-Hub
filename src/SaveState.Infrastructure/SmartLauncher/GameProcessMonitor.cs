// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.SmartLauncher;

namespace SaveState.Infrastructure.SmartLauncher;

/// <summary>
/// Monitors game process and collects performance metrics.
/// </summary>
public sealed class GameProcessMonitor : IGameProcessMonitor, IDisposable
{
    private readonly ILogger<GameProcessMonitor> _logger;
    private readonly ITimeProvider _timeProvider;
    private Process? _gameProcess;
    private Guid _sessionId;
    private readonly List<double> _cpuReadings = new();
    private readonly List<long> _memoryReadings = new();
    private DateTime _monitoringStartTime;
    private bool _isMonitoring;
    private readonly System.Timers.Timer _metricsTimer;

    public event EventHandler<GameProcessExitedEventArgs>? ProcessExited;

    public GameProcessMonitor(ILogger<GameProcessMonitor> logger, ITimeProvider timeProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _metricsTimer = new System.Timers.Timer(1000); // 1 second interval
        _metricsTimer.Elapsed += async (s, e) => await CollectMetricsAsync();
    }

    /// <inheritdoc />
    public Task StartMonitoringAsync(int processId, Guid sessionId, CancellationToken ct = default)
    {
        try
        {
            _gameProcess = Process.GetProcessById(processId);
            _sessionId = sessionId;
            _monitoringStartTime = _timeProvider.UtcNow;
            _isMonitoring = true;

            // Enable performance counters
            _gameProcess.EnableRaisingEvents = true;
            _gameProcess.Exited += OnProcessExited;

            // Start metrics collection timer
            _metricsTimer.Start();

            _logger.LogInformation("Started monitoring game process {ProcessId} for session {SessionId}",
                processId, sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start monitoring process {ProcessId}", processId);
            throw;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<SessionPerformanceMetrics> StopMonitoringAsync(CancellationToken ct = default)
    {
        _isMonitoring = false;
        _metricsTimer.Stop();

        var metrics = CalculateFinalMetrics();

        if (_gameProcess != null)
        {
            _gameProcess.Exited -= OnProcessExited;
            _gameProcess.Dispose();
            _gameProcess = null;
        }

        _logger.LogInformation("Stopped monitoring game process for session {SessionId}", _sessionId);
        return Task.FromResult(metrics);
    }

    /// <inheritdoc />
    public Task<SessionPerformanceMetrics> GetCurrentMetricsAsync(CancellationToken ct = default)
    {
        var metrics = new SessionPerformanceMetrics
        {
            AverageCPUUsage = _cpuReadings.Any() ? _cpuReadings.Average() : null,
            PeakMemoryMB = _memoryReadings.Any() ? _memoryReadings.Max() / (1024 * 1024) : null,
            AverageFPS = null // Would need additional capture tools for FPS
        };

        return Task.FromResult(metrics);
    }

    private async Task CollectMetricsAsync()
    {
        if (!_isMonitoring || _gameProcess?.HasExited != false)
            return;

        try
        {
            _gameProcess.Refresh();

            // Collect CPU usage (simplified - would need PerformanceCounter for accurate CPU %)
            var cpuTime = _gameProcess.TotalProcessorTime;
            // In a real implementation, calculate percentage based on time delta

            // Collect memory usage
            var memoryBytes = _gameProcess.WorkingSet64;
            _memoryReadings.Add(memoryBytes);

            // Keep only last 100 readings (prevent memory growth)
            if (_memoryReadings.Count > 100)
                _memoryReadings.RemoveAt(0);
            if (_cpuReadings.Count > 100)
                _cpuReadings.RemoveAt(0);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to collect metrics for session {SessionId}", _sessionId);
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        try
        {
            _isMonitoring = false;
            _metricsTimer.Stop();

            var exitCode = _gameProcess?.ExitCode;

            ProcessExited?.Invoke(this, new GameProcessExitedEventArgs
            {
                SessionId = _sessionId,
                ProcessId = _gameProcess?.Id ?? 0,
                ExitCode = exitCode,
                ExitTime = _timeProvider.UtcNow
            });

            _logger.LogInformation("Game process exited with code {ExitCode} for session {SessionId}",
                exitCode, _sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling process exit for session {SessionId}", _sessionId);
        }
    }

    private SessionPerformanceMetrics CalculateFinalMetrics()
    {
        return new SessionPerformanceMetrics
        {
            AverageCPUUsage = _cpuReadings.Any() ? _cpuReadings.Average() : null,
            PeakMemoryMB = _memoryReadings.Any() ? _memoryReadings.Max() / (1024 * 1024) : null,
            AverageFPS = null // Would need frame capture integration
        };
    }

    public void Dispose()
    {
        _metricsTimer?.Stop();
        _metricsTimer?.Dispose();
        _gameProcess?.Dispose();
    }
}
