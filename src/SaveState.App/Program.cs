using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SaveState.Core.Data;
using SaveState.Core.Infrastructure;
using SaveState.Core.Interfaces;
using SaveState.Core.Providers;
using SaveState.Core.Services;
using SaveState.UI;
using SaveState.UI.ViewModels;
using Serilog;
using Microsoft.EntityFrameworkCore;

namespace SaveState.App;

class Program
{
    public static IHost? AppHost { get; private set; }

    [STAThread]
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/savestate.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            var singleInstance = new SingleInstanceLock("SaveStateReborn");
            if (!singleInstance.TryAcquire())
            {
                // Use ConfigureAwait(false) to avoid deadlocks in console app context
                await singleInstance.SendCommandToInstance("ACTIVATE", args).ConfigureAwait(false);
                return;
            }

            var host = CreateHostBuilder(args).Build();
            AppHost = host;

            // Migrate database and seed data
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();
                db.Database.EnsureCreated();

                // Seed sample data if empty
                if (!db.Games.Any())
                {
                    var pcPlatform = new SaveState.Core.Entities.Platform { Name = "PC" };
                    db.Platforms.Add(pcPlatform);

                    db.Games.AddRange(
                        new SaveState.Core.Entities.Game
                        {
                            Title = "Half-Life 2",
                            SortTitle = "Half-Life 2",
                            InstallPath = @"C:\Games\HalfLife2",
                            Platform = pcPlatform,
                            Source = "Steam"
                        },
                        new SaveState.Core.Entities.Game
                        {
                            Title = "Portal 2",
                            SortTitle = "Portal 2",
                            InstallPath = @"C:\Games\Portal2",
                            Platform = pcPlatform,
                            Source = "Steam"
                        },
                        new SaveState.Core.Entities.Game
                        {
                            Title = "The Witcher 3: Wild Hunt",
                            SortTitle = "Witcher 3",
                            InstallPath = @"C:\Games\Witcher3",
                            Platform = pcPlatform,
                            Source = "GOG"
                        },
                        new SaveState.Core.Entities.Game
                        {
                            Title = "Cyberpunk 2077",
                            SortTitle = "Cyberpunk 2077",
                            InstallPath = @"C:\Games\Cyberpunk",
                            Platform = pcPlatform,
                            Source = "Steam"
                        },
                        new SaveState.Core.Entities.Game
                        {
                            Title = "Baldur's Gate 3",
                            SortTitle = "Baldurs Gate 3",
                            InstallPath = @"C:\Games\BG3",
                            Platform = pcPlatform,
                            Source = "Steam"
                        }
                    );
                    db.SaveChanges();
                    Log.Information("Seeded {Count} sample games", 5);
                }
            }

            // Assign services to UI
            SaveState.UI.App.Services = host.Services;

            // Start Avalonia
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<SaveState.UI.App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureServices((hostContext, services) =>
            {
                // Data Layer
                services.AddDbContext<SaveStateDbContext>(options =>
                    options.UseSqlite("Data Source=savestate.db"));

                // HttpClient for metadata services
                services.AddHttpClient();

                // Game Providers
                services.AddSingleton<IGameProvider, SteamProvider>();
                services.AddSingleton<IGameProvider, GogProvider>();
                services.AddSingleton<IGameProvider, EpicProvider>();
                services.AddSingleton<IGameProvider, XboxProvider>();
                services.AddSingleton<IGameProvider, EaProvider>();
                services.AddSingleton<IGameProvider, UbisoftProvider>();
                services.AddSingleton<ProviderManager>();

                // Metadata Services
                services.AddSingleton<IMetadataProvider, IgdbService>();
                services.AddSingleton<IMetadataProvider, SteamGridDbService>();

                // Services - Scoped to match DbContext lifetime
                services.AddScoped<IGameService, GameService>();
                services.AddScoped<IEmulatorService, EmulatorService>();
                services.AddScoped<ICollectionService, CollectionService>();
                services.AddScoped<ImportExportService>();
                services.AddSingleton<RomScannerService>();

                // AI Services
                services.AddSingleton<IAiService, GeminiService>();
                services.AddSingleton<CheatAgentService>();

                // RAG Services (Retrieval-Augmented Generation)
                services.AddSingleton<IEmbeddingService, EmbeddingService>();
                services.AddScoped<IVectorStoreService, VectorStoreService>();
                services.AddScoped<IKnowledgeService, KnowledgeService>();

                // MBAD Services (Memory-Based Anomaly Detection)
                services.AddSingleton<IMemoryAnomalyService, MemoryAnomalyService>();

                // Cheat & System Services
                services.AddSingleton<IProcessService, ProcessService>();
                services.AddSingleton<IMemoryScannerService, MemoryScannerService>();
                services.AddSingleton<ITrainerService, TrainerService>();

                services.AddSingleton<IVoiceService, VoiceService>();

                // ============ SaveState.Core Services (AI, Emulation, etc.) ============
                services.AddSaveStateCoreServices();

                // ViewModels - Main
                services.AddSingleton<MainWindowViewModel>();
                services.AddTransient<GameGridViewModel>();
                services.AddTransient<SettingsViewModel>();
                services.AddTransient<RomManagerViewModel>();
                services.AddTransient<AiAssistantViewModel>();
                services.AddTransient<StatisticsViewModel>();
                services.AddTransient<CollectionsViewModel>();
                services.AddTransient<KnowledgeViewModel>();

                // ViewModels - AI & Emulator Enhancements
                services.AddTransient<AiSettingsViewModel>();
                services.AddTransient<LiveCommentaryViewModel>();
                services.AddTransient<DreamSequenceViewModel>();
                services.AddTransient<TimeCapsuleViewModel>();
                services.AddTransient<MemoryEvolutionViewModel>();
                services.AddTransient<ShaderStudioViewModel>();
                services.AddTransient<RetroRewindViewModel>();

                // ViewModels - MUGEN/Fighting
                services.AddTransient<MugenPlayerViewModel>();
                services.AddTransient<CrossGameBattleViewModel>();
                services.AddTransient<CharacterFusionViewModel>();
                services.AddTransient<TrainerGeneratorViewModel>();

                // IPC
                services.AddHostedService<IpcWorker>();
            });
}

public class IpcWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Start gRPC server on Named Pipes here
        await Task.Delay(-1, stoppingToken);
    }
}
