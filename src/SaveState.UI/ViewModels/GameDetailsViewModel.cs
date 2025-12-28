using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using SaveState.Core.Services;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class GameDetailsViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IGameSessionMonitor _gameSessionMonitor;
    private readonly ILogger _logger = Log.ForContext<GameDetailsViewModel>();

    [ObservableProperty]
    private Game _game;

    [ObservableProperty]
    private bool _isLoadingMetadata;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public IAsyncRelayCommand LaunchCommand { get; }
    public IAsyncRelayCommand FetchMetadataCommand { get; }
    public IRelayCommand BackCommand { get; }

    public GameDetailsViewModel(IServiceProvider serviceProvider, IGameSessionMonitor gameSessionMonitor, Game game, Action goBack)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _gameSessionMonitor = gameSessionMonitor ?? throw new ArgumentNullException(nameof(gameSessionMonitor));
        _game = game;

        LaunchCommand = new AsyncRelayCommand(LaunchAsync);
        FetchMetadataCommand = new AsyncRelayCommand(FetchMetadataAsync);
        BackCommand = new RelayCommand(goBack);
    }

    private async Task LaunchAsync()
    {
        _logger.Information("Launching: {Title}", Game.Title);

        try
        {
            Process? process = null;

            var providers = _serviceProvider.GetServices<IGameProvider>();
            var provider = providers.FirstOrDefault(p =>
                p.Name.Equals(Game.Source, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Equals(Game.Source, StringComparison.OrdinalIgnoreCase));

            if (provider != null)
            {
                // Note: Providers should return the PID or process in future updates
                await provider.LaunchGameAsync(Game);
            }
            else if (!string.IsNullOrEmpty(Game.LaunchCommand))
            {
                process = Process.Start(new ProcessStartInfo(Game.LaunchCommand) { UseShellExecute = true });
            }

            // --- AI Session Integration ---
            // If we have a process ID (direct launch), use it.
            // If via provider (Steam/Epic), we might need to find the process or assume successful launch.
            // For now, if no process object, we use a placeholder PID (0) to signal "Monitoring Active".
            int pid = process?.Id ?? 0;

            // Start AI Session Monitoring
            await _gameSessionMonitor.StartMonitoringAsync(Game.Id, pid);
            IsMonitoring = true;
            _logger.Information("AI Session Monitoring started for {Game}", Game.Title);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to launch game");
            StatusMessage = "Failed to launch game";
        }
    }

    private async Task FetchMetadataAsync()
    {
        IsLoadingMetadata = true;
        StatusMessage = "Fetching metadata...";

        try
        {
            var metadataProviders = _serviceProvider.GetServices<IMetadataProvider>().ToList();

            // Try IGDB for metadata
            var igdb = metadataProviders.FirstOrDefault(p => p.Id == "igdb");
            if (igdb != null)
            {
                var metadata = await igdb.GetMetadataAsync(Game.Title);
                if (metadata != null)
                {
                    if (!string.IsNullOrEmpty(metadata.Description))
                        Game.Description = metadata.Description;
                    if (metadata.ReleaseDate.HasValue)
                        Game.ReleaseDate = metadata.ReleaseDate;
                    if (!string.IsNullOrEmpty(metadata.CoverUrl))
                        Game.CoverImage = metadata.CoverUrl;
                }
            }

            // Try SteamGridDB for cover
            var steamGridDb = metadataProviders.FirstOrDefault(p => p.Id == "steamgriddb");
            if (steamGridDb != null && string.IsNullOrEmpty(Game.CoverImage))
            {
                var coverPath = await steamGridDb.GetCoverImageAsync(Game.Title);
                if (!string.IsNullOrEmpty(coverPath))
                {
                    Game.CoverImage = coverPath;
                }
            }

            // Save changes
            using var scope = _serviceProvider.CreateScope();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
            await gameService.UpdateAsync(Game);

            StatusMessage = "Metadata updated!";
            OnPropertyChanged(nameof(Game));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to fetch metadata");
            StatusMessage = "Failed to fetch metadata";
        }
        finally
        {
            IsLoadingMetadata = false;
        }
    }
}
