using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Application.RetroArch.Commands;
using SaveState.Core.RetroArch;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.RetroArch;

/// <summary>
/// ViewModel for managing RetroArch cores.
/// </summary>
public partial class RetroArchCoreManagerViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly ILogger<RetroArchCoreManagerViewModel> _logger;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<RetroArchCore> _installedCores = new();

    [ObservableProperty]
    private ObservableCollection<RetroArchCore> _availableCores = new();

    [ObservableProperty]
    private ObservableCollection<RetroArchCore> _filteredCores = new();

    [ObservableProperty]
    private RetroArchCore? _selectedCore;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public RetroArchCoreManagerViewModel(
        IMediator mediator,
        ILogger<RetroArchCoreManagerViewModel> logger,
        INotificationService notificationService)
    {
        _mediator = mediator;
        _logger = logger;
        _notificationService = notificationService;
    }

    /// <summary>
    /// Filters cores when search query changes.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        FilterCores();
    }

    private void FilterCores()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            FilteredCores = new ObservableCollection<RetroArchCore>(AvailableCores);
            return;
        }

        var query = SearchQuery.Trim().ToLowerInvariant();
        var filtered = AvailableCores.Where(c =>
            c.Name.ToLowerInvariant().Contains(query) ||
            c.System.ToLowerInvariant().Contains(query)).ToList();

        FilteredCores = new ObservableCollection<RetroArchCore>(filtered);
    }

    /// <summary>
    /// Installs the selected core.
    /// </summary>
    [RelayCommand]
    private async Task InstallCoreAsync(RetroArchCore? core)
    {
        if (core is null) return;

        try
        {
            IsLoading = true;

            var result = await _mediator.Send(new InstallCoreCommand(core.Name));
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Installed {core.Name}", "Core Installed");
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to install core", "Installation Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to install RetroArch core: {CoreName}", core.Name);
            _notificationService.ShowError("Failed to install core. Please try again.", "Installation Failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Updates all installed cores.
    /// </summary>
    [RelayCommand]
    private async Task UpdateAllCoresAsync()
    {
        try
        {
            IsLoading = true;

            var result = await _mediator.Send(new UpdateAllCoresCommand());
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess("All cores updated to latest versions", "Update Complete");
            }
            else
            {
                _notificationService.ShowWarning(result.Error ?? "Some cores failed to update", "Update Warning");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update RetroArch cores");
            _notificationService.ShowError("Failed to update cores. Please try again.", "Update Failed");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Uninstalls the selected core.
    /// </summary>
    [RelayCommand]
    private async Task UninstallCoreAsync(RetroArchCore? core)
    {
        if (core is null) return;

        try
        {
            var result = await _mediator.Send(new UninstallCoreCommand(core.Name));
            if (result.IsSuccess)
            {
                _notificationService.ShowSuccess($"Uninstalled {core.Name}", "Core Uninstalled");
            }
            else
            {
                _notificationService.ShowError(result.Error ?? "Failed to uninstall core", "Uninstall Failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to uninstall RetroArch core: {CoreName}", core.Name);
            _notificationService.ShowError("Failed to uninstall core. Please try again.", "Uninstall Failed");
        }
    }
}
