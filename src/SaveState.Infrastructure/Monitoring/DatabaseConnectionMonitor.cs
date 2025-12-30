using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Monitoring;

/// <summary>
/// Monitors database connections and query performance.
/// Tracks connection pool usage and database operation metrics.
/// </summary>
public class DatabaseConnectionMonitor : IDisposable
{
    private readonly SaveStateDbContext _context;
    private readonly IApplicationMetrics _metrics;
    private readonly ILogger<DatabaseConnectionMonitor> _logger;
    private readonly Timer _connectionTimer;
    private bool _disposed;

    public DatabaseConnectionMonitor(
        SaveStateDbContext context,
        IApplicationMetrics metrics,
        ILogger<DatabaseConnectionMonitor> logger)
    {
        _context = context;
        _metrics = metrics;
        _logger = logger;

        // Monitor connection pool every 60 seconds
        _connectionTimer = new Timer(MonitorConnections, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

        _logger.LogInformation("Database connection monitor initialized");
    }

    private void MonitorConnections(object? state)
    {
        try
        {
            if (_disposed)
                return;

            var connection = _context.Database.GetDbConnection();

            // Track connection state
            var isOpen = connection.State == System.Data.ConnectionState.Open;
            _metrics.RecordDatabaseConnectionCount(isOpen ? 1 : 0);

            // In a production system, you might also track:
            // - Connection pool statistics (if using SqlClient)
            // - Active connection count
            // - Connection wait time
            // - Connection lifetime

            _logger.LogDebug("Database connection state: {State}", connection.State);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to monitor database connections");
        }
    }

    /// <summary>
    /// Records a database operation with timing.
    /// </summary>
    public async Task<T> RecordDatabaseOperationAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            var result = await operation().ConfigureAwait(false);
            var duration = DateTime.UtcNow - startTime;

            _metrics.RecordDatabaseQuery(operationName, duration);
            _metrics.RecordDatabaseConnectionCount(1); // Assume connection was used

            return result;
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery(operationName, duration);
            _metrics.RecordDatabaseError(operationName, ex.GetType().Name);

            throw;
        }
    }

    /// <summary>
    /// Records a database operation without return value.
    /// </summary>
    public async Task RecordDatabaseOperationAsync(
        string operationName,
        Func<Task> operation,
        CancellationToken ct = default)
    {
        var startTime = DateTime.UtcNow;

        try
        {
            await operation().ConfigureAwait(false);
            var duration = DateTime.UtcNow - startTime;

            _metrics.RecordDatabaseQuery(operationName, duration);
            _metrics.RecordDatabaseConnectionCount(1);
        }
        catch (Exception ex)
        {
            var duration = DateTime.UtcNow - startTime;
            _metrics.RecordDatabaseQuery(operationName, duration);
            _metrics.RecordDatabaseError(operationName, ex.GetType().Name);

            throw;
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _connectionTimer?.Dispose();

        _logger.LogInformation("Database connection monitor disposed");
    }
}

/// <summary>
/// Extension methods for easy database operation monitoring.
/// </summary>
public static class DatabaseMonitoringExtensions
{
    public static async Task<T> WithMetricsAsync<T>(
        this DatabaseConnectionMonitor monitor,
        string operationName,
        Func<Task<T>> operation,
        CancellationToken ct = default)
    {
        return await monitor.RecordDatabaseOperationAsync(operationName, operation, ct).ConfigureAwait(false);
    }

    public static async Task WithMetricsAsync(
        this DatabaseConnectionMonitor monitor,
        string operationName,
        Func<Task> operation,
        CancellationToken ct = default)
    {
        await monitor.RecordDatabaseOperationAsync(operationName, operation, ct).ConfigureAwait(false);
    }
}
