using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Moq;

namespace SaveState.Tests.Infrastructure;

/// <summary>
/// Base class for unit tests with common setup and teardown.
/// PHASE 7: REQUIRED - Advanced Testing Foundation
/// </summary>
public abstract class BaseUnitTest : IDisposable
{
    protected readonly ServiceCollection _services;
    protected IServiceProvider _serviceProvider;

    protected BaseUnitTest()
    {
        _services = new ServiceCollection();
        SetupServices();
        _serviceProvider = _services.BuildServiceProvider();
    }

    /// <summary>
    /// Override to configure services for testing.
    /// </summary>
    protected virtual void SetupServices()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Gets a service from the DI container.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Creates a mock of a service.
    /// </summary>
    protected Mock<T> CreateMock<T>() where T : class
    {
        return new Mock<T>();
    }

    public virtual void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Base class for integration tests with database context.
/// </summary>
public abstract class BaseIntegrationTest : IDisposable
{
    protected readonly ServiceCollection _services;
    protected IServiceProvider _serviceProvider;

    protected BaseIntegrationTest()
    {
        _services = new ServiceCollection();
        SetupServices();
        _serviceProvider = _services.BuildServiceProvider();
        InitializeDatabase();
    }

    /// <summary>
    /// Override to configure services for testing.
    /// </summary>
    protected virtual void SetupServices()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Override to initialize database.
    /// </summary>
    protected virtual void InitializeDatabase()
    {
        // Override in derived classes
    }

    /// <summary>
    /// Gets a service from the DI container.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Cleans up test data after test.
    /// </summary>
    protected virtual Task CleanupAsync()
    {
        return Task.CompletedTask;
    }

    public virtual async Task DisposeAsync()
    {
        await CleanupAsync();
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }

    public virtual void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}

/// <summary>
/// Test fixture for managing shared test data.
/// </summary>
public class TestFixture
{
    private readonly Dictionary<string, object> _data = new();

    /// <summary>
    /// Sets test data.
    /// </summary>
    public void SetData<T>(string key, T value)
    {
        _data[key] = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets test data.
    /// </summary>
    public T GetData<T>(string key)
    {
        if (_data.TryGetValue(key, out var value))
        {
            return (T)value;
        }
        throw new KeyNotFoundException($"Test data key not found: {key}");
    }

    /// <summary>
    /// Checks if test data exists.
    /// </summary>
    public bool HasData(string key) => _data.ContainsKey(key);

    /// <summary>
    /// Clears all test data.
    /// </summary>
    public void Clear() => _data.Clear();
}

/// <summary>
/// Test assertions helper.
/// </summary>
public static class TestAssertions
{
    /// <summary>
    /// Asserts that a result is successful.
    /// </summary>
    public static void AssertSuccess<T>(Core.Common.Result<T> result)
    {
        Assert.True(result.IsSuccess, $"Result should be successful. Error: {result.Error}");
    }

    /// <summary>
    /// Asserts that a result is a failure.
    /// </summary>
    public static void AssertFailure<T>(Core.Common.Result<T> result)
    {
        Assert.False(result.IsSuccess, "Result should be a failure");
    }

    /// <summary>
    /// Asserts that a result contains expected value.
    /// </summary>
    public static void AssertValue<T>(Core.Common.Result<T> result, T expectedValue)
    {
        AssertSuccess(result);
        Assert.Equal(expectedValue, result.Value);
    }

    /// <summary>
    /// Asserts that collections are equal.
    /// </summary>
    public static void AssertCollectionEqual<T>(IEnumerable<T> expected, IEnumerable<T> actual)
    {
        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// Asserts that collection contains item.
    /// </summary>
    public static void AssertCollectionContains<T>(IEnumerable<T> collection, T item)
    {
        Assert.Contains(item, collection);
    }
}

/// <summary>
/// Test data builder for creating test objects.
/// </summary>
public class TestDataBuilder<T> where T : class, new()
{
    private readonly T _instance = new();

    /// <summary>
    /// Sets a property value.
    /// </summary>
    public TestDataBuilder<T> With<TProp>(string propertyName, TProp value)
    {
        var property = typeof(T).GetProperty(propertyName);
        if (property != null && property.CanWrite)
        {
            property.SetValue(_instance, value);
        }
        return this;
    }

    /// <summary>
    /// Builds the test object.
    /// </summary>
    public T Build() => _instance;
}
