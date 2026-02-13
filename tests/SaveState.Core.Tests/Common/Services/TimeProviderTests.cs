using SaveState.Core.Common.Services;

namespace SaveState.Core.Tests.Common.Services;

public class TimeProviderTests
{
    [Fact]
    public void SystemTimeProvider_Instance_ShouldReturnSingleton()
    {
        // Act
        var instance1 = SystemTimeProvider.Instance;
        var instance2 = SystemTimeProvider.Instance;

        // Assert
        Assert.Same(instance1, instance2);
    }

    [Fact]
    public void SystemTimeProvider_Now_ShouldReturnCurrentTime()
    {
        // Arrange
        var provider = SystemTimeProvider.Instance;
        var before = DateTime.Now.AddSeconds(-1);

        // Act
        var now = provider.Now;
        var after = DateTime.Now.AddSeconds(1);

        // Assert
        Assert.True(now >= before && now <= after);
    }

    [Fact]
    public void SystemTimeProvider_UtcNow_ShouldReturnCurrentUtcTime()
    {
        // Arrange
        var provider = SystemTimeProvider.Instance;
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var utcNow = provider.UtcNow;
        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert
        Assert.True(utcNow >= before && utcNow <= after);
    }

    [Fact]
    public void SystemTimeProvider_Today_ShouldReturnCurrentDate()
    {
        // Arrange
        var provider = SystemTimeProvider.Instance;

        // Act
        var today = provider.Today;

        // Assert
        Assert.Equal(DateTime.Today, today);
    }

    [Fact]
    public void SystemTimeProvider_GetTimestamp_ShouldReturnValue()
    {
        // Arrange
        var provider = SystemTimeProvider.Instance;

        // Act
        var timestamp1 = provider.GetTimestamp();
        System.Threading.Thread.Sleep(10);
        var timestamp2 = provider.GetTimestamp();

        // Assert
        Assert.True(timestamp2 >= timestamp1);
    }

    [Fact]
    public void SystemTimeProvider_CreateTimer_ShouldCreateTimer()
    {
        // Arrange
        var provider = SystemTimeProvider.Instance;
        var callbackInvoked = false;

        // Act
        using var timer = provider.CreateTimer(
            _ => callbackInvoked = true,
            null,
            TimeSpan.FromMilliseconds(1),
            TimeSpan.FromMilliseconds(100));

        // Wait for timer to fire with retry logic
        var retryCount = 0;
        while (!callbackInvoked && retryCount < 20)
        {
            System.Threading.Thread.Sleep(50);
            retryCount++;
        }

        // Assert
        Assert.True(callbackInvoked, "Timer callback was not invoked");
    }
}


