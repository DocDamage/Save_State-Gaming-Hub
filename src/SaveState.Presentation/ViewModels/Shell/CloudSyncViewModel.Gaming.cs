using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Application.CloudServices.Queries;
using SaveState.Application.Sync.Queries;
using SaveState.Core.SaveStates.Services.DTOs;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// Partial class containing cloud gaming operations for CloudSyncViewModel.
/// </summary>
public partial class CloudSyncViewModel
{
    /// <summary>
    /// Command to browse the cloud gaming catalog.
    /// </summary>
    [RelayCommand]
    private async Task BrowseCatalogAsync()
    {
        try
        {
            var catalogResult = await _cloudCatalogService.GetCatalogAsync();
            if (!catalogResult.IsSuccess || catalogResult.Value == null)
            {
                _notificationService.ShowError("Failed to load cloud catalog");
                return;
            }

            var popularGames = catalogResult.Value.Games
                .OrderByDescending(g => g.Providers.Count)
                .ThenBy(g => g.Title)
                .Take(8)
                .ToList();

            if (popularGames.Count == 0)
            {
                _notificationService.ShowInfo("No popular cloud games available right now");
                return;
            }

            TopCloudGames.Clear();
            foreach (var entry in popularGames)
            {
                TopCloudGames.Add(entry);
            }

            _notificationService.ShowInfo("Top Cloud Games refreshed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to browse cloud catalog");
            _notificationService.ShowError("Failed to browse catalog");
        }
    }

    /// <summary>
    /// Loads the cloud gaming catalog.
    /// </summary>
    private async Task LoadCloudCatalogAsync()
    {
        try
        {
            var catalogResult = await _cloudCatalogService.GetCatalogAsync();
            if (!catalogResult.IsSuccess || catalogResult.Value == null)
            {
                _notificationService.ShowWarning("Unable to load cloud catalog metadata");
                return;
            }

            var catalog = catalogResult.Value;
            AvailableCloudGamesCount = catalog.Games.Count;

            TopCloudGames.Clear();
            foreach (var entry in catalog.Games
                .OrderByDescending(g => g.Providers.Count)
                .ThenBy(g => g.Title)
                .Take(5))
            {
                TopCloudGames.Add(entry);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load cloud catalog metadata");
        }
    }

    /// <summary>
    /// Loads the available cloud providers.
    /// </summary>
    private async Task LoadCloudProvidersAsync()
    {
        try
        {
            var providersResult = await _mediator.Send(new GetCloudProvidersQuery());
            if (providersResult.IsSuccess && providersResult.Value is not null)
            {
                CloudProviders.Clear();
                foreach (var provider in providersResult.Value)
                {
                    CloudProviders.Add(provider);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load cloud providers");
        }
    }

    /// <summary>
    /// Loads the active cloud gaming sessions.
    /// </summary>
    private async Task LoadActiveCloudSessionsAsync()
    {
        try
        {
            var sessionsResult = await _mediator.Send(new GetActiveCloudSessionsQuery());
            if (sessionsResult.IsSuccess && sessionsResult.Value is not null)
            {
                ActiveSessions.Clear();
                foreach (var session in sessionsResult.Value)
                {
                    ActiveSessions.Add(session);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load active cloud sessions");
        }
    }
}
