using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.RgbSync;

/// <summary>
/// View model for RGB profile management.
/// </summary>
public partial class RgbProfileManagerViewModel : ObservableObject
{
    private readonly IRgbSyncService _rgbService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<RgbProfileManagerViewModel> _logger;
    private readonly ITimeProvider _timeProvider;

    [ObservableProperty]
    private ObservableCollection<RgbProfile> _profiles = new();

    [ObservableProperty]
    private RgbProfile? _selectedProfile;

    [ObservableProperty]
    private RgbProfile? _defaultProfile;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private ObservableCollection<AutoSwitchRule> _autoSwitchRules = new();

    [ObservableProperty]
    private AutoSwitchRule? _selectedRule;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public RgbProfileManagerViewModel(
        IRgbSyncService rgbService,
        IDialogService dialogService,
        ILogger<RgbProfileManagerViewModel> logger,
        ITimeProvider timeProvider)
    {
        _rgbService = rgbService;
        _dialogService = dialogService;
        _logger = logger;
        _timeProvider = timeProvider;

        LoadProfiles();
        InitializeDefaultRules();
    }

    private void InitializeDefaultRules()
    {
        AutoSwitchRules.Add(new AutoSwitchRule
        {
            Id = Guid.NewGuid(),
            Name = "Gaming Mode",
            TriggerType = AutoSwitchTriggerType.GameLaunch,
            TriggerValue = "*",
            TargetProfileName = "Gaming"
        });

        AutoSwitchRules.Add(new AutoSwitchRule
        {
            Id = Guid.NewGuid(),
            Name = "Night Mode",
            TriggerType = AutoSwitchTriggerType.TimeOfDay,
            TriggerValue = "22:00-06:00",
            TargetProfileName = "Sleep"
        });
    }

