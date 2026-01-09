using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// View model for cloud provider configuration dialog.
/// </summary>
public partial class CloudProviderConfigDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private string _selectedProvider = "GoogleDrive";

    [ObservableProperty]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _bucketName = string.Empty;

    [ObservableProperty]
    private bool _enableAutoSync = true;

    public CloudProviderConfigDialogViewModel(string? currentProvider = null)
    {
        if (!string.IsNullOrEmpty(currentProvider))
        {
            SelectedProvider = currentProvider;
        }

        AvailableProviders = new ObservableCollection<string>
        {
            "GoogleDrive",
            "OneDrive",
            "Dropbox",
            "AWS S3",
            "Azure Blob Storage"
        };
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
            EnableAutoSync);
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
}
