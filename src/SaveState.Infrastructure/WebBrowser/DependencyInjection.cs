using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.WebBrowser.Services;
using SaveState.Infrastructure.WebBrowser.Services;

namespace SaveState.Infrastructure.WebBrowser;

/// <summary>
/// Extension methods for registering WebBrowser services in the dependency injection container.
/// </summary>
public static class WebBrowserDependencyInjection
{
    /// <summary>
    /// Adds CefSharp-based web browser services to the service collection.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWebBrowserServices(this IServiceCollection services)
    {
        // Register the main browser service as a singleton
        services.AddSingleton<IBrowserService, CefSharpBrowserService>();
        
        // Register the concrete implementation for internal use
        services.AddSingleton<CefSharpBrowserService>(sp => 
            (CefSharpBrowserService)sp.GetRequiredService<IBrowserService>());
        
        // Register OAuth handler
        services.AddSingleton<OAuthHandler>();
        
        return services;
    }

    /// <summary>
    /// Adds web browser services with custom configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure browser options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddWebBrowserServices(
        this IServiceCollection services,
        Action<Core.WebBrowser.Models.BrowserSettings> configureOptions)
    {
        services.AddWebBrowserServices();
        services.Configure(configureOptions);
        
        return services;
    }
}
