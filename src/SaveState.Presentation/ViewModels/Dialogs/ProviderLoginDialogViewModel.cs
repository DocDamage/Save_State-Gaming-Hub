using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.CloudGaming;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the Provider Login Dialog.
/// </summary>
public partial class ProviderLoginDialogViewModel : ObservableObject
{
    private readonly ILogger<ProviderLoginDialogViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the ProviderLoginDialogViewModel.
    /// </summary>
    public ProviderLoginDialogViewModel(ILogger<ProviderLoginDialogViewModel> logger)
    {
        _logger = logger;
    }

    #region Observable Properties

    /// <summary>
    /// Provider being connected.
    /// </summary>
    [ObservableProperty]
    private CloudProvider _provider;

    /// <summary>
    /// Username or email.
    /// </summary>
    [ObservableProperty]
    private string _username = string.Empty;

    /// <summary>
    /// Password (if not using OAuth).
    /// </summary>
    [ObservableProperty]
    private string _password = string.Empty;

    /// <summary>
    /// Whether OAuth flow should be used.
    /// </summary>
    [ObservableProperty]
    private bool _useOAuth = true;

    /// <summary>
    /// Whether login is in progress.
    /// </summary>
    [ObservableProperty]
    private bool _isLoggingIn;

    /// <summary>
    /// Error message to display.
    /// </summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>
    /// Whether to remember credentials.
    /// </summary>
    [ObservableProperty]
    private bool _rememberMe;

    /// <summary>
    /// Provider display name.
    /// </summary>
    public string ProviderDisplayName => Provider switch
    {
        CloudProvider.GeForceNow => "NVIDIA GeForce Now",
        CloudProvider.XboxCloudGaming => "Xbox Cloud Gaming",
        CloudProvider.AmazonLuna => "Amazon Luna",
        CloudProvider.Boosteroid => "Boosteroid",
        CloudProvider.ShadowPC => "Shadow PC",
        CloudProvider.Parsec => "Parsec",
        CloudProvider.Moonlight => "Moonlight",
        _ => Provider.ToString()
    };

    /// <summary>
    /// Provider icon/emoji.
    /// </summary>
    public string ProviderIcon => Provider switch
    {
        CloudProvider.GeForceNow => "🟢",
        CloudProvider.XboxCloudGaming => "🔵",
        CloudProvider.AmazonLuna => "🟣",
        CloudProvider.Boosteroid => "🟠",
        CloudProvider.ShadowPC => "⚫",
        CloudProvider.Parsec => "🟡",
        CloudProvider.Moonlight => "🌙",
        _ => "☁️"
    };

    /// <summary>
    /// Description of the login method.
    /// </summary>
    public string LoginDescription => UseOAuth
        ? $"You'll be redirected to {ProviderDisplayName} to authorize access. Your credentials are never stored locally."
        : "Enter your account credentials to connect. This method stores a secure token locally.";

    /// <summary>
    /// Whether form input is valid.
    /// </summary>
    public bool IsValid => UseOAuth || (!string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password));

    #endregion

    #region Commands

    /// <summary>
    /// Initiates the login process.
    /// </summary>
    [RelayCommand]
    private async Task LoginAsync()
    {
        if (!IsValid) return;

        IsLoggingIn = true;
        ErrorMessage = null;

        _logger.LogInformation("Initiating login for {Provider}", Provider);

        try
        {
            if (UseOAuth)
            {
                await PerformOAuthFlowAsync();
            }
            else
            {
                await PerformCredentialLoginAsync();
            }

            _logger.LogInformation("Login successful for {Provider}", Provider);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Login failed: {ex.Message}";
            _logger.LogError(ex, "Login failed for {Provider}", Provider);
        }
        finally
        {
            IsLoggingIn = false;
        }
    }

    /// <summary>
    /// Cancels the login process.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        _logger.LogInformation("Login cancelled for {Provider}", Provider);
        // TODO: Close dialog with cancellation result
    }

    /// <summary>
    /// Opens the provider's website for account creation.
    /// </summary>
    [RelayCommand]
    private async Task CreateAccountAsync()
    {
        var url = Provider switch
        {
            CloudProvider.GeForceNow => "https://www.nvidia.com/geforce-now/",
            CloudProvider.XboxCloudGaming => "https://www.xbox.com/game-pass",
            CloudProvider.AmazonLuna => "https://www.amazon.com/luna",
            CloudProvider.Boosteroid => "https://boosteroid.com/",
            CloudProvider.ShadowPC => "https://shadow.tech/",
            CloudProvider.Parsec => "https://parsec.app/",
            CloudProvider.Moonlight => "https://moonlight-stream.org/",
            _ => ""
        };

        if (!string.IsNullOrEmpty(url))
        {
            _logger.LogInformation("Opening account creation page for {Provider}", Provider);
            // TODO: Open URL in browser
            await Task.Delay(100);
        }
    }

    /// <summary>
    /// Toggles between OAuth and credentials login.
    /// </summary>
    [RelayCommand]
    private void ToggleLoginMethod()
    {
        UseOAuth = !UseOAuth;
        OnPropertyChanged(nameof(LoginDescription));
        OnPropertyChanged(nameof(IsValid));
    }

    #endregion

    private async Task PerformOAuthFlowAsync()
    {
        // TODO: Open browser for OAuth authorization
        // TODO: Start local callback listener
        // TODO: Exchange code for tokens
        // TODO: Store secure token

        await Task.Delay(2000); // Simulate OAuth flow
    }

    private async Task PerformCredentialLoginAsync()
    {
        // TODO: Validate credentials with provider API
        // TODO: Store secure token

        await Task.Delay(1500); // Simulate API call
    }

    partial void OnUsernameChanged(string value) => OnPropertyChanged(nameof(IsValid));
    partial void OnPasswordChanged(string value) => OnPropertyChanged(nameof(IsValid));
    partial void OnUseOAuthChanged(bool value) => OnPropertyChanged(nameof(IsValid));
}
