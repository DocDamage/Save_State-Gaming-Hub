using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Common;
using SaveState.Core.Sync;
using SaveState.Presentation.Models.Accounts;
using SaveState.Presentation.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// Represents the result of testing a connection to an external service.
/// </summary>
public class ConnectionTestResult
{
    /// <summary>Whether the connection test was successful.</summary>
    public bool Success { get; set; }

    /// <summary>The username returned from the service (if successful).</summary>
    public string? Username { get; set; }

    /// <summary>Error message if the test failed.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Time taken to complete the test.</summary>
    public TimeSpan ResponseTime { get; set; }

    /// <summary>Profile image URL from the service.</summary>
    public string? ProfileImageUrl { get; set; }
}

/// <summary>
/// ViewModel for the account connection wizard dialog.
/// Guides users through connecting external gaming accounts step by step.
/// </summary>
public partial class AccountConnectionWizardViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;
    private readonly ICloudAuthenticationService? _cloudAuthenticationService;

    // Wizard step constants
    public const int StepSelectProvider = 1;
    public const int StepAuthentication = 2;
    public const int StepPermissions = 3;
    public const int StepTestConnection = 4;
    public const int StepComplete = 5;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StepSelectProviderVisible))]
    [NotifyPropertyChangedFor(nameof(StepAuthenticationVisible))]
    [NotifyPropertyChangedFor(nameof(StepPermissionsVisible))]
    [NotifyPropertyChangedFor(nameof(StepTestConnectionVisible))]
    [NotifyPropertyChangedFor(nameof(StepCompleteVisible))]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    [NotifyPropertyChangedFor(nameof(CanGoBack))]
    [NotifyPropertyChangedFor(nameof(NextButtonText))]
    [NotifyPropertyChangedFor(nameof(ShowBackButton))]
    private int _currentStep = StepSelectProvider;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private string _selectedProvider = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private string _apiKey = string.Empty;

    [ObservableProperty]
    private string _oauthToken = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private ConnectionTestResult? _testResult;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanGoNext))]
    private bool _connectionTested;

    // Permission flags
    [ObservableProperty]
    private bool _allowLibraryAccess = true;

    [ObservableProperty]
    private bool _allowFriendsAccess;

    [ObservableProperty]
    private bool _allowAchievementsAccess = true;

    [ObservableProperty]
    private bool _allowPlaytimeAccess = true;

    // Provider display info
    public string SelectedProviderDisplayName => SelectedProvider switch
    {
        "steam" => "Steam",
        "gog" => "GOG",
        "epic" => "Epic Games",
        "retroachievements" => "RetroAchievements",
        "discord" => "Discord",
        "xbox" => "Xbox",
        _ => SelectedProvider
    };

    public string SelectedProviderIcon => SelectedProvider.ToLowerInvariant() switch
    {
        "steam" => "🎮",
        "gog" => "🎲",
        "epic" => "🎯",
        "retroachievements" => "🏆",
        "discord" => "💬",
        "xbox" => "🎮",
        _ => "🔗"
    };

    public bool StepSelectProviderVisible => CurrentStep == StepSelectProvider;
    public bool StepAuthenticationVisible => CurrentStep == StepAuthentication;
    public bool StepPermissionsVisible => CurrentStep == StepPermissions;
    public bool StepTestConnectionVisible => CurrentStep == StepTestConnection;
    public bool StepCompleteVisible => CurrentStep == StepComplete;

    public bool CanGoBack => CurrentStep > StepSelectProvider && CurrentStep < StepComplete;
    public bool ShowBackButton => CurrentStep > StepSelectProvider && CurrentStep <= StepComplete;

    public string NextButtonText => CurrentStep switch
    {
        StepSelectProvider => "Continue",
        StepAuthentication => UsesApiKey ? "Continue" : "Open Browser",
        StepPermissions => "Continue",
        StepTestConnection => "Complete",
        StepComplete => "Finish",
        _ => "Next"
    };

    public bool CanGoNext => CurrentStep switch
    {
        StepSelectProvider => !string.IsNullOrEmpty(SelectedProvider),
        StepAuthentication => UsesApiKey ? !string.IsNullOrEmpty(ApiKey) : true,
        StepPermissions => true,
        StepTestConnection => ConnectionTested && TestResult?.Success == true,
        StepComplete => true,
        _ => false
    };

    public bool UsesApiKey => SelectedProvider.ToLowerInvariant() switch
    {
        "retroachievements" => true,
        _ => false
    };

    public bool UsesOAuth => !UsesApiKey;

    public string[] AvailableProviders { get; } = new[]
    {
        "steam",
        "gog",
        "epic",
        "retroachievements",
        "discord"
    };

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public AccountConnectionWizardViewModel()
    {
        _notificationService = null!;
        _cloudAuthenticationService = null;
    }

    public AccountConnectionWizardViewModel(
        INotificationService notificationService,
        ICloudAuthenticationService? cloudAuthenticationService = null)
    {
        _notificationService = notificationService;
        _cloudAuthenticationService = cloudAuthenticationService;
    }

    [RelayCommand]
    private void SelectProvider(string provider)
    {
        SelectedProvider = provider;
        // Auto-advance for API key providers, stay for OAuth
        if (UsesApiKey)
        {
            CurrentStep = StepAuthentication;
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private async Task NextStepAsync()
    {
        if (CurrentStep == StepAuthentication && UsesOAuth)
        {
            await LaunchOAuthFlowAsync();
            CurrentStep = StepPermissions;
        }
        else if (CurrentStep < StepComplete)
        {
            CurrentStep++;

            // Auto-trigger connection test when reaching that step
            if (CurrentStep == StepTestConnection && !ConnectionTested)
            {
                await TestConnectionAsync();
            }
        }
        else if (CurrentStep == StepComplete)
        {
            CompleteWizard();
        }
    }

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void PreviousStep()
    {
        if (CurrentStep > StepSelectProvider)
        {
            CurrentStep--;
        }
    }

    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        IsLoading = true;
        StatusMessage = "Testing connection...";
        TestResult = null;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Simulate connection test based on provider
            await Task.Delay(1500); // Simulate network delay

            TestResult = SelectedProvider.ToLowerInvariant() switch
            {
                "steam" => await TestSteamConnectionAsync(),
                "gog" => await TestGogConnectionAsync(),
                "epic" => await TestEpicConnectionAsync(),
                "retroachievements" => await TestRetroAchievementsConnectionAsync(),
                "discord" => await TestDiscordConnectionAsync(),
                _ => new ConnectionTestResult
                {
                    Success = false,
                    ErrorMessage = "Unknown provider"
                }
            };

            stopwatch.Stop();
            TestResult.ResponseTime = stopwatch.Elapsed;

            ConnectionTested = true;

            if (TestResult.Success)
            {
                StatusMessage = $"Connected successfully as {TestResult.Username}";
                _notificationService.ShowSuccess(
                    $"Successfully connected to {SelectedProviderDisplayName}",
                    "Connection Test");
            }
            else
            {
                StatusMessage = $"Connection failed: {TestResult.ErrorMessage}";
                _notificationService.ShowError(
                    TestResult.ErrorMessage ?? "Connection test failed",
                    "Connection Failed");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            TestResult = new ConnectionTestResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                ResponseTime = stopwatch.Elapsed
            };
            ConnectionTested = true;
            StatusMessage = $"Error: {ex.Message}";
            _notificationService.ShowError($"Connection test error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void CompleteWizard()
    {
        if (TestResult?.Success != true)
        {
            _notificationService.ShowError("Please complete the connection test first.");
            return;
        }

        var result = new AccountConnectionResult(
            SelectedProvider,
            SelectedProviderDisplayName,
            TestResult.Username ?? "Unknown",
            TestResult.ProfileImageUrl,
            AllowLibraryAccess,
            AllowFriendsAccess,
            AllowAchievementsAccess,
            AllowPlaytimeAccess);

        CloseDialog(result);
    }

    [RelayCommand]
    private void Cancel()
    {
        CloseDialog(null);
    }

    private async Task LaunchOAuthFlowAsync()
    {
        var authUrl = SelectedProvider.ToLowerInvariant() switch
        {
            "steam" => GetSteamAuthUrl(),
            "gog" => GetGogAuthUrl(),
            "epic" => GetEpicAuthUrl(),
            "discord" => GetDiscordAuthUrl(),
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(authUrl))
        {
            var processStartInfo = new ProcessStartInfo(authUrl)
            {
                UseShellExecute = true
            };
            Process.Start(processStartInfo);

            await _notificationService.ShowNotificationAsync(
                $"Browser opened for {SelectedProviderDisplayName} authentication. Please complete the sign-in process.",
                "Authentication Started");

            // In a real implementation, we would:
            // 1. Start a local callback listener on a specific port
            // 2. Wait for the OAuth callback with the authorization code
            // 3. Exchange the code for access/refresh tokens
            // 4. Store the tokens securely in the keychain/credential manager
        }
    }

    private static string GetSteamAuthUrl()
    {
        var callbackUrl = "http://localhost:5000/auth/callback";
        return $"https://steamcommunity.com/openid/login?" +
               $"openid.mode=checkid_setup&" +
               $"openid.return_to={Uri.EscapeDataString(callbackUrl)}&" +
               $"openid.realm={Uri.EscapeDataString(callbackUrl)}&" +
               $"openid.ns=http://specs.openid.net/auth/2.0&" +
               $"openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select&" +
               $"openid.identity=http://specs.openid.net/auth/2.0/identifier_select";
    }

    private static string GetGogAuthUrl()
    {
        var clientId = "savestate_gog_client";
        var redirectUri = Uri.EscapeDataString("savestate://auth/callback");
        var scope = Uri.EscapeDataString("library.read friends.read");
        return $"https://auth.gog.com/auth?client_id={clientId}&" +
               $"redirect_uri={redirectUri}&" +
               $"response_type=code&" +
               $"scope={scope}&" +
               $"layout=client";
    }

    private static string GetEpicAuthUrl()
    {
        var clientId = "savestate_epic_client";
        var redirectUri = Uri.EscapeDataString("https://localhost:5000/auth/epic/callback");
        var scope = Uri.EscapeDataString("basic_profile friends_list presence");
        return $"https://www.epicgames.com/id/authorize?client_id={clientId}&" +
               $"redirect_uri={redirectUri}&" +
               $"response_type=code&" +
               $"scope={scope}";
    }

    private static string GetDiscordAuthUrl()
    {
        var clientId = "savestate_discord_client";
        var redirectUri = Uri.EscapeDataString("savestate://auth/discord/callback");
        var scope = Uri.EscapeDataString("identify rpc rpc.activities.write");
        return $"https://discord.com/api/oauth2/authorize?client_id={clientId}&" +
               $"redirect_uri={redirectUri}&" +
               $"response_type=code&" +
               $"scope={scope}";
    }

    private Task<ConnectionTestResult> TestSteamConnectionAsync()
    {
        // In a real implementation, this would:
        // 1. Call Steam Web API with the obtained access token
        // 2. Validate the token and get user info
        // 3. Return success/failure with user details

        return Task.FromResult(new ConnectionTestResult
        {
            Success = true,
            Username = "SteamUser_" + Random.Shared.Next(1000, 9999),
            ProfileImageUrl = "https://avatars.steamstatic.com/default_avatar.jpg"
        });
    }

    private Task<ConnectionTestResult> TestGogConnectionAsync()
    {
        return Task.FromResult(new ConnectionTestResult
        {
            Success = true,
            Username = "GOGUser_" + Random.Shared.Next(1000, 9999),
            ProfileImageUrl = null
        });
    }

    private Task<ConnectionTestResult> TestEpicConnectionAsync()
    {
        return Task.FromResult(new ConnectionTestResult
        {
            Success = true,
            Username = "EpicUser_" + Random.Shared.Next(1000, 9999),
            ProfileImageUrl = null
        });
    }

    private Task<ConnectionTestResult> TestRetroAchievementsConnectionAsync()
    {
        // For RetroAchievements, we validate the API key
        if (string.IsNullOrEmpty(ApiKey) || ApiKey.Length < 10)
        {
            return Task.FromResult(new ConnectionTestResult
            {
                Success = false,
                ErrorMessage = "Invalid API key format. Please check your RetroAchievements API key."
            });
        }

        return Task.FromResult(new ConnectionTestResult
        {
            Success = true,
            Username = "RAUser_" + ApiKey[..4],
            ProfileImageUrl = "https://retroachievements.org/UserPic/" + ApiKey[..4] + ".png"
        });
    }

    private Task<ConnectionTestResult> TestDiscordConnectionAsync()
    {
        return Task.FromResult(new ConnectionTestResult
        {
            Success = true,
            Username = "DiscordUser_" + Random.Shared.Next(1000, 9999),
            ProfileImageUrl = null
        });
    }

    private void CloseDialog(AccountConnectionResult? result)
    {
        var lifetime = Avalonia.Application.Current?.ApplicationLifetime;
        if (lifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}

/// <summary>
/// Result of a completed account connection wizard.
/// </summary>
public sealed record AccountConnectionResult(
    string ProviderId,
    string ProviderName,
    string Username,
    string? ProfileImageUrl,
    bool AllowLibraryAccess,
    bool AllowFriendsAccess,
    bool AllowAchievementsAccess,
    bool AllowPlaytimeAccess);
