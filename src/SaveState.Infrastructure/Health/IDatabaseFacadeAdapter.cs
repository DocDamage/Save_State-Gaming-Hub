namespace SaveState.Infrastructure.Health;

using System.Collections.Generic;

public interface IDatabaseFacadeAdapter
{
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default);
}