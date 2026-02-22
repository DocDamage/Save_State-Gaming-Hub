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
    /// Adds diagnostics, health checks, and monitoring services.
    /// </summary>
    private static void AddDiagnosticsServices(IServiceCollection services)
    {
        // Health Checks
        services.AddHealthChecks()
            .AddDbContextCheck<SaveStateDbContext>("Database", tags: new[] { "database", "infrastructure" })
            .AddCheck<DatabaseHealthCheck>("Database Detailed", tags: new[] { "database", "infrastructure" })
            .AddCheck<MetricsHealthCheck>("Application Metrics", tags: new[] { "metrics", "performance" })
            .AddCheck<ExternalApiHealthCheck>("External APIs", tags: new[] { "external", "apis", "dependencies" })
            .AddCheck<ResourceHealthCheck>("System Resources", tags: new[] { "system", "resources", "infrastructure" })
            .AddCheck<DependencyHealthCheck>("Dependencies", tags: new[] { "dependencies", "infrastructure" })
            .AddCheck<PerformanceHealthCheck>("Performance", tags: new[] { "performance", "monitoring" });

        // Application Metrics
        services.AddSingleton<SaveState.Core.Monitoring.IApplicationMetrics, Monitoring.ApplicationMetricsService>();

        // Performance Monitoring
        services.AddSingleton<Monitoring.PerformanceMonitorService>();
        services.AddHostedService<Monitoring.PerformanceMonitorBackgroundService>();

        // Database Monitoring
        services.AddScoped<Monitoring.DatabaseConnectionMonitor>();

        // Cache Monitoring
        services.AddSingleton<ICachePerformanceMonitor, Monitoring.CachePerformanceMonitor>();

        // Error Tracking
        services.AddSingleton<Monitoring.ErrorTrackingService>();
    }
}
