namespace SaveState.Presentation;

using Microsoft.Extensions.DependencyInjection;

public class Locator
{
    private static Locator? _current;
    private IServiceProvider? _services;

    public static Locator Current => _current ??= new Locator();

    public void SetServices(IServiceProvider services)
    {
        _services = services;
    }

    public T GetService<T>() where T : class
    {
        return _services?.GetService<T>() ?? throw new InvalidOperationException($"Service {typeof(T)} not found");
    }
}
