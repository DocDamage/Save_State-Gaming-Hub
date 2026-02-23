using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Presentation.Models.Mobile;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Dialogs;

/// <summary>
/// ViewModel for the pairing confirmation dialog.
/// </summary>
public partial class PairingDialogViewModel : ObservableObject
{
    [ObservableProperty]
    private PairingRequest _request = new();

    [ObservableProperty]
    private ObservableCollection<PermissionItem> _permissions = new();

    [ObservableProperty]
    private string _deviceIcon = "📱";

    public PairingDialogViewModel()
    {
        InitializeDefaultPermissions();
    }

    public PairingDialogViewModel(PairingRequest request) : this()
    {
        Request = request;
        UpdateDeviceIcon();
    }

    /// <summary>
    /// Initializes default permissions for a new pairing request
    /// </summary>
    private void InitializeDefaultPermissions()
    {
        Permissions = new ObservableCollection<PermissionItem>
        {
            new PermissionItem
            {
                Id = "launch_games",
                Name = "Launch games",
                Description = "Start and launch games from your library",
                IsGranted = true
            },
            new PermissionItem
            {
                Id = "control_media",
                Name = "Control media",
                Description = "Play, pause, skip, and adjust volume",
                IsGranted = true
            },
            new PermissionItem
            {
                Id = "manage_save_states",
                Name = "Manage save states",
                Description = "Create, load, and delete save states",
                IsGranted = true
            },
            new PermissionItem
            {
                Id = "view_screenshots",
                Name = "View screenshots",
                Description = "Access and download screenshots",
                IsGranted = true
            },
            new PermissionItem
            {
                Id = "stream_gameplay",
                Name = "Stream gameplay",
                Description = "View live gameplay stream from your PC",
                IsGranted = false
            },
            new PermissionItem
            {
                Id = "system_control",
                Name = "System control",
                Description = "Shutdown, restart, or sleep the PC",
                IsGranted = false
            }
        };
    }

    /// <summary>
    /// Updates the device icon based on device type
    /// </summary>
    private void UpdateDeviceIcon()
    {
        DeviceIcon = Request.DeviceType.ToLowerInvariant() switch
        {
            var t when t.Contains("iphone") => "📱",
            var t when t.Contains("ipad") => "📱",
            var t when t.Contains("android") => "📱",
            var t when t.Contains("pixel") => "📱",
            var t when t.Contains("samsung") => "📱",
            _ => "📱"
        };
    }

    /// <summary>
    /// Allows the pairing request and grants selected permissions
    /// </summary>
    [RelayCommand]
    private void Allow()
    {
        // Update request with granted permissions
        Request.RequestedPermissions = Permissions
            .Where(p => p.IsGranted)
            .Select(p => p.Id)
            .ToList();

        // Close dialog with true result
        CloseDialog(true);
    }

    /// <summary>
    /// Declines the pairing request
    /// </summary>
    [RelayCommand]
    private void Decline()
    {
        // Close dialog with false result
        CloseDialog(false);
    }

    /// <summary>
    /// Closes the dialog with the specified result
    /// </summary>
    private void CloseDialog(bool result)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.Windows.FirstOrDefault(w => w.DataContext == this);
            window?.Close(result);
        }
    }
}

/// <summary>
/// Represents a permission that can be granted to a paired device
/// </summary>
public partial class PermissionItem : ObservableObject
{
    [ObservableProperty] private string _id = string.Empty;
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _description = string.Empty;
    [ObservableProperty] private bool _isGranted;
}
