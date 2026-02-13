using Xunit;

namespace SaveState.Infrastructure.Tests.Ai;

/// <summary>
/// Test collection for AI-related tests to prevent stack overflow when running async tests.
/// These tests must run non-parallelized due to potential threading issues with async state machines.
/// </summary>
[CollectionDefinition("AiOrchestrator", DisableParallelization = true)]
public class AiTestCollection
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
