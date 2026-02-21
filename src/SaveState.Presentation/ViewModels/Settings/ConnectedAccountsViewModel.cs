using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.Accounts;
using SaveState.Presentation.Services;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Settings;

/// <summary>
/// ViewModel for managing connected external accounts (gaming platforms, achievements, social).
/// </summary>
public partial class ConnectedAccountsViewModel : ObservableObject
{
    private readonly INotificationService _notificationService;

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

    // Discord Rich Presence settings
    [ObservableProperty]
    private bool _discordShowCurrentGame = true;

    [ObservableProperty]
    private bool _discordShowPlaytime = true;

    [ObservableProperty]
    private bool _discordAllowJoinRequests = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedAccountsViewModel"/> class.
    /// </summary>
    public ConnectedAccountsViewModel()
    {
        // Design-time constructor
        _notificationService = null!;
        InitializeSampleData();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectedAccountsViewModel"/> class.
    /// </summary>
    public ConnectedAccountsViewModel(INotificationService notificationService)
    {
        _notificationService = notificationService;
        InitializeSampleData();
    }

    private void InitializeSampleData()
    {
        GamingPlatforms = new ObservableCollection<AccountConnectionStatus>
        {
            new()
            {
                PlatformName = "Steam",
                Status = ConnectionStatus.Connected,
                Username = "SteamUser123",
                ConnectedSince = DateTime.Now.AddMonths(-6),
                LastSync = DateTime.Now.AddHours(-1),
                CanSync = true
            },
            new()
            {
                PlatformName = "GOG",
                Status = ConnectionStatus.Disconnected,
                CanSync = false
            },
            new()
            {
                PlatformName = "Epic Games",
                Status = ConnectionStatus.Connected,
                Username = "EpicGamer",
                ConnectedSince = DateTime.Now.AddMonths(-3),
                LastSync = DateTime.Now.AddHours(-2),
                CanSync = true
            },
            new()
            {
                PlatformName = "Origin",
                Status = ConnectionStatus.NotAvailable,
                ErrorMessage = "Deprecated - EA App migration required"
            }
        };

        AchievementServices = new ObservableCollection<AccountConnectionStatus>
        {
            new()
            {
                PlatformName = "RetroAchievements",
                Status = ConnectionStatus.Connected,
                Username = "RetroHero",
                ConnectedSince = DateTime.Now.AddMonths(-1),
                LastSync = DateTime.Now.AddMinutes(-30),
                CanSync = true
            }
        };

        SocialPlatforms = new ObservableCollection<AccountConnectionStatus>
        {
            new()
            {
                PlatformName = "Discord",
                Status = ConnectionStatus.Connected,
                ConnectedSince = DateTime.Now.AddDays(-1),
                CanSync = false
            }
        };
    }

    /// <summary>
    /// Initiates connection to the specified account platform.
    /// </summary>
    [RelayCommand]
    private async Task ConnectAccountAsync(AccountConnectionStatus? account)
    {
        if (account is null) return;

        IsConnecting = true;
        ConnectionStatusMessage = $"Connecting to {account.PlatformName}...";
        account.Status = ConnectionStatus.Connecting;

        try
        {
            // TODO: Implement OAuth flow
            await Task.Delay(2000);

            // Simulate successful connection
            account.Status = ConnectionStatus.Connected;
            account.Username = "NewUser";
            account.ConnectedSince = DateTime.Now;
            account.CanSync = true;

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
    /// Disconnects the specified account.
    /// </summary>
    [RelayCommand]
    private async Task DisconnectAccountAsync(AccountConnectionStatus? account)
    {
        if (account is null) return;

        try
        {
            account.Status = ConnectionStatus.Disconnected;
            account.Username = null;
            account.ConnectedSince = null;
            account.LastSync = null;
            account.CanSync = false;

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
        if (account is null || !account.CanSync) return;

        try
        {
            account.Status = ConnectionStatus.Connecting;

            // TODO: Call sync service
            await Task.Delay(1500);

            account.LastSync = DateTime.Now;
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
}
