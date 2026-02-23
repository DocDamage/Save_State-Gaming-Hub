using Microsoft.Extensions.DependencyInjection;

namespace SaveState.Presentation.Services.Animation;

/// <summary>
/// Extension methods for registering the animation service with dependency injection.
/// 
/// Usage:
/// <code>
/// // In Program.cs or App.axaml.cs:
/// services.AddAnimationServices();
/// </code>
/// </summary>
public static class AnimationServiceExtensions
{
    /// <summary>
    /// Adds the animation service and related services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAnimationServices(this IServiceCollection services)
    {
        // Register as singleton since animation service maintains state
        services.AddSingleton<IAnimationService, AnimationService>();

        return services;
    }

    /// <summary>
    /// Adds the animation service and all related presentation services.
    /// This is a convenience method that registers all UI-related services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddPresentationServices(this IServiceCollection services)
    {
        // Animation services
        services.AddAnimationServices();

        // Add other presentation services as needed
        // services.AddSingleton<INotificationService, NotificationService>();
        // services.AddSingleton<IDialogService, DialogService>();

        return services;
    }
}
