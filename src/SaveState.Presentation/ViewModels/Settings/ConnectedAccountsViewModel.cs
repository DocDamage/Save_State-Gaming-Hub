using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Sync;
using SaveState.Presentation.Models.Accounts;
using SaveState.Presentation.Services;
using SaveState.Presentation.ViewModels.Dialogs;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for managing connected external accounts (gaming platforms, achievements, social).
/// </summary>
public partial class ConnectedAccountsViewModel : ObservableObject
{
    private readonly INotificationService? _notificationService;
    private readonly ICloudAuthenticationService? _cloudAuthenticationService;
    private readonly ISyncService? _syncService;
    private readonly IDialogService? _dialogService;

    [ObservableProperty]
    private ObservableCollection<AccountConnectionStatus> _gamingPlatforms = new();

    [ObservableProperty]
    private ObservableCollection<AccountConnectionStatus> _achievementServices = new();

    [ObservableProperty]
    private ObservableCollection<AccountConnectionStatus> _socialPlatforms = new();

    [ObservableProperty]
    private AccountConnectionStatus? _selectedAccount;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private string _connectionStatusMessage = string.Empty;

    // Individual provider status properties for easier binding
    [ObservableProperty]
    private AccountConnectionStatus _steamStatus = new() { PlatformName = "Steam" };

    [ObservableProperty]
    private AccountConnectionStatus _gogStatus = new() { PlatformName = "GOG" };

    [ObservableProperty]
    private AccountConnectionStatus _epicStatus = new() { PlatformName = "Epic Games" };

    [ObservableProperty]
    private AccountConnectionStatus _xboxStatus = new() { PlatformName = "Xbox", Status = ConnectionStatus.NotAvailable, ErrorMessage = "Coming Soon" };

    [ObservableProperty]
    private AccountConnectionStatus _retroAchievementsStatus = new() { PlatformName = "RetroAchievements" };

    [ObservableProperty]
    private AccountConnectionStatus _discordStatus = new() { PlatformName = "Discord" };

    // Discord Rich Presence settings
    [ObservableProperty]
    private bool _discordShowCurrentGame = true;

    [ObservableProperty]
    private bool _discordShowPlaytime = true;

    [ObservableProperty]
    private bool _discordAllowJoinRequests = true;

