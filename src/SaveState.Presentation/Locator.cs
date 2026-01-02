namespace SaveState.Presentation;

using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Service locator for dependency injection in the Avalonia UI layer.
/// Provides access to registered services throughout the presentation layer.
/// </summary>
public class Locator
{
    private static Locator? _current;
    private IServiceProvider? _services;

    /// <summary>
    /// Gets the singleton instance of the service locator.
    /// </summary>
    public static Locator Current => _current ??= new Locator();

    /// <summary>
    /// Sets the service provider for dependency resolution.
    /// Called during application initialization to provide access to registered services.
    /// </summary>
    /// <param name="services">The service provider instance.</param>
    public void SetServices(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Gets a service instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of service to retrieve.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the service is not registered or services are not set.</exception>
    public T GetService<T>() where T : class
    {
        return _services?.GetService<T>() ?? throw new InvalidOperationException($"Service {typeof(T)} not found");
    }
}