    private async void LoadProfiles()
    {
        try
        {
            var configResult = await _rgbService.GetConfigurationAsync();
            if (configResult.IsSuccess)
            {
                // In a real implementation, profiles would be loaded from storage
                // For now, create some defaults
                if (Profiles.Count == 0)
                {
                    CreateDefaultProfiles();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading profiles");
            StatusMessage = "Error loading profiles";
        }
    }

    private void CreateDefaultProfiles()
    {
        var gamingProfile = new RgbProfile
        {
            Id = Guid.NewGuid(),
            Name = "Gaming",
            IsDefault = true,
            DeviceEffects = new Dictionary<Guid, RgbEffect>(),
            CreatedAt = _timeProvider.Now,
            ModifiedAt = _timeProvider.Now
        };

        var movieProfile = new RgbProfile
        {
            Id = Guid.NewGuid(),
            Name = "Movie",
            DeviceEffects = new Dictionary<Guid, RgbEffect>(),
            CreatedAt = _timeProvider.Now,
            ModifiedAt = _timeProvider.Now
        };

        var workProfile = new RgbProfile
        {
            Id = Guid.NewGuid(),
            Name = "Work",
            DeviceEffects = new Dictionary<Guid, RgbEffect>(),
            CreatedAt = _timeProvider.Now,
            ModifiedAt = _timeProvider.Now
        };

        var sleepProfile = new RgbProfile
        {
            Id = Guid.NewGuid(),
            Name = "Sleep",
            DeviceEffects = new Dictionary<Guid, RgbEffect>(),
            CreatedAt = _timeProvider.Now,
            ModifiedAt = _timeProvider.Now
        };

        Profiles.Add(gamingProfile);
        Profiles.Add(movieProfile);
        Profiles.Add(workProfile);
        Profiles.Add(sleepProfile);

        DefaultProfile = gamingProfile;
    }

    [RelayCommand]
    private async Task CreateProfileAsync()
    {
        var profileName = await _dialogService.ShowInputDialogAsync("Create Profile", "Enter profile name:", "New Profile");
        if (string.IsNullOrWhiteSpace(profileName))
            return;

        var profile = new RgbProfile
        {
            Id = Guid.NewGuid(),
            Name = profileName,
            DeviceEffects = new Dictionary<Guid, RgbEffect>(),
            CreatedAt = _timeProvider.Now,
            ModifiedAt = _timeProvider.Now
        };

        Profiles.Add(profile);
        SelectedProfile = profile;
        StatusMessage = $"Profile '{profileName}' created";
    }

    [RelayCommand]
    private async Task DeleteProfileAsync(RgbProfile? profile)
    {
        if (profile == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Delete Profile",
            $"Are you sure you want to delete '{profile.Name}'?");

        if (confirmed)
        {
            Profiles.Remove(profile);
            if (DefaultProfile?.Id == profile.Id)
            {
                DefaultProfile = Profiles.FirstOrDefault();
            }
            StatusMessage = $"Profile '{profile.Name}' deleted";
        }
    }

    [RelayCommand]
    private async Task DuplicateProfileAsync(RgbProfile? profile)
    {
        if (profile == null) return;

        var newName = $"{profile.Name} Copy";
        var duplicated = new RgbProfile
        {
            Id = Guid.NewGuid(),
            Name = newName,
            DeviceEffects = new Dictionary<Guid, RgbEffect>(profile.DeviceEffects),
            CreatedAt = _timeProvider.Now,
            ModifiedAt = _timeProvider.Now
        };

        Profiles.Add(duplicated);
        SelectedProfile = duplicated;
        StatusMessage = $"Profile duplicated as '{newName}'";
    }

    [RelayCommand]
    private async Task SetAsDefaultAsync(RgbProfile? profile)
    {
        if (profile == null) return;

        foreach (var p in Profiles)
        {
            p.IsDefault = p.Id == profile.Id;
        }

        DefaultProfile = profile;
        StatusMessage = $"'{profile.Name}' set as default profile";
    }

    [RelayCommand]
    private async Task ExportProfileAsync(RgbProfile? profile)
    {
        if (profile == null) return;

        IsExporting = true;
        try
        {
            // Serialize profile to JSON and save to file
            var json = System.Text.Json.JsonSerializer.Serialize(profile, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            // Show save file dialog
            var filePath = await _dialogService.ShowSaveFileDialogAsync(
                "Export Profile",
                "JSON files (*.json)|*.json|All files (*.*)|*.*",
                $"{profile.Name}.json");

            if (!string.IsNullOrWhiteSpace(filePath))
            {
                await File.WriteAllTextAsync(filePath, json);
                StatusMessage = $"Profile exported to '{filePath}'";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting profile");
            StatusMessage = "Error exporting profile";
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private async Task ImportProfileAsync()
    {
        IsImporting = true;
        try
        {
            var filePath = await _dialogService.ShowOpenFileDialogAsync(
                "Import Profile",
                "JSON files (*.json)|*.json|All files (*.*)|*.*");

            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                return;

            var json = await File.ReadAllTextAsync(filePath);
            var profile = System.Text.Json.JsonSerializer.Deserialize<RgbProfile>(json);

            if (profile != null)
            {
                profile.Id = Guid.NewGuid(); // Assign new ID
                profile.Name = $"{profile.Name} (Imported)";
                profile.CreatedAt = _timeProvider.Now;
                profile.ModifiedAt = _timeProvider.Now;

                Profiles.Add(profile);
                SelectedProfile = profile;
                StatusMessage = $"Profile '{profile.Name}' imported";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing profile");
            StatusMessage = "Error importing profile";
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private async Task ApplyProfileAsync(RgbProfile? profile)
    {
        if (profile == null) return;

        try
        {
            foreach (var (deviceId, effect) in profile.DeviceEffects)
            {
                await _rgbService.ApplyEffectAsync(deviceId.ToString(), effect);
            }

            StatusMessage = $"Profile '{profile.Name}' applied";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying profile");
            StatusMessage = "Error applying profile";
        }
    }

    [RelayCommand]
    private async Task CreateAutoSwitchRuleAsync()
    {
        var rule = new AutoSwitchRule
        {
            Id = Guid.NewGuid(),
            Name = "New Rule",
            TriggerType = AutoSwitchTriggerType.GameLaunch,
            TargetProfileName = SelectedProfile?.Name ?? "Gaming"
        };

        AutoSwitchRules.Add(rule);
        SelectedRule = rule;
    }

    [RelayCommand]
    private void DeleteAutoSwitchRule(AutoSwitchRule? rule)
    {
        if (rule != null && AutoSwitchRules.Contains(rule))
        {
            AutoSwitchRules.Remove(rule);
        }
    }

    [RelayCommand]
    private async Task EditAutoSwitchRuleAsync(AutoSwitchRule? rule)
    {
        if (rule == null) return;

        // Show edit dialog
        StatusMessage = $"Editing rule '{rule.Name}'";
    }
}

/// <summary>
/// Auto-switch rule for profile switching.
/// </summary>
public class AutoSwitchRule
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public AutoSwitchTriggerType TriggerType { get; set; }
    public string TriggerValue { get; set; } = string.Empty;
    public string TargetProfileName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Types of auto-switch triggers.
/// </summary>
public enum AutoSwitchTriggerType
{
    GameLaunch,
    TimeOfDay,
    DayOfWeek,
    ProcessStart,
    Idle,
    Manual
}
