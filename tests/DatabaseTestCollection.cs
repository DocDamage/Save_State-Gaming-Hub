using Xunit;

namespace SaveState;

/// <summary>
/// Unified test collection for all database-related tests to ensure proper isolation.
/// This prevents stack overflow issues when running multiple database test classes simultaneously.
/// </summary>
[CollectionDefinition("DatabaseTests")]
public class DatabaseTestCollection : ICollectionFixture<DatabaseTestFixture>
{
    // This class has no code, and is never created. Its purpose is to be the place to
    // apply [CollectionDefinition] and all the ICollectionFixture<> interfaces.
}

/// <summary>
/// Shared fixture for all database tests to ensure proper setup and cleanup.
/// </summary>
public class DatabaseTestFixture : IDisposable
{
    public DatabaseTestFixture()
    {
        // Setup code that runs once before all tests in the collection
        // This ensures proper initialization order
    }

    public void Dispose()
    {
        // Cleanup code that runs once after all tests in the collection
        // This ensures proper cleanup order and prevents resource leaks
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
