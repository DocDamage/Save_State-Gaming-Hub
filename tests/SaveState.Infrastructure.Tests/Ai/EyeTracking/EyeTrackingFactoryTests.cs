using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Assistant.Services;
using SaveState.Core.Common.Services;
using SaveState.Infrastructure.Ai.EyeTracking;
using SaveState.Infrastructure.Assistant;
using SaveState.Tests.Infrastructure;

namespace SaveState.Infrastructure.Tests.AI.EyeTracking;

public class EyeTrackingFactoryTests
{
    private readonly TestServiceProvider _serviceProvider;

    public EyeTrackingFactoryTests()
    {
        _serviceProvider = new TestServiceProvider();
    }

    [Fact]
    public void CreateBestAvailable_WhenNoHardwareAvailable_ReturnsNoOpProvider()
    {
        // Act
        var provider = EyeTrackingFactory.CreateBestAvailable(_serviceProvider);

        // Assert
        provider.Should().NotBeNull();
        if (provider.IsAvailable)
        {
            provider.Should().NotBeOfType<NoOpEyeTrackingMonitor>();
        }
        else
        {
            provider.Should().BeOfType<NoOpEyeTrackingMonitor>();
        }
    }

    [Fact]
    public void CreateProvider_WithNoOpType_ReturnsNoOpProvider()
    {
        // Act
        var provider = EyeTrackingFactory.CreateProvider(EyeTrackingProviderType.NoOp, _serviceProvider);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<NoOpEyeTrackingMonitor>();
        provider.IsAvailable.Should().BeFalse();
    }

    [Fact]
    public void CreateProvider_WithInvalidType_ThrowsArgumentOutOfRangeException()
    {
        // Act & Assert
        var act = () => EyeTrackingFactory.CreateProvider((EyeTrackingProviderType)999, _serviceProvider);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetAvailableProviders_ReturnsAllProviderTypes()
    {
        // Act
        var providers = EyeTrackingFactory.GetAvailableProviders(_serviceProvider).ToList();

        // Assert
        providers.Should().NotBeEmpty();
        providers.Should().Contain(p => p.Type == EyeTrackingProviderType.NoOp);
        providers.Should().Contain(p => p.IsAvailable == true); // At least NoOp is available
    }

    [Fact]
    public void GetAvailableProviders_ReturnsSortedByPriority()
    {
        // Act
        var providers = EyeTrackingFactory.GetAvailableProviders(_serviceProvider).ToList();

        // Assert
        var priorities = providers.Select(p => p.Priority).ToList();
        priorities.Should().BeInAscendingOrder();
    }

    [Fact]
    public void CreateProvider_WithWindowsEyeControlOnWindows_DoesNotThrow()
    {
        // Skip on non-Windows platforms
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var provider = EyeTrackingFactory.CreateProvider(EyeTrackingProviderType.WindowsEyeControl, _serviceProvider);

        // Assert
        provider.Should().NotBeNull();
        provider.Should().BeOfType<WindowsEyeControlProvider>();
    }

    [Fact]
    public void CreateProvider_WithWindowsEyeControlOnNonWindows_ThrowsPlatformNotSupported()
    {
        // This test only makes sense on non-Windows platforms
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        // Act & Assert
        var act = () => EyeTrackingFactory.CreateProvider(EyeTrackingProviderType.WindowsEyeControl, _serviceProvider);
        act.Should().Throw<PlatformNotSupportedException>();
    }

    [Fact]
    public void AddEyeTrackingServices_AddsSingletonToServices()
    {
        // Arrange
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        // Act
        services.AddEyeTrackingServices();

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IEyeTrackingMonitor));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEyeTrackingServices_WithSpecificProvider_AddsThatProvider()
    {
        // Arrange
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();

        // Act
        services.AddEyeTrackingServices(EyeTrackingProviderType.NoOp);

        // Assert
        var descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IEyeTrackingMonitor));
        descriptor.Should().NotBeNull();
    }

    private class TestServiceProvider : IServiceProvider
    {
        private readonly ITimeProvider _timeProvider = new TestTimeProvider();
        private readonly ILoggerFactory _loggerFactory = NullLoggerFactory.Instance;

        public object? GetService(Type serviceType)
        {
            if (serviceType == typeof(ITimeProvider))
                return _timeProvider;
            if (serviceType == typeof(ILoggerFactory))
                return _loggerFactory;
            return null;
        }
    }
}
