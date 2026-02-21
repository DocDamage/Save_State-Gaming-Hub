using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for cloud provider configuration dialog.
/// </summary>
public partial class CloudProviderConfigDialogViewModel : ObservableObject
{
    private const int MinAlertCooldownSeconds = 15;
    private const int MaxAlertCooldownSeconds = 600;
    private const int MaxBucketNameLength = 63;
    private static readonly Regex ValidBucketNamePattern = new Regex(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$", RegexOptions.Compiled);

    [ObservableProperty]
    private string _selectedProvider = "GoogleDrive";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsApiKeyValid))]
    [NotifyPropertyChangedFor(nameof(HasValidationErrors))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsBucketNameValid))]
    [NotifyPropertyChangedFor(nameof(HasValidationErrors))]
    [NotifyPropertyChangedFor(nameof(ValidationError))]
    private string _bucketName = string.Empty;

    [ObservableProperty]
    private bool _enableAutoSync = true;

    [ObservableProperty]
    private bool _enableBackgroundFailureAlerts = true;

    [ObservableProperty]
    private bool _enableBackgroundConflictAlerts = true;

    [ObservableProperty]
    private int _alertCooldownSeconds = 60;

    [ObservableProperty]
    private string _validationError = string.Empty;

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
    /// Gets whether the API key is valid.
    /// </summary>
    public bool IsApiKeyValid => !string.IsNullOrWhiteSpace(ApiKey);

    /// <summary>
    /// Gets whether the bucket name is valid.
    /// </summary>
    public bool IsBucketNameValid => 
        string.IsNullOrEmpty(BucketName) || // Bucket name is optional for some providers
        (BucketName.Length <= MaxBucketNameLength &&
         ValidBucketNamePattern.IsMatch(BucketName));

    /// <summary>
    /// Gets whether there are any validation errors.
    /// </summary>
    public bool HasValidationErrors => !IsApiKeyValid || !IsBucketNameValid;

    /// <summary>
    /// Command to save the configuration.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (HasValidationErrors)
        {
            UpdateValidationError();
            return;
        }

        Result = new Services.CloudProviderConfigResult(
            SelectedProvider,
            ApiKey.Trim(),
            string.IsNullOrWhiteSpace(BucketName) ? null : BucketName.Trim(),
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

    partial void OnAlertCooldownSecondsChanged(int value)
    {
        // Auto-clamp to valid range
        if (value < MinAlertCooldownSeconds || value > MaxAlertCooldownSeconds)
        {
            AlertCooldownSeconds = ClampAlertCooldownSeconds(value);
        }
    }

    partial void OnApiKeyChanged(string value)
    {
        UpdateValidationError();
    }

    partial void OnBucketNameChanged(string value)
    {
        // Auto-lowercase bucket names (S3 requirement)
        if (value != value?.ToLowerInvariant())
        {
            BucketName = value?.ToLowerInvariant() ?? string.Empty;
            return;
        }
        UpdateValidationError();
    }

    private void UpdateValidationError()
    {
        if (!IsApiKeyValid)
        {
            ValidationError = "API Key is required.";
        }
        else if (!IsBucketNameValid)
        {
            if (BucketName.Length > MaxBucketNameLength)
                ValidationError = $"Bucket name must not exceed {MaxBucketNameLength} characters.";
            else
                ValidationError = "Bucket name must contain only lowercase letters, numbers, and hyphens, and must start and end with a letter or number.";
        }
        else
        {
            ValidationError = string.Empty;
        }
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
