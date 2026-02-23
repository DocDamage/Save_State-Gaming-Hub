using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.RgbSync;

/// <summary>
/// View model for creating and managing RGB sync groups.
/// </summary>
public partial class RgbSyncGroupEditorViewModel : ObservableObject
{
    private readonly IRgbSyncService _rgbService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RgbSyncGroupEditorViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private ObservableCollection<RgbSyncGroup> _syncGroups = new();

    [ObservableProperty]
    private RgbSyncGroup? _selectedGroup;

    [ObservableProperty]
    private ObservableCollection<RgbDevice> _availableDevices = new();

    [ObservableProperty]
    private ObservableCollection<RgbDevice> _selectedDevices = new();

    [ObservableProperty]
    private string _groupName = string.Empty;

    [ObservableProperty]
    private RgbEffect _sharedEffect = new() { Name = "Group Effect" };

    [ObservableProperty]
    private bool _isCreatingNew;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private RgbDevice? _draggedDevice;

    public RgbSyncGroupEditorViewModel(
        IRgbSyncService rgbService,
        IDialogService dialogService,
        ILogger<RgbSyncGroupEditorViewModel> logger,
        ITimeProvider timeProvider)
    {
        _rgbService = rgbService;
        _dialogService = dialogService;
        _logger = logger;
        _timeProvider = timeProvider;

        LoadDevices();
        LoadGroups();
    }

    private async void LoadDevices()
    {
        try
        {
            var result = await _rgbService.GetDevicesAsync();
            if (result.IsSuccess)
            {
                AvailableDevices.Clear();
                foreach (var device in result.Value)
                {
                    if (device.IsConnected)
                    {
                        AvailableDevices.Add(device);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading devices");
        }
    }

    private void LoadGroups()
    {
        // In a real implementation, load from storage
        // For now, create some examples
        if (SyncGroups.Count == 0)
        {
            SyncGroups.Add(new RgbSyncGroup
            {
                Id = Guid.NewGuid(),
                Name = "Peripherals",
                DeviceIds = new List<Guid>(),
                SharedEffect = new RgbEffect { Type = RgbEffectType.Rainbow }
            });

            SyncGroups.Add(new RgbSyncGroup
            {
                Id = Guid.NewGuid(),
                Name = "Case Lighting",
                DeviceIds = new List<Guid>(),
                SharedEffect = new RgbEffect { Type = RgbEffectType.Breathing, Colors = new List<RgbColor> { RgbColor.Blue } }
            });
        }
    }

    [RelayCommand]
    private void CreateNewGroup()
    {
        IsCreatingNew = true;
        GroupName = "New Group";
        SelectedDevices.Clear();
        SharedEffect = new RgbEffect { Name = "Group Effect" };
        StatusMessage = "Creating new sync group";
    }

    [RelayCommand]
    private async Task SaveGroupAsync()
    {
        if (string.IsNullOrWhiteSpace(GroupName))
        {
            StatusMessage = "Group name is required";
            return;
        }

        if (SelectedDevices.Count == 0)
        {
            StatusMessage = "Select at least one device";
            return;
        }

        try
        {
            var group = new RgbSyncGroup
            {
                Id = Guid.NewGuid(),
                Name = GroupName,
                DeviceIds = SelectedDevices.Select(d => d.Id).ToList(),
                SharedEffect = SharedEffect
            };

            SyncGroups.Add(group);
            SelectedGroup = group;

            // Apply effect to all devices in group
            foreach (var device in SelectedDevices)
            {
                await _rgbService.ApplyEffectAsync(device.Id.ToString(), SharedEffect);
            }

            IsCreatingNew = false;
            StatusMessage = $"Group '{GroupName}' created with {SelectedDevices.Count} devices";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving sync group");
            StatusMessage = "Error saving group";
        }
    }

    [RelayCommand]
    private void CancelCreate()
    {
        IsCreatingNew = false;
        GroupName = string.Empty;
        SelectedDevices.Clear();
    }

    [RelayCommand]
    private async Task DeleteGroupAsync(RgbSyncGroup? group)
    {
        if (group == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Group",
            $"Are you sure you want to delete '{group.Name}'?");

        if (confirmed)
        {
            SyncGroups.Remove(group);
            if (SelectedGroup?.Id == group.Id)
            {
                SelectedGroup = null;
            }
            StatusMessage = $"Group '{group.Name}' deleted";
        }
    }

    [RelayCommand]
    private async Task ApplyGroupEffectAsync(RgbSyncGroup? group)
    {
        if (group == null) return;

        try
        {
            foreach (var deviceId in group.DeviceIds)
            {
                await _rgbService.ApplyEffectAsync(deviceId.ToString(), group.SharedEffect);
            }

            StatusMessage = $"Effect applied to group '{group.Name}'";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying group effect");
            StatusMessage = "Error applying effect";
        }
    }

    [RelayCommand]
    private void AddDeviceToGroup(RgbDevice? device)
    {
        if (device == null) return;

        if (!SelectedDevices.Contains(device))
        {
            SelectedDevices.Add(device);
            StatusMessage = $"Added {device.Name} to selection";
        }
    }

    [RelayCommand]
    private void RemoveDeviceFromGroup(RgbDevice? device)
    {
        if (device == null) return;

        if (SelectedDevices.Contains(device))
        {
            SelectedDevices.Remove(device);
            StatusMessage = $"Removed {device.Name} from selection";
        }
    }

    [RelayCommand]
    private async Task EditGroupEffectAsync(RgbSyncGroup? group)
    {
        if (group == null) return;

        // Show effect editor
        SelectedGroup = group;
        SharedEffect = group.SharedEffect;
        StatusMessage = "Editing group effect";
    }

    [RelayCommand]
    private async Task UpdateGroupEffectAsync()
    {
        if (SelectedGroup == null) return;

        SelectedGroup.SharedEffect = SharedEffect;
        await ApplyGroupEffectAsync(SelectedGroup);
    }

    [RelayCommand]
    private void StartDragDevice(RgbDevice? device)
    {
        DraggedDevice = device;
    }

    [RelayCommand]
    private void DropDevice()
    {
        if (DraggedDevice != null)
        {
            AddDeviceToGroup(DraggedDevice);
            DraggedDevice = null;
        }
    }

    [RelayCommand]
    private void RenameGroup(RgbSyncGroup? group)
    {
        if (group == null) return;

        _dialogService.ShowInputDialogAsync("Rename Group", "Enter new name:", group.Name)
            .ContinueWith(t =>
            {
                if (t.Result != null && !string.IsNullOrWhiteSpace(t.Result))
                {
                    group.Name = t.Result;
                    StatusMessage = $"Group renamed to '{t.Result}'";
                }
            });
    }

    [RelayCommand]
    private async Task CloneGroupAsync(RgbSyncGroup? group)
    {
        if (group == null) return;

        var cloned = new RgbSyncGroup
        {
            Id = Guid.NewGuid(),
            Name = $"{group.Name} Copy",
            DeviceIds = new List<Guid>(group.DeviceIds),
            SharedEffect = new RgbEffect
            {
                Type = group.SharedEffect.Type,
                Colors = new List<RgbColor>(group.SharedEffect.Colors),
                Speed = group.SharedEffect.Speed,
                Brightness = group.SharedEffect.Brightness,
                Direction = group.SharedEffect.Direction
            }
        };

        SyncGroups.Add(cloned);
        SelectedGroup = cloned;
        StatusMessage = $"Group cloned as '{cloned.Name}'";
    }
}
