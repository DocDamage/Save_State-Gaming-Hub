using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using SaveState.Core.Configuration;
using SaveState.Core.Common;
using SaveState.Core.Common.Configuration;
using SaveState.Core.Common.Services;
using SaveState.Core.Monitoring;
using SaveState.Infrastructure.Logging;
using SaveState.Infrastructure.Services;
using SaveState.Infrastructure.UserManagement;
using SaveState.Infrastructure.Ai.Providers;
using SaveState.Infrastructure.AiCoOp.Services;
using SaveState.Core.GameLibrary.Services;
using SaveState.Core.GameLibrary;
using SaveState.Core.RomManagement;
using SaveState.Core.Mugen.ComboDatabase.Services;
using SaveState.Core.Mugen.Services;
using SaveState.Core.OpenMK.Repositories;
using SaveState.Core.OpenMK.Services;
using SaveState.Infrastructure.Repositories;
using SaveState.Infrastructure.Mugen;
using SaveState.Infrastructure.Mugen.ComboDatabase;
using SaveState.Infrastructure.Mugen.IkemenGo;
using SaveState.Infrastructure.Mugen.ComboDatabase.Managers;
using SaveState.Infrastructure.Mugen.IkemenGo.Managers;
using SaveState.Infrastructure.Mugen.SpriteAnimation.Managers;
using SaveState.Application.Mugen.Services.PredictiveAnalytics.Managers;
using SaveState.Application.Mugen.Services.Blockchain.Managers;
using SaveState.Application.Mugen.Services.Graphics.Managers;
using SaveState.Application.Mugen.Services.SoundDesign;
using SaveState.Infrastructure.Mugen.StoryMode;
using SaveState.Infrastructure.Mugen.StoryMode.Managers;
using SaveState.Infrastructure.Mugen.PerformanceProfiler;
using SaveState.Infrastructure.Mugen.PerformanceProfiler.Managers;
using SaveState.Application.Mugen.Services.SymbioticPartner;
using SaveState.Infrastructure.Mugen.ReplayAnalysis.Managers;
using SaveState.Infrastructure.OpenMK;
using SaveState.Infrastructure.OpenMK.Services.OpenMK;
using SaveState.Core.Ai.Knowledge;
using SaveState.Core.Ai.Services;
using SaveState.Core.Ai.Memory;
using SaveState.Core.Ai.Learning;
using SaveState.Core.Ai.Context;
using SaveState.Core.Ai.Voice;
using SaveState.Core.Sync;
using SaveState.Infrastructure.Ai;
using SaveState.Infrastructure.Ai.Resilience;
using SaveState.Infrastructure.Ai.Knowledge;
using SaveState.Infrastructure.Ai.Memory;
using SaveState.Infrastructure.Ai.Learning;
using SaveState.Infrastructure.Ai.Context;
using SaveState.Infrastructure.Ai.Voice;
using SaveState.Infrastructure.Assistant;
using SaveState.Infrastructure.Sync;
using SaveState.Infrastructure.External;
using SaveState.Infrastructure.GameLibrary.Services;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.Persistence;
using SaveState.Infrastructure.RomManagement.Services;
using SaveState.Infrastructure.Common;
using SaveState.Infrastructure.Health;
using SaveState.Infrastructure.CrossPlatform;
using SaveState.Infrastructure.Resilience;
using SaveState.Infrastructure.Performance;
using SaveState.Infrastructure.DataPortability;
using SaveState.Core.DataPortability;
using SaveState.Core.RetroArch.Services;
using SaveState.Infrastructure.RetroArch;
using SaveState.Infrastructure.RetroArch.RetroArchCloudSync;
using SaveState.Infrastructure.Subscriptions;
using SaveState.Core.Intelligence.Recommendations.Services;
using SaveState.Core.Analytics.Services;
using SaveState.Core.Intelligence.AiContent.Services;
using SaveState.Core.ContentGeneration.Services;
using SaveState.Core.Intelligence.Search.Services;
using SaveState.Core.Search.Services;
using SaveState.Infrastructure.Intelligence.Recommendations;
using SaveState.Infrastructure.Analytics.Services;
using SaveState.Infrastructure.Intelligence.AiContent;
using SaveState.Infrastructure.ContentGeneration.Services;
using SaveState.Infrastructure.Intelligence.Search;
using SaveState.Infrastructure.Search.Services;
using SaveState.Infrastructure.Search.Providers;
using SaveState.Infrastructure.GameDeals;
using SaveState.Infrastructure.SmartLauncher;
using SaveState.Core.SmartLauncher;
using SaveState.Infrastructure.OpenApi;

namespace SaveState.Infrastructure;

public static partial class DependencyInjection
{
    /// <summary>
    /// Adds configuration options with validation.
    /// </summary>
    private static void AddConfigurationOptions(IServiceCollection services, IConfiguration configuration)
    {
        // Configuration with validation
        services.AddOptions<OpenAiOptions>()
            .Bind(configuration.GetSection("OpenAi"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.BaseUrl) &&
                       !string.IsNullOrEmpty(options.ApiKey) &&
                       !string.IsNullOrEmpty(options.DefaultModel) &&
                       Uri.IsWellFormedUriString(options.BaseUrl, UriKind.Absolute);
            }, "Invalid OpenAI configuration")
            .ValidateOnStart();

