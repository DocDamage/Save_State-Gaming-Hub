using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Intelligence.AiContent.Services;
using SaveState.Core.Intelligence.GamingDna.Services;
using SaveState.Core.Intelligence.Recommendations.Services;
using SaveState.Core.Intelligence.Search.Services;
using SaveState.Infrastructure.Intelligence.AiContent;
using SaveState.Infrastructure.Intelligence.GamingDna;
using SaveState.Infrastructure.Intelligence.Recommendations;
using SaveState.Infrastructure.Intelligence.Search;

namespace SaveState.Infrastructure.Intelligence;

/// <summary>
/// Extension methods for registering Phase 2 Intelligence & Personalization services.
/// </summary>
public static class IntelligenceServiceExtensions
{
    /// <summary>
    /// Adds Phase 2 Intelligence & Personalization services to the DI container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddIntelligenceServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Smart Game Recommendations 2.0
        services.AddSingleton<IRecommendationEngineV2, HybridRecommendationEngineV2>();

        // Gaming DNA Profile
        services.AddSingleton<IGamingDnaAnalyzer, GamingDnaAnalyzer>();

        // Generative AI Content Hub
        services.AddSingleton<IThumbnailGeneratorService, ThumbnailGeneratorService>();
        services.AddSingleton<INaturalLanguageSaveSearch, NaturalLanguageSaveSearch>();

        // Universal Search 2.0
        services.AddSingleton<IUniversalSearchService, UniversalSearchService>();

        // Configure AI Content Generation options
        services.Configure<AiContentGenerationOptions>(options =>
        {
            options.DefaultProvider = configuration["AI:ContentGeneration:DefaultProvider"] ?? "OpenAI";
            options.OpenAiApiKey = configuration["AI:ContentGeneration:OpenAiApiKey"];
            options.StableDiffusionEndpoint = configuration["AI:ContentGeneration:StableDiffusionEndpoint"];
            options.MaxConcurrentGenerations = configuration.GetValue<int>("AI:ContentGeneration:MaxConcurrentGenerations", 3);
            options.GenerationTimeout = configuration.GetValue<TimeSpan>("AI:ContentGeneration:GenerationTimeout", TimeSpan.FromMinutes(5));
            options.EnableCaching = configuration.GetValue<bool>("AI:ContentGeneration:EnableCaching", true);
            options.CacheExpiration = configuration.GetValue<TimeSpan>("AI:ContentGeneration:CacheExpiration", TimeSpan.FromHours(24));
        });

        return services;
    }

    /// <summary>
    /// Adds only recommendation services.
    /// </summary>
    public static IServiceCollection AddRecommendationServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IRecommendationEngineV2, HybridRecommendationEngineV2>();
        return services;
    }

    /// <summary>
    /// Adds only gaming DNA services.
    /// </summary>
    public static IServiceCollection AddGamingDnaServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IGamingDnaAnalyzer, GamingDnaAnalyzer>();
        return services;
    }

    /// <summary>
    /// Adds only AI content generation services.
    /// </summary>
    public static IServiceCollection AddAiContentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IThumbnailGeneratorService, ThumbnailGeneratorService>();
        services.AddSingleton<INaturalLanguageSaveSearch, NaturalLanguageSaveSearch>();

        services.Configure<AiContentGenerationOptions>(options =>
        {
            options.DefaultProvider = configuration["AI:ContentGeneration:DefaultProvider"] ?? "OpenAI";
            options.OpenAiApiKey = configuration["AI:ContentGeneration:OpenAiApiKey"];
            options.StableDiffusionEndpoint = configuration["AI:ContentGeneration:StableDiffusionEndpoint"];
            options.MaxConcurrentGenerations = configuration.GetValue<int>("AI:ContentGeneration:MaxConcurrentGenerations", 3);
            options.GenerationTimeout = configuration.GetValue<TimeSpan>("AI:ContentGeneration:GenerationTimeout", TimeSpan.FromMinutes(5));
            options.EnableCaching = configuration.GetValue<bool>("AI:ContentGeneration:EnableCaching", true);
            options.CacheExpiration = configuration.GetValue<TimeSpan>("AI:ContentGeneration:CacheExpiration", TimeSpan.FromHours(24));
        });

        return services;
    }

    /// <summary>
    /// Adds only universal search services.
    /// </summary>
    public static IServiceCollection AddUniversalSearchServices(
        this IServiceCollection services)
    {
        services.AddSingleton<IUniversalSearchService, UniversalSearchService>();
        return services;
    }
}
