using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Shell;

public partial class ProviderConfigurationDialogViewModel : ObservableObject
{
    private readonly IOverlayService _overlayService;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<CloudProviderViewModel> _providers = new();

    [ObservableProperty]
    private CloudProviderViewModel? _selectedProvider;

    [ObservableProperty]
    private string _clientId = string.Empty;

    [ObservableProperty]
    private string _clientSecret = string.Empty;

    [ObservableProperty]
    private string _bucketName = string.Empty;

    [ObservableProperty]
    private string _region = "us-east-1";

    [ObservableProperty]
    private bool _autoSync = true;

    [ObservableProperty]
    private string _syncInterval = "Every 4 Hours";

    public ProviderConfigurationDialogViewModel(
        IOverlayService overlayService,
        INotificationService notificationService)
    {
        _overlayService = overlayService;
        _notificationService = notificationService;
        LoadProviders();
    }

    private void LoadProviders()
    {
        Providers.Clear();
        Providers.Add(new CloudProviderViewModel("Google Drive", "☁️", false));
        Providers.Add(new CloudProviderViewModel("OneDrive", "☁️", false));
        Providers.Add(new CloudProviderViewModel("Dropbox", "☁️", false));
        Providers.Add(new CloudProviderViewModel("Amazon S3", "☁️", false));
        Providers.Add(new CloudProviderViewModel("Custom WebDAV", "🌐", false));

        SelectedProvider = Providers[0];
    }

    [RelayCommand]
    private void TestConnection()
    {
        _notificationService.ShowInfo("Testing connection...", "Cloud Provider");
        // Simulate connection test
        Task.Delay(1000).ContinueWith(_ =>
        {
            _notificationService.ShowSuccess("Connection successful!", "Cloud Provider");
        });
    }

    [RelayCommand]
    private void Save()
    {
        if (string.IsNullOrWhiteSpace(ClientId) || string.IsNullOrWhiteSpace(ClientSecret))
        {
            _notificationService.ShowWarning("Please fill in all required fields", "Configuration");
            return;
        }

        _notificationService.ShowSuccess($"{SelectedProvider?.Name} configured successfully", "Cloud Provider");
        Close();
    }

    [RelayCommand]
    private void Close()
    {
        _overlayService.HideProviderConfigurationDialog();
    }
}

public partial class CloudProviderViewModel : ObservableObject
{
    public CloudProviderViewModel(string name, string icon, bool isConfigured)
    {
        Name = name;
        Icon = icon;
        IsConfigured = isConfigured;
    }

    public string Name { get; }
    public string Icon { get; }

    [ObservableProperty]
    private bool isConfigured;
}
