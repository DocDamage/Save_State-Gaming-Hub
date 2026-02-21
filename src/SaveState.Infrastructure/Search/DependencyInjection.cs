using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Search.Services;
using SaveState.Infrastructure.Search.Providers;
using SaveState.Infrastructure.Search.Services;

namespace SaveState.Infrastructure.Search;

/// <summary>
/// Extension methods for registering universal search services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds universal search services to the dependency injection container.
    /// </summary>
    public static IServiceCollection AddUniversalSearch(this IServiceCollection services)
    {
        // Core search service
        services.AddSingleton<IUniversalSearchService, UniversalSearchService>();

        // Embedding client - use OpenAI if available, fallback to local
        services.AddSingleton<IOpenAiEmbeddingClient>(sp =>
        {
            var llmProvider = sp.GetService<SaveState.Core.Ai.Services.ILlmProvider>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OpenAiEmbeddingClient>>();

            if (llmProvider != null && llmProvider.IsAvailable)
            {
                return new OpenAiEmbeddingClient(llmProvider, logger);
            }

            // Fallback to local embeddings for development/testing
            var localLogger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LocalEmbeddingClient>>();
            return new LocalEmbeddingClient(localLogger);
        });

        // Search providers
        services.AddSingleton<ISearchProvider, GameSearchProvider>();
        services.AddSingleton<ISearchProvider, SettingsSearchProvider>();
        services.AddSingleton<ISearchProvider, ActionSearchProvider>();
        services.AddSingleton<ISearchProvider, SaveStateSearchProvider>();
        services.AddSingleton<ISearchProvider, CommandSearchProvider>();

        return services;
    }
}
