namespace SaveState.Infrastructure.Health;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SaveState.Infrastructure.Persistence;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly SaveStateDbContext _dbContext;

    public DatabaseHealthCheck(SaveStateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Simple query to check database connectivity
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);

            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Cannot connect to database");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Check if migrations are applied
            var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
            if (pendingMigrations.Any())
            {
                return HealthCheckResult.Degraded(
                    $"Database has {pendingMigrations.Count()} pending migrations",
                    data: new Dictionary<string, object>
                    {
                        ["PendingMigrations"] = pendingMigrations
                    });
            }

            return HealthCheckResult.Healthy("Database is healthy");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database check failed", ex);
        }
    }
}