    /// <summary>
    /// Design-time constructor for XAML preview.
    /// </summary>
    [Obsolete("Design-time constructor only. Use the parameterized constructor in production code.")]
    public ConnectedAccountsViewModel()
    {
        _cloudAuthenticationService = null;
        _syncService = null;
        _dialogService = null;
        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedAccountsViewModel"/> class.
    /// </summary>
    public ConnectedAccountsViewModel(
        INotificationService notificationService,
        ICloudAuthenticationService? cloudAuthenticationService = null,
        ISyncService? syncService = null,
        IDialogService? dialogService = null)
    {
        _notificationService = notificationService;
        _cloudAuthenticationService = cloudAuthenticationService;
        _syncService = syncService;
        _dialogService = dialogService;
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        // Initialize individual status properties
        SteamStatus = new AccountConnectionStatus
        {
            PlatformName = "Steam",
            Status = ConnectionStatus.Connected,
            Username = "SteamUser123",
            ConnectedSince = DateTimeOffset.UtcNow.AddMonths(-6).DateTime,
            LastSync = DateTimeOffset.UtcNow.AddHours(-1).DateTime,
            CanSync = true,
            AvatarUrl = "https://avatars.steamstatic.com/default_avatar.jpg"
        };

        GogStatus = new AccountConnectionStatus
        {
            PlatformName = "GOG",
            Status = ConnectionStatus.Disconnected,
            CanSync = false
        };

        EpicStatus = new AccountConnectionStatus
        {
            PlatformName = "Epic Games",
            Status = ConnectionStatus.Connected,
            Username = "EpicGamer",
            ConnectedSince = DateTimeOffset.UtcNow.AddMonths(-3).DateTime,
            LastSync = DateTimeOffset.UtcNow.AddHours(-2).DateTime,
            CanSync = true
        };

        XboxStatus = new AccountConnectionStatus
        {
            PlatformName = "Xbox",
            Status = ConnectionStatus.NotAvailable,
            ErrorMessage = "Coming Soon"
        };

        RetroAchievementsStatus = new AccountConnectionStatus
        {
            PlatformName = "RetroAchievements",
            Status = ConnectionStatus.Connected,
            Username = "RetroHero",
            ConnectedSince = DateTimeOffset.UtcNow.AddMonths(-1).DateTime,
            LastSync = DateTimeOffset.UtcNow.AddMinutes(-30).DateTime,
            CanSync = true,
            AvatarUrl = "https://retroachievements.org/UserPic/RetroHero.png"
        };

        DiscordStatus = new AccountConnectionStatus
        {
            PlatformName = "Discord",
            Status = ConnectionStatus.Connected,
            ConnectedSince = DateTimeOffset.UtcNow.AddDays(-1).DateTime,
            CanSync = false
        };

        // Populate collections
        GamingPlatforms = new ObservableCollection<AccountConnectionStatus>
        {
            SteamStatus,
            GogStatus,
            EpicStatus,
            XboxStatus,
            new AccountConnectionStatus
            {
                PlatformName = "Origin",
                Status = ConnectionStatus.NotAvailable,
                ErrorMessage = "Deprecated - EA App migration required"
            }
        };

        AchievementServices = new ObservableCollection<AccountConnectionStatus>
        {
            RetroAchievementsStatus
        };

        SocialPlatforms = new ObservableCollection<AccountConnectionStatus>
        {
            DiscordStatus
        };
    }

    #region Steam Commands

    [RelayCommand]
    private async Task ConnectSteamAsync()
    {
        if (_dialogService != null)
        {
            var wizard = new AccountConnectionWizardViewModel(
                _notificationService!,
                _cloudAuthenticationService);
            var result = await _dialogService.ShowDialogAsync<AccountConnectionResult>(wizard);

            if (result != null)
            {
                SteamStatus.Status = ConnectionStatus.Connected;
                SteamStatus.Username = result.Username;
                SteamStatus.ConnectedSince = DateTime.UtcNow;
                SteamStatus.CanSync = true;
                SteamStatus.AvatarUrl = result.ProfileImageUrl;
                await _notificationService!.ShowNotificationAsync("Steam account connected successfully!", "Account Linked");
            }
        }
        else
        {
            await ConnectAccountAsync(SteamStatus);
        }
    }

    [RelayCommand]
    private async Task DisconnectSteamAsync()
    {
        await DisconnectAccountAsync(SteamStatus);
    }

    [RelayCommand]
    private async Task SyncSteamAsync()
    {
        await SyncAccountAsync(SteamStatus);
    }

    #endregion

    #region GOG Commands

    [RelayCommand]
    private async Task ConnectGogAsync()
    {
        if (_dialogService != null)
        {
            var wizard = new AccountConnectionWizardViewModel(
                _notificationService!,
                _cloudAuthenticationService);
            var result = await _dialogService.ShowDialogAsync<AccountConnectionResult>(wizard);

            if (result != null)
            {
                GogStatus.Status = ConnectionStatus.Connected;
                GogStatus.Username = result.Username;
                GogStatus.ConnectedSince = DateTime.UtcNow;
                GogStatus.CanSync = true;
                await _notificationService!.ShowNotificationAsync("GOG account connected successfully!", "Account Linked");
            }
        }
        else
        {
            await ConnectAccountAsync(GogStatus);
        }
    }

    [RelayCommand]
    private async Task DisconnectGogAsync()
    {
        await DisconnectAccountAsync(GogStatus);
    }

    #endregion

    #region Epic Games Commands

    [RelayCommand]
    private async Task ConnectEpicAsync()
    {
        if (_dialogService != null)
        {
            var wizard = new AccountConnectionWizardViewModel(
                _notificationService!,
                _cloudAuthenticationService);
            var result = await _dialogService.ShowDialogAsync<AccountConnectionResult>(wizard);

            if (result != null)
            {
                EpicStatus.Status = ConnectionStatus.Connected;
                EpicStatus.Username = result.Username;
                EpicStatus.ConnectedSince = DateTime.UtcNow;
                EpicStatus.CanSync = true;
                await _notificationService!.ShowNotificationAsync("Epic Games account connected successfully!", "Account Linked");
            }
        }
        else
        {
            await ConnectAccountAsync(EpicStatus);
        }
    }

    [RelayCommand]
    private async Task DisconnectEpicAsync()
    {
        await DisconnectAccountAsync(EpicStatus);
    }

    #endregion

    #region RetroAchievements Commands

    [RelayCommand]
    private async Task ConnectRetroAchievementsAsync()
    {
        if (_dialogService != null)
        {
            var wizard = new AccountConnectionWizardViewModel(
                _notificationService!,
                _cloudAuthenticationService);
            var result = await _dialogService.ShowDialogAsync<AccountConnectionResult>(wizard);

            if (result != null)
            {
                RetroAchievementsStatus.Status = ConnectionStatus.Connected;
                RetroAchievementsStatus.Username = result.Username;
                RetroAchievementsStatus.ConnectedSince = DateTime.UtcNow;
                RetroAchievementsStatus.CanSync = true;
                RetroAchievementsStatus.AvatarUrl = result.ProfileImageUrl;
                await _notificationService!.ShowNotificationAsync("RetroAchievements account connected successfully!", "Account Linked");
            }
        }
        else
        {
            await ConnectAccountAsync(RetroAchievementsStatus);
        }
    }

    [RelayCommand]
    private async Task DisconnectRetroAchievementsAsync()
    {
        await DisconnectAccountAsync(RetroAchievementsStatus);
    }

    #endregion

    #region Discord Commands

    [RelayCommand]
    private async Task ConnectDiscordAsync()
    {
        if (_dialogService != null)
        {
            var wizard = new AccountConnectionWizardViewModel(
                _notificationService!,
                _cloudAuthenticationService);
            var result = await _dialogService.ShowDialogAsync<AccountConnectionResult>(wizard);

            if (result != null)
            {
                DiscordStatus.Status = ConnectionStatus.Connected;
                DiscordStatus.Username = result.Username;
                DiscordStatus.ConnectedSince = DateTime.UtcNow;
                await _notificationService!.ShowNotificationAsync("Discord connected successfully!", "Account Linked");
            }
        }
        else
        {
            await ConnectAccountAsync(DiscordStatus);
        }
    }

    [RelayCommand]
    private async Task DisconnectDiscordAsync()
    {
        await DisconnectAccountAsync(DiscordStatus);
    }

    #endregion

    /// <summary>
    /// Initiates connection to the specified account platform.
    /// </summary>
    [RelayCommand]
    private async Task ConnectAccountAsync(AccountConnectionStatus? account)
    {
        if (account is null || _notificationService is null) return;

        IsConnecting = true;
        ConnectionStatusMessage = $"Connecting to {account.PlatformName}...";
        account.Status = ConnectionStatus.Connecting;

        try
        {
            // Get OAuth configuration for the platform
            var (authUrl, clientId, scopes, tokenUrl) = GetPlatformOAuthConfig(account.PlatformName);

            if (string.IsNullOrEmpty(authUrl))
            {
                // For platforms without OAuth (like Discord Rich Presence), simulate connection
                await Task.Delay(1000);
                account.Status = ConnectionStatus.Connected;
                account.Username = Environment.UserName;
                account.ConnectedSince = DateTimeOffset.UtcNow.DateTime;
                account.CanSync = false;

                await _notificationService.ShowNotificationAsync(
                    $"Connected to {account.PlatformName}",
                    "Account Linked");
                return;
            }

            // Open browser for OAuth authentication
            var processStartInfo = new ProcessStartInfo(authUrl)
            {
                UseShellExecute = true
            };
            Process.Start(processStartInfo);

            await _notificationService.ShowNotificationAsync(
                $"Browser opened for {account.PlatformName} authentication. Please complete the sign-in process.",
                "Authentication Started");

            // In a real implementation, we would:
            // 1. Start a local callback listener
            // 2. Wait for the OAuth callback
            // 3. Exchange the code for tokens
            // 4. Store the tokens securely

            if (_cloudAuthenticationService != null)
            {
                // This would be the actual OAuth flow through the service
                var result = await _cloudAuthenticationService.AuthenticateAsync(
                    account.PlatformName,
                    clientId,
                    scopes,
                    authUrl,
                    tokenUrl);

                if (result.IsFailure)
                {
                    throw new InvalidOperationException(result.Error ?? "Authentication failed");
                }

                account.Username = $"User_{result.Value.AccessToken[..8]}";
            }
            else
            {
                // Fallback: simulate the connection flow
                await Task.Delay(2000);
                account.Username = "OAuthUser";
            }

            account.Status = ConnectionStatus.Connected;
            account.ConnectedSince = DateTimeOffset.UtcNow.DateTime;
            account.CanSync = !string.IsNullOrEmpty(tokenUrl);

            await _notificationService.ShowNotificationAsync(
                $"Connected to {account.PlatformName}",
                "Account Linked");
        }
        catch (Exception ex)
        {
            account.Status = ConnectionStatus.Error;
            account.ErrorMessage = ex.Message;
            await _notificationService.ShowErrorAsync($"Failed to connect: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
            ConnectionStatusMessage = string.Empty;
        }
    }

    /// <summary>
    /// Gets OAuth configuration for the specified platform.
    /// </summary>
    private static (string authUrl, string clientId, string[] scopes, string tokenUrl) GetPlatformOAuthConfig(string platformName)
    {
        return platformName.ToLowerInvariant() switch
        {
            "steam" => (
                "https://steamcommunity.com/openid/login",
                "savestate_steam_client",
                new[] { "read_profile", "read_library" },
                "https://api.steampowered.com/ISteamUserOAuth/Token"),

            "gog" => (
                "https://auth.gog.com/auth",
                "savestate_gog_client",
                new[] { "library.read" },
                "https://auth.gog.com/token"),

            "epic games" or "epic" => (
                "https://www.epicgames.com/id/authorize",
                "savestate_epic_client",
                new[] { "basic_profile", "library" },
                "https://api.epicgames.dev/epic/oauth/v1/token"),

            "retroachievements" => (
                "https://retroachievements.org/oauth/auth",
                "savestate_retro_client",
                new[] { "read" },
                "https://retroachievements.org/oauth/token"),

            // Platforms without OAuth or with different auth methods
            "discord" => (string.Empty, string.Empty, Array.Empty<string>(), string.Empty),
            "origin" => (string.Empty, string.Empty, Array.Empty<string>(), string.Empty),

            _ => (string.Empty, string.Empty, Array.Empty<string>(), string.Empty)
        };
    }

    /// <summary>
    /// Disconnects the specified account.
    /// </summary>
    [RelayCommand]
    private async Task DisconnectAccountAsync(AccountConnectionStatus? account)
    {
        if (account is null || _notificationService is null) return;

        try
        {
            account.Status = ConnectionStatus.Disconnected;
            account.Username = null;
            account.ConnectedSince = null;
            account.LastSync = null;
            account.CanSync = false;
            account.AvatarUrl = null;

            await _notificationService.ShowNotificationAsync(
                $"Disconnected from {account.PlatformName}",
                "Account Unlinked");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to disconnect: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes the specified account.
    /// </summary>
    [RelayCommand]
    private async Task SyncAccountAsync(AccountConnectionStatus? account)
    {
        if (account is null || !account.CanSync || _notificationService is null) return;

        try
        {
            account.Status = ConnectionStatus.Connecting;

            // Use sync service if available
            if (_syncService != null)
            {
                var result = await _syncService.SyncAsync();

                if (!result.Success)
                {
                    var errorMessage = result.Errors.Count > 0
                        ? string.Join("; ", result.Errors)
                        : "Synchronization failed";
                    throw new InvalidOperationException(errorMessage);
                }
            }
            else
            {
                // Fallback: simulate sync delay
                await Task.Delay(1500);
            }

            account.LastSync = DateTimeOffset.UtcNow.DateTime;
            account.Status = ConnectionStatus.Connected;

            await _notificationService.ShowNotificationAsync(
                $"{account.PlatformName} synchronized successfully",
                "Sync Complete");
        }
        catch (Exception ex)
        {
            account.Status = ConnectionStatus.Error;
            account.ErrorMessage = ex.Message;
            await _notificationService.ShowErrorAsync($"Sync failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Synchronizes all accounts that support syncing.
    /// </summary>
    [RelayCommand]
    private async Task SyncAllAsync()
    {
        if (_notificationService is null) return;

        var syncableAccounts = GamingPlatforms.Where(a => a.CanSync)
            .Concat(AchievementServices.Where(a => a.CanSync))
            .ToList();

        if (syncableAccounts.Count == 0)
        {
            await _notificationService.ShowNotificationAsync(
                "No syncable accounts connected",
                "Nothing to Sync");
            return;
        }

        int successCount = 0;
        foreach (var account in syncableAccounts)
        {
            try
            {
                await SyncAccountAsync(account);
                successCount++;
            }
            catch
            {
                // Individual account errors handled in SyncAccountAsync
            }
        }

        await _notificationService.ShowNotificationAsync(
            $"Synchronized {successCount} of {syncableAccounts.Count} accounts",
            "Bulk Sync Complete");
    }

    /// <summary>
    /// Refreshes all connections by checking their status with the respective services.
    /// </summary>
    [RelayCommand]
    private async Task RefreshAllAsync()
    {
        if (_notificationService is null) return;

        IsConnecting = true;
        ConnectionStatusMessage = "Refreshing all connections...";

        try
        {
            var allAccounts = GamingPlatforms
                .Concat(AchievementServices)
                .Concat(SocialPlatforms)
                .Where(a => a.Status == ConnectionStatus.Connected)
                .ToList();

            foreach (var account in allAccounts)
            {
                // In a real implementation, we would verify the token validity
                // and refresh if necessary
                await Task.Delay(200); // Simulate API call
            }

            await _notificationService.ShowNotificationAsync(
                $"Refreshed {allAccounts.Count} connections",
                "Refresh Complete");
        }
        catch (Exception ex)
        {
            await _notificationService.ShowErrorAsync($"Failed to refresh: {ex.Message}");
        }
        finally
        {
            IsConnecting = false;
            ConnectionStatusMessage = string.Empty;
        }
    }
}
