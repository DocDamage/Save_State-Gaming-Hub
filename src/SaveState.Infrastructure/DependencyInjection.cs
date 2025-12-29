using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SaveState.Core.Configuration;
using SaveState.Infrastructure.Ai.Providers;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.RomManagement;
using SaveState.Core.Mugen.Services;
using SaveState.Infrastructure.Repositories;
using SaveState.Infrastructure.Mugen;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Ai.Services;
using SaveState.Core.Ai.Memory;
using SaveState.Core.Ai.Learning;
using SaveState.Infrastructure.Ai;
using SaveState.Infrastructure.Ai.Resilience;
using SaveState.Infrastructure.Ai.Knowledge;
using SaveState.Infrastructure.Ai.Memory;
using SaveState.Infrastructure.Ai.Learning;
using SaveState.Infrastructure.External;
using SaveState.Infrastructure.GameLibrary.Services;
using SaveState.Infrastructure.Persistence;
using SaveState.Infrastructure.RomManagement.Services;

namespace SaveState.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ISaveStateDbContext, SaveStateDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        // Repositories
        services.AddScoped<IGameRepository, GameRepository>();
        services.AddScoped<IPlatformRepository, PlatformRepository>();
        services.AddScoped<IRomFileRepository, RomFileRepository>();
        services.AddScoped<IEmulatorRepository, EmulatorRepository>();
        services.AddScoped<IAchievementRepository, AchievementRepository>();
        services.AddScoped<IPlatformExtensionRegistry, PlatformExtensionRegistry>();

        // MUGEN Services
        services.AddScoped<IMugenCharacterLoader, MugenCharacterLoader>();
        services.AddScoped<IMugenLauncher, MugenLauncher>();

        // Game Providers
        services.AddScoped<IGameProvider, SteamProvider>();
        services.AddScoped<IGameProvider, GogProvider>();
        services.AddScoped<IGameProvider, EpicProvider>();

        // External API Clients
        services.AddHttpClient<ISteamApiClient, SteamApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.steampowered.com/");
        });

        services.AddHttpClient<IGogApiClient, GogApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.gog.com/");
        });

        services.AddHttpClient<IEpicApiClient, EpicApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.epicgames.dev/");
        });

        // Metadata Services
        services.AddMemoryCache();
        services.AddScoped<IMetadataService, IgdbMetadataService>();
        services.Decorate<IMetadataService, ResilientMetadataService>();
        services.AddHttpClient<IIgdbApiClient, IgdbApiClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.igdb.com/v4/");
        });

        // Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<SaveStateDbContext>();

        // AI Providers (with HttpClient)
        services.AddHttpClient<ILlmProvider, OpenAiProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        });

        services.AddHttpClient<ILlmProvider, GroqProvider>((sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<GroqOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.ApiKey}");
        });

        // Configuration
        services.Configure<OpenAiOptions>(configuration.GetSection("OpenAi"));
        services.Configure<GroqOptions>(configuration.GetSection("Groq"));
        services.Configure<AiOptions>(configuration.GetSection("Ai"));
        services.Configure<SteamOptions>(configuration.GetSection("Steam"));
        services.Configure<GogOptions>(configuration.GetSection("Gog"));
        services.Configure<EpicOptions>(configuration.GetSection("Epic"));
        services.Configure<IgdbOptions>(configuration.GetSection("Igdb"));

        // AI Services
        services.AddScoped<IKnowledgeStore, SqliteVectorStore>();
        services.AddScoped<SemanticKnowledgeClient>();
        services.AddScoped<IShortTermMemory, EnhancedShortTermMemory>();
        services.AddScoped<IAiOrchestrator, AiOrchestrator>();
        services.AddScoped<AiResiliencePolicy>();
        services.AddScoped<IFeedbackLoop, LocalLearningService>();
        services.AddScoped<IChaosTester, ChaosTester>();


        return services;
    }
}
