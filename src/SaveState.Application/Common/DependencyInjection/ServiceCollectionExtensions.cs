namespace SaveState.Application.Common.DependencyInjection;

using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Application.Common.Behaviors;
using SaveState.Application.Common.Events;
using SaveState.Core.GameLibrary;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds all application layer services to the dependency injection container.
    /// This includes MediatR, domain services, and application services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <returns>The service collection with application services added.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssemblies(
                typeof(ServiceCollectionExtensions).Assembly,
                typeof(IGameRepository).Assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(RateLimitingBehavior<,>));
        });

        // Register event publisher
        services.AddScoped<IEventPublisher, EventPublisher>();


           // Register application services
           services.AddScoped<SaveState.Application.GameLibrary.Services.IGameImportService, SaveState.Application.GameLibrary.Services.GameImportService>();
           services.AddScoped<SaveState.Application.RomManagement.Services.IRomScannerService, SaveState.Application.RomManagement.Services.RomScannerService>();
           services.AddScoped<SaveState.Application.RomManagement.Services.IEmulatorService, SaveState.Application.RomManagement.Services.EmulatorService>();
           services.AddSingleton<SaveState.Application.RomManagement.Services.ILiveSyncService, SaveState.Application.RomManagement.Services.LiveSyncService>();

           // Register domain services
        services.AddScoped<SaveState.Core.GameLibrary.DomainServices.IGameValidationService, SaveState.Core.GameLibrary.DomainServices.GameValidationService>();
        services.AddScoped<SaveState.Core.GameLibrary.DomainServices.IGameImportService, SaveState.Core.GameLibrary.DomainServices.GameImportService>();
        services.AddScoped<SaveState.Core.GameLibrary.DomainServices.IMetadataEnrichmentService, SaveState.Core.GameLibrary.DomainServices.MetadataEnrichmentService>();

           // Register game library services
           services.AddScoped<SaveState.Core.GameLibrary.Services.IAchievementService, SaveState.Application.GameLibrary.Services.AchievementService>();

           // Register onboarding services
           services.AddScoped<SaveState.Application.Onboarding.Services.OnboardingService>();

           // Register authentication services
           services.AddScoped<SaveState.Core.UserManagement.Services.IAuthenticationService, SaveState.Application.UserManagement.AuthenticationService>();

        return services;
    }
}
