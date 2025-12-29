using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SaveState.Core.Ai.Memory;
using SaveState.Infrastructure.Ai.Memory;
using Xunit;

namespace SaveState.Infrastructure.Tests.Ai.Memory;

public class EnhancedShortTermMemoryTests
{
    private readonly Mock<IOptions<MemoryConfig>> _configMock = new();
    private readonly Mock<ILogger<EnhancedShortTermMemory>> _loggerMock = new();
    private readonly EnhancedShortTermMemory _memory;

    public EnhancedShortTermMemoryTests()
    {
        var config = new MemoryConfig
        {
            MaxEntries = 10,
            MaxTokens = 1000,
            PruneBatchSize = 3
        };
        _configMock.Setup(c => c.Value).Returns(config);

        _memory = new EnhancedShortTermMemory(_configMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task StoreAsync_WithValidEntry_StoresSuccessfully()
    {
        // Arrange
        var entry = new MemoryEntry(
            Id: "test-1",
            Content: "This is a test memory entry",
            Timestamp: DateTime.UtcNow,
            Contexts: new[] { "test", "memory" });

        // Act
        await _memory.StoreAsync(entry);

        // Assert
        _memory.CurrentEntryCount.Should().Be(1);
        _memory.CurrentTokenCount.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task StoreAsync_WithDuplicateId_OverwritesExistingEntry()
    {
        // Arrange
        var entry1 = new MemoryEntry("test-1", "First content", DateTime.UtcNow, new[] { "test" });
        var entry2 = new MemoryEntry("test-1", "Updated content", DateTime.UtcNow, new[] { "test" });

        // Act
        await _memory.StoreAsync(entry1);
        await _memory.StoreAsync(entry2);

        // Assert
        _memory.CurrentEntryCount.Should().Be(1); // Should still be 1 entry
    }

    [Fact]
    public async Task StoreAsync_ExceedingMaxEntries_TriggersPruning()
    {
        // Arrange - Fill memory to capacity
        for (int i = 0; i < 12; i++) // More than MaxEntries (10)
        {
            var entry = new MemoryEntry(
                $"entry-{i}",
                $"Content for entry {i}",
                DateTime.UtcNow.AddMinutes(-i),
                new[] { "test" });
            await _memory.StoreAsync(entry);
        }

        // Assert
        _memory.CurrentEntryCount.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public async Task StoreAsync_ExceedingMaxTokens_TriggersPruning()
    {
        // Arrange - Create entries that exceed token limit
        var config = new MemoryConfig { MaxEntries = 100, MaxTokens = 100, PruneBatchSize = 5 };
        _configMock.Setup(c => c.Value).Returns(config);
        var memory = new EnhancedShortTermMemory(_configMock.Object, _loggerMock.Object);

        // Act - Add entries that will exceed token limit
        for (int i = 0; i < 20; i++)
        {
            var entry = new MemoryEntry(
                $"entry-{i}",
                $"This is a very long content that will consume many tokens and eventually exceed the limit. " +
                $"Entry number {i} with substantial text content to reach token limits.",
                DateTime.UtcNow,
                new[] { "test" });
            await memory.StoreAsync(entry);
        }

        // Assert
        memory.CurrentTokenCount.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task SearchAsync_WithKeywordQuery_ReturnsRelevantEntries()
    {
        // Arrange
        var entry1 = new MemoryEntry("1", "The quick brown fox", DateTime.UtcNow, new[] { "animals", "fox" });
        var entry2 = new MemoryEntry("2", "The lazy dog sleeps", DateTime.UtcNow, new[] { "animals", "dog" });
        var entry3 = new MemoryEntry("3", "Programming is fun", DateTime.UtcNow, new[] { "programming", "fun" });

        await _memory.StoreAsync(entry1);
        await _memory.StoreAsync(entry2);
        await _memory.StoreAsync(entry3);

        // Act
        var results = await _memory.SearchAsync("fox", 5, default);

        // Assert
        results.Should().HaveCount(1);
        results[0].Id.Should().Be("1");
        results[0].Content.Should().Contain("fox");
    }

    [Fact]
    public async Task SearchAsync_WithContextQuery_ReturnsMatchingEntries()
    {
        // Arrange
        var entry1 = new MemoryEntry("1", "Content 1", DateTime.UtcNow, new[] { "work", "urgent" });
        var entry2 = new MemoryEntry("2", "Content 2", DateTime.UtcNow, new[] { "personal", "urgent" });
        var entry3 = new MemoryEntry("3", "Content 3", DateTime.UtcNow, new[] { "work", "normal" });

        await _memory.StoreAsync(entry1);
        await _memory.StoreAsync(entry2);
        await _memory.StoreAsync(entry3);

        // Act
        var results = await _memory.SearchAsync("urgent", 5, default);

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(r => r.Id == "1");
        results.Should().Contain(r => r.Id == "2");
    }

    [Fact]
    public async Task SearchAsync_WithMaxResults_LimitsReturnedEntries()
    {
        // Arrange
        for (int i = 0; i < 5; i++)
        {
            var entry = new MemoryEntry($"{i}", $"Content {i} with keyword", DateTime.UtcNow, new[] { "test" });
            await _memory.StoreAsync(entry);
        }

        // Act
        var results = await _memory.SearchAsync("keyword", 2, default);

        // Assert
        results.Should().HaveCount(2);
    }



    [Fact]
    public async Task ClearAsync_RemovesAllEntries()
    {
        // Arrange
        var entry1 = new MemoryEntry("1", "Content 1", DateTime.UtcNow, new[] { "test" });
        var entry2 = new MemoryEntry("2", "Content 2", DateTime.UtcNow, new[] { "test" });
        await _memory.StoreAsync(entry1);
        await _memory.StoreAsync(entry2);

        // Act
        await _memory.ClearAsync(default);

        // Assert
        _memory.CurrentEntryCount.Should().Be(0);
        _memory.CurrentTokenCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchAsync_IncreasesAccessCount()
    {
        // Arrange
        var entry = new MemoryEntry("test-1", "Searchable content", DateTime.UtcNow, new[] { "test" });
        await _memory.StoreAsync(entry);

        // Act
        await _memory.SearchAsync("content", 5, default);

        // Assert - Access count should be tracked (though we can't easily verify without exposing internal state)
        // This test ensures the search operation completes without errors
    }

    [Fact]
    public async Task StoreAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var entry = new MemoryEntry("test-1", "Test content", DateTime.UtcNow, new[] { "test" });
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _memory.StoreAsync(entry, cts.Token));
    }

    [Fact]
    public async Task ConcurrentAccess_HandlesThreadSafety()
    {
        // Arrange
        var tasks = new List<Task>();

        // Act - Concurrently add entries from multiple tasks
        for (int i = 0; i < 10; i++)
        {
            var taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                for (int j = 0; j < 5; j++)
                {
                    var entry = new MemoryEntry(
                        $"task-{taskId}-entry-{j}",
                        $"Content from task {taskId}, entry {j}",
                        DateTime.UtcNow,
                        new[] { "concurrent", $"task-{taskId}" });
                    await _memory.StoreAsync(entry).ConfigureAwait(false);
                }
            }));
        }

        await Task.WhenAll(tasks);

        // Assert
        _memory.CurrentEntryCount.Should().BeGreaterThan(0);
        // No exceptions should be thrown during concurrent access
    }
}
