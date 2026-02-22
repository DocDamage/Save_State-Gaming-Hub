using Microsoft.Extensions.Logging;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.Ai.EyeTracking;

namespace SaveState.Infrastructure.Assistant;

/// <summary>
/// Composite eye-tracking provider that tries multiple providers in order of preference.
/// Provides fallback mechanism from Tobii -> Windows Eye Control -> No-op.
/// </summary>
public sealed class CompositeEyeTrackingProvider : IEyeTrackingMonitor, IDisposable
{
    private readonly IReadOnlyList<IEyeTrackingMonitor> _providers;
    private readonly ILogger<CompositeEyeTrackingProvider> _logger;
    private readonly ITimeProvider _timeProvider;
    private IEyeTrackingMonitor? _activeProvider;
    private readonly object _lock = new();
    private bool _isDisposed;

    public CompositeEyeTrackingProvider(
        ILogger<CompositeEyeTrackingProvider> logger,
        ITimeProvider timeProvider,
        IEnumerable<IEyeTrackingMonitor> providers)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _providers = providers?.ToList() ?? throw new ArgumentNullException(nameof(providers));
        
        // Filter to only available providers and order by preference
        _providers = _providers
            .Where(p => p.IsAvailable)
            .OrderByDescending(p => p is TobiiEyeTrackingProvider ? 100 : p is WindowsEyeTrackingMonitor ? 50 : 0)
            .ToList();

        _logger.LogInformation(
            "Composite eye-tracking provider initialized with {Count} available providers: {Providers}",
            _providers.Count,
            string.Join(", ", _providers.Select(p => p.GetType().Name)));
    }

    /// <inheritdoc />
    public bool IsAvailable => _providers.Any(p => p.IsAvailable);

    /// <inheritdoc />
    public bool IsMonitoring => _activeProvider?.IsMonitoring ?? false;

    /// <summary>
    /// Gets the currently active provider, if any.
    /// </summary>
    public IEyeTrackingMonitor? ActiveProvider => _activeProvider;

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    public IReadOnlyList<IEyeTrackingMonitor> Providers => _providers;

    /// <inheritdoc />
    public async Task<Result> StartMonitoringAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Result.Failure("Composite provider has been disposed.", ErrorType.Validation);
        }

        lock (_lock)
        {
            if (_activeProvider?.IsMonitoring == true)
            {
                return Result.Success();
            }
        }

        // Try each provider in order
        foreach (var provider in _providers)
        {
            try
            {
                _logger.LogDebug("Attempting to start monitoring with {Provider}", provider.GetType().Name);
                
                var result = await provider.StartMonitoringAsync(ct);
                
                if (result.IsSuccess)
                {
                    lock (_lock)
                    {
                        _activeProvider = provider;
                    }
                    
                    _logger.LogInformation(
                        "Eye-tracking monitoring started with {Provider}",
                        provider.GetType().Name);
                    
                    return Result.Success();
                }
                
                _logger.LogWarning(
                    "Failed to start {Provider}: {Error}",
                    provider.GetType().Name,
                    result.Error);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Exception starting {Provider}",
                    provider.GetType().Name);
            }
        }

        return Result.Failure(
            "No eye-tracking provider could be started. Ensure Tobii or Windows Eye Control is installed.",
            ErrorType.NotImplemented);
    }

    /// <inheritdoc />
    public async Task<Result> StopMonitoringAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Result.Success();
        }

        IEyeTrackingMonitor? providerToStop;
        
        lock (_lock)
        {
            providerToStop = _activeProvider;
            _activeProvider = null;
        }

        if (providerToStop == null)
        {
            // Stop all providers to be safe
            foreach (var provider in _providers)
            {
                try
                {
                    await provider.StopMonitoringAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error stopping {Provider}", provider.GetType().Name);
                }
            }
            
            return Result.Success();
        }

        try
        {
            var result = await providerToStop.StopMonitoringAsync(ct);
            
            _logger.LogInformation(
                "Eye-tracking monitoring stopped for {Provider}",
                providerToStop.GetType().Name);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping {Provider}", providerToStop.GetType().Name);
            return Result.Failure($"Error stopping monitoring: {ex.Message}", ErrorType.External);
        }
    }

    /// <inheritdoc />
    public Task<Result<EyeTrackingSnapshot>> GetSnapshotAsync(CancellationToken ct = default)
    {
        if (_isDisposed)
        {
            return Task.FromResult(Result.Failure<EyeTrackingSnapshot>(
                "Composite provider has been disposed.",
                ErrorType.Validation));
        }

        IEyeTrackingMonitor? activeProvider;
        
        lock (_lock)
        {
            activeProvider = _activeProvider;
        }

        if (activeProvider == null)
        {
            return Task.FromResult(Result.Failure<EyeTrackingSnapshot>(
                "No eye-tracking provider is currently active.",
                ErrorType.Validation));
        }

        return activeProvider.GetSnapshotAsync(ct);
    }

    /// <summary>
    /// Gets snapshots from all available providers for comparison/diagnostics.
    /// </summary>
    public async Task<Result<IReadOnlyList<ProviderSnapshot>>> GetAllSnapshotsAsync(CancellationToken ct = default)
    {
        var snapshots = new List<ProviderSnapshot>();

        foreach (var provider in _providers)
        {
            try
            {
                if (!provider.IsMonitoring)
                {
                    snapshots.Add(new ProviderSnapshot(
                        provider.GetType().Name,
                        provider.IsAvailable,
                        false,
                        null,
                        null));
                    continue;
                }

                var result = await provider.GetSnapshotAsync(ct);
                
                snapshots.Add(new ProviderSnapshot(
                    provider.GetType().Name,
                    provider.IsAvailable,
                    provider.IsMonitoring,
                    result.IsSuccess ? result.Value : null,
                    result.IsFailure ? result.Error : null));
            }
            catch (Exception ex)
            {
                snapshots.Add(new ProviderSnapshot(
                    provider.GetType().Name,
                    provider.IsAvailable,
                    provider.IsMonitoring,
                    null,
                    ex.Message));
            }
        }

        return Result.Success<IReadOnlyList<ProviderSnapshot>>(snapshots.AsReadOnly());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        lock (_lock)
        {
            _activeProvider = null;
        }

        // Dispose all disposable providers
        foreach (var provider in _providers.OfType<IDisposable>())
        {
            try
            {
                provider.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing {Provider}", provider.GetType().Name);
            }
        }

        _logger.LogDebug("Composite eye-tracking provider disposed");
    }

    /// <summary>
    /// Represents a snapshot from a specific provider.
    /// </summary>
    public sealed record ProviderSnapshot(
        string ProviderName,
        bool IsAvailable,
        bool IsMonitoring,
        EyeTrackingSnapshot? Snapshot,
        string? Error);
}
