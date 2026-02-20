using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SaveState.Core.Common.Configuration;
using SaveState.Infrastructure.Services;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.Services;

public class RateLimiterTests
{
    [Fact]
    public async Task IsAllowedAsync_WhenUnderLimit_ShouldReturnTrue()
    {
        var timeProvider = new TestTimeProvider(new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(cache, timeProvider, maxRequests: 2, windowMinutes: 1);

        await sut.RecordOperationAsync("user-1", "ImportGame");

        var allowed = await sut.IsAllowedAsync("user-1", "ImportGame");

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenLimitReached_ShouldReturnFalse()
    {
        var timeProvider = new TestTimeProvider(new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(cache, timeProvider, maxRequests: 2, windowMinutes: 1);

        await sut.RecordOperationAsync("user-1", "ImportGame");
        await sut.RecordOperationAsync("user-1", "ImportGame");

        var allowed = await sut.IsAllowedAsync("user-1", "ImportGame");

        allowed.Should().BeFalse();
    }

    [Fact]
    public async Task IsAllowedAsync_WhenWindowElapsed_ShouldReturnTrue()
    {
        var timeProvider = new TestTimeProvider(new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(cache, timeProvider, maxRequests: 2, windowMinutes: 1);

        await sut.RecordOperationAsync("user-1", "ImportGame");
        await sut.RecordOperationAsync("user-1", "ImportGame");
        (await sut.IsAllowedAsync("user-1", "ImportGame")).Should().BeFalse();

        timeProvider.Advance(TimeSpan.FromMinutes(2));

        var allowed = await sut.IsAllowedAsync("user-1", "ImportGame");

        allowed.Should().BeTrue();
    }

    [Fact]
    public async Task GetResetTimeAsync_ShouldUseConfiguredWindow()
    {
        var initialTime = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc);
        var timeProvider = new TestTimeProvider(initialTime);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = CreateSut(cache, timeProvider, maxRequests: 2, windowMinutes: 1);

        await sut.RecordOperationAsync("user-1", "ImportGame");
        var result = await sut.GetResetTimeAsync("user-1", "ImportGame");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new DateTimeOffset(initialTime).AddMinutes(1));
    }

    private static RateLimiter CreateSut(
        IMemoryCache cache,
        TestTimeProvider timeProvider,
        int maxRequests,
        int windowMinutes)
    {
        var options = Options.Create(new RateLimitingOptions
        {
            Enabled = true,
            Operations = new RateLimitingOptions.OperationLimits
            {
                ImportGame = new RateLimitingOptions.OperationLimit
                {
                    MaxRequests = maxRequests,
                    WindowMinutes = windowMinutes
                }
            }
        });

        return new RateLimiter(cache, NullLogger<RateLimiter>.Instance, options, timeProvider);
    }
}
