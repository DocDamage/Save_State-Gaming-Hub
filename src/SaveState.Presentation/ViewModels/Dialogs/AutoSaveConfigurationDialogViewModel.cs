using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the auto-save configuration dialog.
/// </summary>
public partial class AutoSaveConfigurationDialogViewModel : ObservableObject
{
    // Validation constants
    private const int MinAutoSaves = 1;
    private const int MaxAutoSavesAllowed = 100;

    [ObservableProperty]
    private bool _autoSaveEnabled = true;

    [ObservableProperty]
    private string _selectedInterval = "5 min";

    [ObservableProperty]
    private int _maxAutoSaves = 10;

    [ObservableProperty]
    private bool _createAutoSaveOnGameStart;

    [ObservableProperty]
    private bool _createAutoSaveOnBossEncounter;

    [ObservableProperty]
    private bool _notifyOnAutoSave = true;

    [ObservableProperty]
    private bool _compressAutoSaves = true;

    public ObservableCollection<string> AvailableIntervals { get; } = new()
    {
        "1 min",
        "5 min",
        "10 min",
        "15 min",
        "30 min",
        "1 hour"
    };

    public ObservableCollection<int> MaxAutoSavesOptions { get; } = new()
    {
        5, 10, 15, 20, 25, 30, 50, 100
    };

    public AutoSaveConfigurationDialogViewModel(
        bool autoSaveEnabled = true,
        string selectedInterval = "5 min",
        int maxAutoSaves = 10)
    {
        AutoSaveEnabled = autoSaveEnabled;
        SelectedInterval = selectedInterval;
        MaxAutoSaves = maxAutoSaves;
    }

    partial void OnMaxAutoSavesChanged(int value)
    {
        // Clamp to valid range
        if (value < MinAutoSaves || value > MaxAutoSavesAllowed)
        {
            MaxAutoSaves = Math.Clamp(value, MinAutoSaves, MaxAutoSavesAllowed);
        }
    }

    [RelayCommand]
    private void Save()
    {
        // Ensure value is within range
        var clampedMaxAutoSaves = Math.Clamp(MaxAutoSaves, MinAutoSaves, MaxAutoSavesAllowed);

        var result = new AutoSaveConfigurationResult(
            AutoSaveEnabled: AutoSaveEnabled,
            Interval: SelectedInterval,
            MaxAutoSaves: clampedMaxAutoSaves,
            CreateOnGameStart: CreateAutoSaveOnGameStart,
            CreateOnBossEncounter: CreateAutoSaveOnBossEncounter,
            NotifyOnAutoSave: NotifyOnAutoSave,
            CompressAutoSaves: CompressAutoSaves);

        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }

    private void CloseDialog(AutoSaveConfigurationResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        AutoSaveEnabled = true;
        SelectedInterval = "5 min";
        MaxAutoSaves = 10;
        CreateAutoSaveOnGameStart = false;
        CreateAutoSaveOnBossEncounter = false;
        NotifyOnAutoSave = true;
        CompressAutoSaves = true;
    }
}