        services.AddOptions<GroqOptions>()
            .Bind(configuration.GetSection("Groq"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.BaseUrl) &&
                       !string.IsNullOrEmpty(options.ApiKey) &&
                       !string.IsNullOrEmpty(options.DefaultModel) &&
                       Uri.IsWellFormedUriString(options.BaseUrl, UriKind.Absolute);
            }, "Invalid Groq configuration")
            .ValidateOnStart();

        services.AddOptions<SaveState.Core.Common.Configuration.AiOptions>()
            .Bind(configuration.GetSection("Ai"))
            .ValidateDataAnnotations()
            .Validate(options => options != null, "AI options cannot be null")
            .ValidateOnStart();

        services.AddOptions<SteamOptions>()
            .Bind(configuration.GetSection("Steam"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.ApiKey) &&
                       !string.IsNullOrEmpty(options.SteamId) &&
                       long.TryParse(options.SteamId, out _);
            }, "Invalid Steam configuration")
            .ValidateOnStart();

        services.AddOptions<GogOptions>()
            .Bind(configuration.GetSection("Gog"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.Username) &&
                       !string.IsNullOrEmpty(options.Password);
            }, "Invalid GOG configuration")
            .ValidateOnStart();

        services.AddOptions<EpicOptions>()
            .Bind(configuration.GetSection("Epic"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.AccountId) &&
                       !string.IsNullOrEmpty(options.AuthToken);
            }, "Invalid Epic configuration")
            .ValidateOnStart();

        services.AddOptions<IgdbOptions>()
            .Bind(configuration.GetSection("Igdb"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.ClientId) &&
                       !string.IsNullOrEmpty(options.ClientSecret);
            }, "Invalid IGDB configuration")
            .ValidateOnStart();

        services.AddOptions<SteamGridDbOptions>()
            .Bind(configuration.GetSection("SteamGridDB"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return !string.IsNullOrEmpty(options.ApiKey) &&
                       Uri.IsWellFormedUriString(options.BaseUrl, UriKind.Absolute) &&
                       options.MaxConcurrentRequests > 0 &&
                       options.CacheDurationHours > 0;
            }, "Invalid SteamGridDB configuration")
            .ValidateOnStart();

        services.AddOptions<ResilienceConfig>()
            .Bind(configuration.GetSection("Resilience"))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return options.CircuitBreakerThreshold > 0 &&
                       options.CircuitBreakerDurationMs > 0 &&
                       options.MaxRetries >= 0 &&
                       options.InitialRetryDelayMs > 0 &&
                       options.RetryBackoffMultiplier >= 1.0 &&
                       options.DefaultTimeoutMs > 0;
            }, "Invalid resilience configuration")
            .ValidateOnStart();

        // Additional Configuration Validation
        services.AddOptions<SaveState.Core.Common.Configuration.MemoryOptions>()
            .Bind(configuration.GetSection(SaveState.Core.Common.Configuration.MemoryOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Core.Common.Configuration.ApplicationOptions>()
            .Bind(configuration.GetSection(SaveState.Core.Common.Configuration.ApplicationOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Core.Common.Configuration.DatabaseOptions>()
            .Bind(configuration.GetSection(SaveState.Core.Common.Configuration.DatabaseOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Application.Common.Options.LaunchOptions>()
            .Bind(configuration.GetSection("Launch"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Application.AiGaming.Options.CheatDetectionOptions>()
            .Bind(configuration.GetSection("CheatDetection"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Core.Common.Configuration.RateLimitingOptions>()
            .Bind(configuration.GetSection(SaveState.Core.Common.Configuration.RateLimitingOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Authentication Configuration
        services.AddOptions<SaveState.Core.UserManagement.Configuration.JwtOptions>()
            .Bind(configuration.GetSection(SaveState.Core.UserManagement.Configuration.JwtOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<SaveState.Core.UserManagement.Configuration.AuthenticationOptions>()
            .Bind(configuration.GetSection(SaveState.Core.UserManagement.Configuration.AuthenticationOptions.Section))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Localization Configuration
        services.AddOptions<SaveState.Core.Configuration.LocalizationOptions>()
            .Bind(configuration.GetSection(SaveState.Core.Configuration.LocalizationOptions.Section))
            .ValidateDataAnnotations()
            .Validate(options =>
            {
                return options.SupportedCultures.Contains(options.DefaultCulture) &&
                       options.CacheDurationDays > 0;
            }, "Invalid localization configuration")
            .ValidateOnStart();

        services.AddOptions<MugenOptions>()
            .Bind(configuration.GetSection(MugenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<CloudSyncOptions>()
            .Bind(configuration.GetSection(CloudSyncOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }
}
