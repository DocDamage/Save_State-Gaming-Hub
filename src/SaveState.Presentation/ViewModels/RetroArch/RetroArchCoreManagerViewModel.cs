using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.RetroArch;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.RetroArch;

/// <summary>
/// ViewModel for managing RetroArch cores.
/// </summary>
public partial class RetroArchCoreManagerViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<RetroArchCore> _installedCores = new();

    [ObservableProperty]
    private ObservableCollection<RetroArchCore> _availableCores = new();

    [ObservableProperty]
    private RetroArchCore? _selectedCore;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Filters cores when search query changes.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        // TODO: Filter cores based on search query
    }

    /// <summary>
    /// Installs the selected core.
    /// </summary>
    [RelayCommand]
    private async Task InstallCoreAsync(RetroArchCore? core)
    {
        if (core is null) return;
        IsLoading = true;
        // TODO: Install core via mediator
        await Task.Delay(500);
        IsLoading = false;
    }

    /// <summary>
    /// Updates all installed cores.
    /// </summary>
    [RelayCommand]
    private async Task UpdateAllCoresAsync()
    {
        IsLoading = true;
        // TODO: Update all cores via mediator
        await Task.Delay(1000);
        IsLoading = false;
    }

    /// <summary>
    /// Uninstalls the selected core.
    /// </summary>
    [RelayCommand]
    private async Task UninstallCoreAsync(RetroArchCore? core)
    {
        if (core is null) return;
        // TODO: Uninstall core via mediator
        await Task.CompletedTask;
    }
}
