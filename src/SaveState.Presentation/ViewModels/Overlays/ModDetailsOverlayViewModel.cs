using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the mod details overlay.
/// </summary>
public partial class ModDetailsOverlayViewModel : ObservableObject
{
    [ObservableProperty]
    private string _modName = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private string _author = string.Empty;

    [ObservableProperty]
    private string _size = string.Empty;

    [ObservableProperty]
    private string _installDate = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private bool _hasChangelog;

    [ObservableProperty]
    private ObservableCollection<ChangelogEntryViewModel> _changelogEntries = new();

    [ObservableProperty]
    private bool _hasConflicts;

    [ObservableProperty]
    private ObservableCollection<ModConflictViewModel> _conflicts = new();

    [ObservableProperty]
    private bool _hasDependencies;

    [ObservableProperty]
    private ObservableCollection<ModDependencyViewModel> _dependencies = new();

    [ObservableProperty]
    private bool _canConfigure;

    private readonly SaveState.Presentation.Services.IOverlayService _overlayService;

    public ModDetailsOverlayViewModel(SaveState.Presentation.Services.IOverlayService overlayService)
    {
        _overlayService = overlayService;
        // Design-time data
        LoadDesignTimeData();
    }

    private void LoadDesignTimeData()
    {
        ModName = "HD Texture Pack";
        Version = "2.1.0";
        Author = "ModMaster3000";
        Size = "1.2 GB";
        InstallDate = "Jan 2, 2026";
        Description = "High-definition texture pack that enhances all game textures with 4K resolution. Includes improved character models, environment textures, and UI elements. Requires a GPU with at least 6GB VRAM for optimal performance.";

        HasChangelog = true;
        ChangelogEntries.Add(new ChangelogEntryViewModel
        {
            Version = "2.1.0",
            Date = "Jan 1, 2026",
            Changes = "• Added 4K textures for all weapons\n• Fixed texture flickering on water surfaces\n• Improved performance by 15%"
        });

        ChangelogEntries.Add(new ChangelogEntryViewModel
        {
            Version = "2.0.0",
            Date = "Dec 15, 2025",
            Changes = "• Complete overhaul of character textures\n• New environment textures\n• Added support for ray tracing"
        });

        HasConflicts = true;
        Conflicts.Add(new ModConflictViewModel
        {
            ModName = "Low-Poly Graphics Mod",
            ConflictReason = "Both mods modify the same texture files. Disable one to avoid conflicts."
        });

        HasDependencies = true;
        Dependencies.Add(new ModDependencyViewModel
        {
            Name = "Mod Framework v3.0+",
            Status = "Installed",
            StatusColor = "#10B981"
        });

        Dependencies.Add(new ModDependencyViewModel
        {
            Name = "Texture Loader v2.5+",
            Status = "Installed",
            StatusColor = "#10B981"
        });

        CanConfigure = true;
    }

    [RelayCommand]
    private void Configure()
    {
        // Open mod configuration
    }

    [RelayCommand]
    private void Uninstall()
    {
        // Uninstall mod
    }

    public void Initialize(Guid modId)
    {
        ModName = $"Mod {modId}";
        // Load actual mod data
    }

    [RelayCommand]
    private void Close()
    {
        _overlayService.HideModDetailsOverlay();
    }
}

/// <summary>
/// ViewModel for a changelog entry.
/// </summary>
public class ChangelogEntryViewModel
{
    public string Version { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string Changes { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for a mod conflict.
/// </summary>
public class ModConflictViewModel
{
    public string ModName { get; set; } = string.Empty;
    public string ConflictReason { get; set; } = string.Empty;
}

/// <summary>
/// ViewModel for a mod dependency.
/// </summary>
public class ModDependencyViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusColor { get; set; } = "#6B7280";
}
