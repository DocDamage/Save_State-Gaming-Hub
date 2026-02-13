using System;
using Microsoft.EntityFrameworkCore;

namespace SaveState.Tests.Infrastructure;

/// <summary>
/// Provides DbContextOptions for in-memory testing.
///
/// NOTE: Model caching was removed to avoid shared-type entity conflicts with EF Core.
/// The previous implementation used SaveStateDbContextModelSnapshot which stored entities
/// as Dictionary&lt;string, object&gt; (shared-type entities), causing conflicts when actual
/// CLR types were used in tests.
///
/// See: docs/plans/STACK_OVERFLOW_FIX_PLAN_2026-01-17.md
/// </summary>
public static class SaveStateDbContextModelFactory
{
    /// <summary>
    /// Builds an in-memory DbContextOptions instance for testing.
    /// Each call creates a fresh database instance with a properly built model.
    /// </summary>
    /// <param name="databaseName">Optional database name. Defaults to a unique GUID.</param>
    /// <returns>DbContextOptions configured for in-memory testing.</returns>
    public static DbContextOptions<TContext> CreateInMemoryOptions<TContext>(string? databaseName = null)
        where TContext : DbContext
    {
        return new DbContextOptionsBuilder<TContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;
    }
}
