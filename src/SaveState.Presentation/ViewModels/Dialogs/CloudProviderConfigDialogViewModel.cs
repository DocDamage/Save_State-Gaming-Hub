using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for cloud provider configuration dialog.
/// </summary>
public partial class CloudProviderConfigDialogViewModel : ObservableObject
{
    private const int MinAlertCooldownSeconds = 15;
    private const int MaxAlertCooldownSeconds = 600;

    [ObservableProperty]
    private string _selectedProvider = "GoogleDrive";

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _bucketName = string.Empty;

    [ObservableProperty]
    private bool _enableAutoSync = true;

    [ObservableProperty]
    private bool _enableBackgroundFailureAlerts = true;

    [ObservableProperty]
    private bool _enableBackgroundConflictAlerts = true;

    [ObservableProperty]
    private int _alertCooldownSeconds = 60;

    public CloudProviderConfigDialogViewModel(Services.CloudProviderConfigResult? currentSettings = null)
    {
        AvailableProviders = new ObservableCollection<string>
        {
            "GoogleDrive",
            "OneDrive",
            "Dropbox",
            "AWS S3",
            "Azure Blob Storage"
        };

        if (currentSettings is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(currentSettings.ProviderName))
        {
            SelectedProvider = currentSettings.ProviderName;
            if (!AvailableProviders.Contains(currentSettings.ProviderName))
            {
                AvailableProviders.Insert(0, currentSettings.ProviderName);
            }
        }

        ApiKey = currentSettings.ApiKey;
        BucketName = currentSettings.BucketName ?? string.Empty;
        EnableAutoSync = currentSettings.EnableAutoSync;
        EnableBackgroundFailureAlerts = currentSettings.EnableBackgroundFailureAlerts;
        EnableBackgroundConflictAlerts = currentSettings.EnableBackgroundConflictAlerts;
        AlertCooldownSeconds = ClampAlertCooldownSeconds(currentSettings.AlertCooldownSeconds);
    }

    /// <summary>
    /// Gets the list of available cloud providers.
    /// </summary>
    public ObservableCollection<string> AvailableProviders { get; }

    /// <summary>
    /// Command to save the configuration.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        Result = new Services.CloudProviderConfigResult(
            SelectedProvider,
            ApiKey,
            BucketName,
            EnableAutoSync,
            EnableBackgroundFailureAlerts,
            EnableBackgroundConflictAlerts,
            ClampAlertCooldownSeconds(AlertCooldownSeconds));
    }

    /// <summary>
    /// Command to cancel the dialog.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        Result = null;
    }

    /// <summary>
    /// Gets the result of the dialog.
    /// </summary>
    public Services.CloudProviderConfigResult? Result { get; private set; }

    private static int ClampAlertCooldownSeconds(int cooldownSeconds)
    {
        if (cooldownSeconds < MinAlertCooldownSeconds)
        {
            return MinAlertCooldownSeconds;
        }

        if (cooldownSeconds > MaxAlertCooldownSeconds)
        {
            return MaxAlertCooldownSeconds;
        }

        return cooldownSeconds;
    }
}
