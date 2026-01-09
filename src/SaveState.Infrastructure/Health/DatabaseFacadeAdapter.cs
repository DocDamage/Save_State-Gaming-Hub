namespace SaveState.Infrastructure.Health;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Collections.Generic;

public class DatabaseFacadeAdapter : IDatabaseFacadeAdapter
{
    private readonly DatabaseFacade _facade;

    public DatabaseFacadeAdapter(DatabaseFacade facade)
    {
        _facade = facade;
    }

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        return _facade.CanConnectAsync(cancellationToken);
    }

    public Task<IEnumerable<string>> GetPendingMigrationsAsync(CancellationToken cancellationToken = default)
    {
        // Use EF Core's extension method for pending migrations
        // This returns IEnumerable<string>, we wrap in Task via Task.FromResult for simplicity
        var migrations = _facade.GetPendingMigrations();
        return Task.FromResult(migrations);
    }
}