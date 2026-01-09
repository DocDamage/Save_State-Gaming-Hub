using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for setting a price alert.
/// </summary>
public partial class PriceAlertDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _gameTitle = string.Empty;

    [ObservableProperty]
    private double _currentPrice;

    [ObservableProperty]
    private double _targetPrice;

    [ObservableProperty]
    private bool _emailNotification = true;

    [ObservableProperty]
    private bool _inAppNotification = true;

    [ObservableProperty]
    private ObservableCollection<string> _stores = new()
    {
        "Any Store",
        "Steam",
        "Epic Games Store",
        "GOG",
        "Humble Store",
        "Green Man Gaming"
    };

    [ObservableProperty]
    private string _selectedStore = "Any Store";

    public bool CanSave => TargetPrice > 0 && (EmailNotification || InAppNotification);

    public PriceAlertDialogViewModel(string gameTitle, double currentPrice)
    {
        GameTitle = gameTitle;
        CurrentPrice = currentPrice;
        TargetPrice = currentPrice > 0 ? currentPrice * 0.8 : 10.00; // Default to 20% off
    }

    partial void OnTargetPriceChanged(double value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnEmailNotificationChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnInAppNotificationChanged(bool value)
    {
        OnPropertyChanged(nameof(CanSave));
    }

    [RelayCommand]
    private void Save()
    {
        if (!CanSave) return;

        var result = new PriceAlertResult(
            TargetPrice,
            SelectedStore,
            EmailNotification,
            InAppNotification);

        // Close dialog with result
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        // Close dialog without result
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(null);
        }
    }
}
