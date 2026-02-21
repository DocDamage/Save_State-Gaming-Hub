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
    // Validation constants
    private const double MinTargetPrice = 0.01;
    private const double MaxTargetPrice = 9999.99;

    [ObservableProperty]
    private string _gameTitle = string.Empty;

    [ObservableProperty]
    private double _currentPrice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsTargetPriceValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private double _targetPrice;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreNotificationsValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
    private bool _emailNotification = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AreNotificationsValid))]
    [NotifyPropertyChangedFor(nameof(CanSave))]
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

    [ObservableProperty]
    private string _validationError = string.Empty;

    /// <summary>
    /// Gets whether the target price is valid.
    /// </summary>
    public bool IsTargetPriceValid => 
        TargetPrice >= MinTargetPrice && 
        TargetPrice <= MaxTargetPrice &&
        (CurrentPrice <= 0 || TargetPrice <= CurrentPrice);

    /// <summary>
    /// Gets whether at least one notification method is selected.
    /// </summary>
    public bool AreNotificationsValid => EmailNotification || InAppNotification;

    /// <summary>
    /// Gets whether there are any validation errors.
    /// </summary>
    public bool HasValidationErrors => !IsTargetPriceValid || !AreNotificationsValid;

    /// <summary>
    /// Gets whether the save button should be enabled.
    /// </summary>
    public bool CanSave => IsTargetPriceValid && AreNotificationsValid;

    public PriceAlertDialogViewModel(string gameTitle, double currentPrice)
    {
        GameTitle = gameTitle;
        CurrentPrice = currentPrice;
        // Default to 20% off, clamped to valid range
        var defaultTarget = currentPrice > 0 ? currentPrice * 0.8 : 10.00;
        TargetPrice = Math.Clamp(defaultTarget, MinTargetPrice, MaxTargetPrice);
    }

    partial void OnTargetPriceChanged(double value)
    {
        UpdateValidationError();
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnEmailNotificationChanged(bool value)
    {
        UpdateValidationError();
        OnPropertyChanged(nameof(CanSave));
    }

    partial void OnInAppNotificationChanged(bool value)
    {
        UpdateValidationError();
        OnPropertyChanged(nameof(CanSave));
    }

    private void UpdateValidationError()
    {
        if (!IsTargetPriceValid)
        {
            if (TargetPrice < MinTargetPrice)
                ValidationError = $"Target price must be at least {MinTargetPrice:C}.";
            else if (TargetPrice > MaxTargetPrice)
                ValidationError = $"Target price must not exceed {MaxTargetPrice:C}.";
            else if (CurrentPrice > 0 && TargetPrice > CurrentPrice)
                ValidationError = "Target price should be less than or equal to current price.";
            else
                ValidationError = "Please enter a valid target price.";
        }
        else if (!AreNotificationsValid)
        {
            ValidationError = "Please select at least one notification method.";
        }
        else
        {
            ValidationError = string.Empty;
        }
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

        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }

    private void CloseDialog(PriceAlertResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}
