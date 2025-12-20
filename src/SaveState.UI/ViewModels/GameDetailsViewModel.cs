using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using Serilog;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class GameDetailsViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger = Log.ForContext<GameDetailsViewModel>();

    [ObservableProperty]
    private Game _game;

    [ObservableProperty]
    private bool _isLoadingMetadata;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public IAsyncRelayCommand LaunchCommand { get; }
    public IAsyncRelayCommand FetchMetadataCommand { get; }
    public IRelayCommand BackCommand { get; }

    public GameDetailsViewModel(IServiceProvider serviceProvider, Game game, Action goBack)
    {
        _serviceProvider = serviceProvider;
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
            var providers = _serviceProvider.GetServices<IGameProvider>();
            var provider = providers.FirstOrDefault(p =>
                p.Name.Equals(Game.Source, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Equals(Game.Source, StringComparison.OrdinalIgnoreCase));

            if (provider != null)
            {
                await provider.LaunchGameAsync(Game);
            }
            else if (!string.IsNullOrEmpty(Game.LaunchCommand))
            {
                Process.Start(new ProcessStartInfo(Game.LaunchCommand) { UseShellExecute = true });
            }
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
