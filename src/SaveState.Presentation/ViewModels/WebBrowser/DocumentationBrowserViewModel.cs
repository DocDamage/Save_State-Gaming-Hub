using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.WebBrowser;

/// <summary>
/// ViewModel for the in-app documentation browser.
/// </summary>
public partial class DocumentationBrowserViewModel : ObservableObject
{
    private readonly ILogger<DocumentationBrowserViewModel> _logger;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private ObservableCollection<DocumentationSection> _sections = new();

    [ObservableProperty]
    private DocumentationSection? _selectedSection;

    [ObservableProperty]
    private ObservableCollection<DocumentationArticle> _articles = new();

    [ObservableProperty]
    private DocumentationArticle? _selectedArticle;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _currentContent = string.Empty;

    [ObservableProperty]
    private string _currentTitle = "Welcome";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<DocumentationArticle> _searchResults = new();

    [ObservableProperty]
    private bool _isSearchMode;

    public DocumentationBrowserViewModel(
        ILogger<DocumentationBrowserViewModel> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;

        LoadDocumentationSections();
    }

    private void LoadDocumentationSections()
    {
        // User Manual Section
        var userManual = new DocumentationSection
        {
            Title = "User Manual",
            Icon = "📖",
            Articles = new List<DocumentationArticle>
            {
                new() { Title = "Getting Started", Content = GetGettingStartedContent() },
                new() { Title = "Game Library", Content = GetGameLibraryContent() },
                new() { Title = "Save States", Content = GetSaveStatesContent() },
                new() { Title = "Cloud Sync", Content = GetCloudSyncContent() },
                new() { Title = "Importing Games", Content = GetImportingGamesContent() }
            }
        };

        // Feature Guides Section
        var featureGuides = new DocumentationSection
        {
            Title = "Feature Guides",
            Icon = "✨",
            Articles = new List<DocumentationArticle>
            {
                new() { Title = "AI Companion", Content = GetAiCompanionContent() },
                new() { Title = "MUGEN Workbench", Content = GetMugenWorkbenchContent() },
                new() { Title = "RetroArch Integration", Content = GetRetroArchContent() },
                new() { Title = "Cloud Gaming", Content = GetCloudGamingContent() },
                new() { Title = "Memory Intelligence", Content = GetMemoryIntelligenceContent() },
                new() { Title = "Big Picture Mode", Content = GetBigPictureContent() },
                new() { Title = "Mobile Companion", Content = GetMobileCompanionContent() }
            }
        };

        // Keyboard Shortcuts Section
        var shortcuts = new DocumentationSection
        {
            Title = "Keyboard Shortcuts",
            Icon = "⌨️",
            Articles = new List<DocumentationArticle>
            {
                new() { Title = "Global Shortcuts", Content = GetGlobalShortcutsContent() },
                new() { Title = "Game Library", Content = GetLibraryShortcutsContent() },
                new() { Title = "Save States", Content = GetSaveStateShortcutsContent() },
                new() { Title = "In-Game Overlay", Content = GetOverlayShortcutsContent() }
            }
        };

        // FAQ Section
        var faq = new DocumentationSection
        {
            Title = "FAQ",
            Icon = "❓",
            Articles = new List<DocumentationArticle>
            {
                new() { Title = "General Questions", Content = GetGeneralFaqContent() },
                new() { Title = "Save States", Content = GetSaveStateFaqContent() },
                new() { Title = "Cloud Sync", Content = GetCloudSyncFaqContent() },
                new() { Title = "Performance", Content = GetPerformanceFaqContent() },
                new() { Title = "Troubleshooting", Content = GetTroubleshootingFaqContent() }
            }
        };

        // Troubleshooting Section
        var troubleshooting = new DocumentationSection
        {
            Title = "Troubleshooting",
            Icon = "🔧",
            Articles = new List<DocumentationArticle>
            {
                new() { Title = "Game Won't Launch", Content = GetWontLaunchContent() },
                new() { Title = "Save State Issues", Content = GetSaveStateIssuesContent() },
                new() { Title = "Cloud Sync Problems", Content = GetSyncProblemsContent() },
                new() { Title = "Performance Issues", Content = GetPerformanceIssuesContent() },
                new() { Title = "Reset Settings", Content = GetResetSettingsContent() }
            }
        };

        // Video Tutorials Section
        var videos = new DocumentationSection
        {
            Title = "Video Tutorials",
            Icon = "🎥",
            Articles = new List<DocumentationArticle>
            {
                new() { Title = "Quick Start Guide", VideoUrl = "https://www.youtube.com/watch?v=example1" },
                new() { Title = "Save State Mastery", VideoUrl = "https://www.youtube.com/watch?v=example2" },
                new() { Title = "MUGEN Character Creation", VideoUrl = "https://www.youtube.com/watch?v=example3" },
                new() { Title = "Cloud Gaming Setup", VideoUrl = "https://www.youtube.com/watch?v=example4" },
                new() { Title = "Memory Scanning Tutorial", VideoUrl = "https://www.youtube.com/watch?v=example5" }
            }
        };

        Sections.Add(userManual);
        Sections.Add(featureGuides);
        Sections.Add(shortcuts);
        Sections.Add(faq);
        Sections.Add(troubleshooting);
        Sections.Add(videos);

        // Select first section and article
        SelectedSection = Sections.FirstOrDefault();
        if (SelectedSection != null)
        {
            Articles = new ObservableCollection<DocumentationArticle>(SelectedSection.Articles);
            SelectedArticle = Articles.FirstOrDefault();
            if (SelectedArticle != null)
            {
                CurrentContent = SelectedArticle.Content;
                CurrentTitle = SelectedArticle.Title;
            }
        }
    }

    [RelayCommand]
    private void SelectSection(DocumentationSection section)
    {
        if (section == null) return;

        SelectedSection = section;
        Articles = new ObservableCollection<DocumentationArticle>(section.Articles);
        IsSearchMode = false;

        _logger.LogDebug("Selected documentation section: {Section}", section.Title);
    }

    [RelayCommand]
    private void SelectArticle(DocumentationArticle article)
    {
        if (article == null) return;

        SelectedArticle = article;
        CurrentContent = article.Content;
        CurrentTitle = article.Title;

        _logger.LogDebug("Selected documentation article: {Article}", article.Title);
    }

    [RelayCommand]
    private void SearchDocumentation()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            IsSearchMode = false;
            return;
        }

        IsSearchMode = true;
        SearchResults.Clear();

        var query = SearchQuery.ToLowerInvariant();

        foreach (var section in Sections)
        {
            foreach (var article in section.Articles)
            {
                if (article.Title.ToLowerInvariant().Contains(query) ||
                    article.Content.ToLowerInvariant().Contains(query))
                {
                    SearchResults.Add(article);
                }
            }
        }

        _logger.LogInformation("Documentation search for '{Query}' returned {Count} results", 
            SearchQuery, SearchResults.Count);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        IsSearchMode = false;
        SearchResults.Clear();
    }

    [RelayCommand]
    private void PrintCurrentPage()
    {
        _notificationService.ShowInfo("Printing documentation...");
        // Would integrate with print dialog
    }

    [RelayCommand]
    private void OpenExternalHelp()
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "https://savestate.reborn/docs",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open external help");
            _notificationService.ShowError("Failed to open help page");
        }
    }

    #region Content Generators

    private static string GetGettingStartedContent() => @"
# Getting Started with SaveState Reborn

Welcome to SaveState Reborn! This guide will help you get up and running quickly.

## Installation

1. Download SaveState Reborn from the official website
2. Run the installer and follow the prompts
3. Launch the application

## First Launch

On first launch, SaveState will:
- Scan for installed games from Steam, Epic, GOG, and other platforms
- Create your game library
- Set up default configurations

## Quick Tour

### Main Interface
- **Game Library**: Browse and manage your games
- **Save States**: Create and manage save states
- **Cloud Sync**: Sync your data across devices
- **Settings**: Configure application preferences

### Adding Games
You can add games by:
- Auto-detection from installed platforms
- Manual addition with executable path
- ROM import for emulated games
- MUGEN/Ikemen content

## Next Steps

- Explore the [Game Library](game-library) features
- Learn about [Save States](save-states)
- Set up [Cloud Sync](cloud-sync)
";

    private static string GetGameLibraryContent() => @"
# Game Library

The Game Library is your central hub for managing all your games.

## Features

### Views
- **Grid View**: Visual grid with cover art
- **List View**: Detailed list with sortable columns
- **Big Picture**: 10-foot UI for living room gaming

### Organization
- Collections and categories
- Tags and filtering
- Custom sorting
- Search functionality

### Game Details
Each game shows:
- Cover art and media
- Playtime statistics
- Achievement progress
- Save state history
- Notes and ratings

## Importing Games

### Automatic Import
SaveState automatically detects games from:
- Steam
- Epic Games Store
- GOG Galaxy
- Origin/EA App
- Ubisoft Connect
- Xbox Game Pass

### Manual Import
For games not auto-detected:
1. Click '+' in the toolbar
2. Select executable or ROM file
3. Add metadata
4. Save

## Managing Games

Right-click any game to:
- Launch
- Create save state
- Edit details
- Set categories
- View properties
";

    private static string GetSaveStatesContent() => @"
# Save States

Save states allow you to save and resume your game progress at any point.

## Creating Save States

### Quick Save
- Press F5 to quick save
- Press F9 to quick load

### Manual Save
1. Open the overlay (Shift+Tab)
2. Click 'Create Save State'
3. Add description
4. Choose branch (optional)

## Save State Features

### Branching
- Create multiple save paths
- Compare different routes
- Merge branches
- Visual timeline

### Metadata
Each save state includes:
- Screenshot
- Timestamp
- Description
- Tags
- Playtime

### Cloud Sync
- Automatically sync save states
- Access from any device
- Version history

## Best Practices

1. Save before difficult sections
2. Use descriptive names
3. Organize with branches
4. Enable auto-save for important games
";

    private static string GetCloudSyncContent() => @"
# Cloud Sync

Keep your save states and settings synchronized across all your devices.

## Setup

1. Go to Settings > Cloud Sync
2. Choose your provider:
   - Google Drive
   - Dropbox
   - OneDrive
   - iCloud
3. Authenticate
4. Configure sync options

## Sync Options

### What to Sync
- Save states
- Game library
- Settings
- Screenshots
- Achievements

### Sync Modes
- **Real-time**: Sync immediately
- **Scheduled**: Sync at intervals
- **Manual**: Sync on demand

## Conflict Resolution

When conflicts occur:
1. SaveState detects the conflict
2. Shows both versions
3. You choose which to keep
4. Or merge the changes

## Bandwidth

- Compressed transfers
- Incremental sync
- Bandwidth limiting options
";

    private static string GetImportingGamesContent() => @"
# Importing Games

Add your games to SaveState for unified management.

## Platform Integration

### Steam
Automatically detects:
- Installed games
- Playtime
- Achievements
- Last played

### Epic Games Store
Import your Epic library with:
- Cover art
- Metadata
- Installation status

### GOG
Full GOG Galaxy integration:
- Games
- Playtime
- Cloud saves

### Emulated Games
Import ROMs from:
- NES, SNES, N64
- Genesis, Saturn, Dreamcast
- PlayStation 1 & 2
- And more!

## Manual Import

For games not auto-detected:
1. Click '+' button
2. Select executable
3. Fill game details
4. Add cover art (optional)
5. Save

## Bulk Import

Import multiple games at once:
- Scan folders
- Auto-detect emulators
- Batch edit metadata
";

    private static string GetAiCompanionContent() => @"
# AI Companion

Your personal gaming assistant powered by AI.

## Features

### Voice Commands
Speak naturally to:
- Launch games
- Create save states
- Search your library
- Control playback

### Game Help
Ask questions about:
- Game mechanics
- Quest solutions
- Item locations
- Strategy tips

### Smart Recommendations
Get suggestions for:
- What to play next
- Games on sale
- Hidden gems
- Similar titles

### Session Analysis
Review your gaming:
- Play patterns
- Achievement progress
- Time management
- Skill development
";

    private static string GetMugenWorkbenchContent() => @"
# MUGEN/Ikemen Workbench

Complete fighting game development environment.

## Features

### Character Management
- Browse characters
- Install from downloads
- Test and validate
- Organize collections

### Stage Editor
- Create stages
- Edit backgrounds
- Add music
- Configure cameras

### Screenpack Editor
- Design menus
- Customize UI
- Add animations
- Edit fonts

### AI Training
- Train AI fighters
- Set behaviors
- Test matchups
- Export AI files

## Character Fusion

Create fusion characters:
1. Select two characters
2. Choose fusion style (Vegito/Potara)
3. Merge stats and moves
4. Test and refine

## Death Battle

Simulate epic battles:
- Research characters
- Set conditions
- Run simulations
- View results
";

    private static string GetRetroArchContent() => @"
# RetroArch Integration

Enhanced retro gaming with RetroArch.

## Setup

1. Install RetroArch
2. Configure core directory
3. Scan for ROMs
4. Launch games

## Features

### Core Management
- Download cores
- Update automatically
- Configure per-game
- Switch cores easily

### Netplay
- Online multiplayer
- Spectator mode
- Lobby system
- Rollback netcode

### Shaders
- CRT effects
- Scanlines
- Upscaling
- Custom shaders

### Save State Integration
- Unified save states
- Cloud sync support
- Import/export
";

    private static string GetCloudGamingContent() => @"
# Cloud Gaming

Stream games from the cloud.

## Supported Services

### GeForce NOW
- Full integration
- Game sync
- Session management

### Xbox Cloud Gaming
- Xbox Game Pass
- Touch controls
- Quick resume

### Amazon Luna
- Luna library access
- Controller support
- 4K streaming

## Features

### Connection Testing
- Speed test
- Latency check
- Quality estimation
- Server selection

### Streaming Overlay
- Performance stats
- Quick actions
- Chat integration
- Stream controls

### Hybrid Gaming
Switch between:
- Local play
- Cloud streaming
- Based on performance
";

    private static string GetMemoryIntelligenceContent() => @"
# Memory Intelligence

Advanced game memory analysis and modification.

## Features

### Memory Scanning
- Scan for values
- Filter results
- Track changes
- Find pointers

### Value Editing
- Edit in real-time
- Freeze values
- Set breakpoints
- Monitor changes

### Signature Database
- 5,000+ games
- Pre-configured signatures
- Community contributions
- Auto-detection

### Pattern Recognition
- AI-powered detection
- Heuristic analysis
- Automatic discovery
- Save and share

## Use Cases

### Cheat Development
- Find health addresses
- Modify ammo counts
- Unlock abilities
- Debug games

### Tool-Assisted Speedruns
- Frame-perfect inputs
- Memory watching
- Rerecording
- Playback

### Game Research
- Analyze mechanics
- Find hidden content
- Understand behavior
- Extract data
";

    private static string GetBigPictureContent() => @"
# Big Picture Mode

10-foot UI optimized for TV and controller use.

## Features

### Interface
- Large text and icons
- Controller navigation
- Voice commands
- Simplified menus

### On-Screen Keyboard
- Gamepad input
- Predictive text
- Quick phrases
- Emoji support

### Steam Deck Support
- Optimized layout
- Touch controls
- Gyro aiming
- Performance profiles

### Launch Experience
- Pre-launch briefings
- Achievement progress
- Last save state
- Quick actions

## Navigation

Use your controller to:
- Browse library
- Launch games
- Create save states
- Access settings

## Customization

- Change themes
- Adjust font size
- Configure sounds
- Set animations
";

    private static string GetMobileCompanionContent() => @"
# Mobile Companion App

Control SaveState from your phone.

## Features

### Remote Control
- Launch games
- Create save states
- View library
- Check status

### Second Screen
- Map display
- Inventory management
- Chat integration
- Stream viewing

### Notifications
- Achievement alerts
- Save state confirmations
- Friend activity
- Game invites

### QR Code Pairing
1. Open mobile app
2. Scan QR code in SaveState
3. Confirm pairing
4. Start controlling

## Security

- Encrypted connection
- Local network only
- Manual approval
- Revoke access anytime
";

    private static string GetGlobalShortcutsContent() => GetShortcutsTable(new[]
    {
        ("Ctrl + Space", "Quick Search"),
        ("Ctrl + Tab", "Switch View"),
        ("Ctrl + N", "Add New Game"),
        ("Ctrl + S", "Create Save State"),
        ("Ctrl + O", "Open Overlay"),
        ("Ctrl + ,", "Settings"),
        ("F11", "Toggle Fullscreen"),
        ("Ctrl + Q", "Quit Application")
    });

    private static string GetLibraryShortcutsContent() => GetShortcutsTable(new[]
    {
        ("Ctrl + F", "Focus Search"),
        ("Ctrl + G", "Toggle Grid/List View"),
        ("Ctrl + R", "Refresh Library"),
        ("Delete", "Remove Selected Game"),
        ("Enter", "Launch Selected Game"),
        ("Ctrl + E", "Edit Game Details"),
        ("Ctrl + Shift + S", "Create Save State for Selected")
    });

    private static string GetSaveStateShortcutsContent() => GetShortcutsTable(new[]
    {
        ("F5", "Quick Save"),
        ("F9", "Quick Load"),
        ("Ctrl + Shift + S", "Create Named Save State"),
        ("Ctrl + Shift + L", "Load Save State"),
        ("Ctrl + B", "Create New Branch"),
        ("Ctrl + M", "Merge Branches")
    });

    private static string GetOverlayShortcutsContent() => GetShortcutsTable(new[]
    {
        ("Shift + Tab", "Toggle Overlay"),
        ("Ctrl + Shift + O", "Open/Close Overlay"),
        ("F12", "Take Screenshot"),
        ("Ctrl + F12", "Start/Stop Recording"),
        ("Ctrl + Shift + T", "Show Performance HUD")
    });

    private static string GetShortcutsTable((string Key, string Action)[] shortcuts)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("| Shortcut | Action |");
        sb.AppendLine("|----------|--------|");
        foreach (var (key, action) in shortcuts)
        {
            sb.AppendLine($"| {key} | {action} |");
        }
        return sb.ToString();
    }

    private static string GetGeneralFaqContent() => @"
# General FAQ

## What is SaveState Reborn?

SaveState Reborn is a comprehensive gaming management platform with advanced features like save state management, cloud sync, AI assistance, and more.

## Is it free?

SaveState Reborn has both free and premium features. Core functionality is free, with advanced features available in premium tiers.

## What platforms are supported?

- Windows 10/11 (full features)
- Linux/Steam Deck (most features)
- macOS (basic features)

## Can I import my existing save files?

Yes! SaveState can import save files from most games and platforms.
";

    private static string GetSaveStateFaqContent() => @"
# Save State FAQ

## How do save states work?

Save states capture the complete memory state of a running game, allowing you to resume from that exact point later.

## Do all games support save states?

Most single-player games support save states. Some online/multiplayer games may have limitations due to anti-cheat systems.

## Can I share save states?

Yes, save states can be exported and shared with others (for supported games).

## How much space do save states use?

Typically 10-100MB per save state, depending on the game.
";

    private static string GetCloudSyncFaqContent() => @"
# Cloud Sync FAQ

## Which cloud providers are supported?

- Google Drive
- Dropbox
- OneDrive
- iCloud
- WebDAV (custom)

## Is my data secure?

Yes, all data is encrypted both in transit and at rest.

## Can I use multiple devices?

Yes, sync works across unlimited devices.

## What happens if there's a conflict?

SaveState detects conflicts and allows you to choose which version to keep or merge changes.
";

    private static string GetPerformanceFaqContent() => @"
# Performance FAQ

## Does SaveState affect game performance?

Minimal impact. SaveState runs in the background and uses resources only when actively creating save states.

## How much RAM does SaveState use?

Typically 100-300MB depending on library size.

## Can I limit CPU usage?

Yes, go to Settings > Performance to configure resource limits.
";

    private static string GetTroubleshootingFaqContent() => @"
# Troubleshooting FAQ

## Where are log files located?

`%AppData%\SaveState\logs\` on Windows
`~/.config/SaveState/logs/` on Linux
`~/Library/Logs/SaveState/` on macOS

## How do I reset settings?

Go to Settings > Advanced > Reset to Defaults, or delete the config file.

## Who can I contact for help?

- Discord: discord.gg/savestate
- Email: support@savestate.reborn
- Forums: forums.savestate.reborn
";

    private static string GetWontLaunchContent() => @"
# Game Won't Launch

## Check the Basics

1. Verify game files through platform (Steam, Epic, etc.)
2. Run as administrator
3. Update graphics drivers
4. Check antivirus isn't blocking the game

## SaveState-Specific Solutions

1. Try launching directly (bypass SaveState)
2. Check launch configuration in game properties
3. Verify executable path is correct
4. Try compatibility mode

## Still Not Working?

Check the logs:
- SaveState logs: Settings > Advanced > View Logs
- Windows Event Viewer
- Game-specific logs
";

    private static string GetSaveStateIssuesContent() => @"
# Save State Issues

## Save State Won't Create

- Ensure game is running
- Check disk space
- Verify write permissions
- Try creating manually (not quick save)

## Save State Won't Load

- Verify save state file exists
- Check game version matches
- Try loading from overlay instead of hotkey
- Check for game updates

## Corrupted Save State

SaveState keeps backups:
1. Right-click save state
2. Select 'Restore Backup'
3. Choose backup version
";

    private static string GetSyncProblemsContent() => @"
# Cloud Sync Problems

## Sync Not Working

1. Check internet connection
2. Verify cloud provider is authenticated
3. Check sync settings (Settings > Cloud Sync)
4. Try manual sync

## Sync Too Slow

- Enable compression (Settings > Cloud Sync)
- Limit bandwidth usage
- Schedule sync for off-peak hours
- Exclude large files

## Conflict Resolution

When conflicts occur:
1. Review both versions
2. Choose which to keep
3. Or manually merge
4. Resolve all conflicts before next sync
";

    private static string GetPerformanceIssuesContent() => @"
# Performance Issues

## SaveState Running Slow

1. Close unnecessary panels
2. Reduce animation effects
3. Limit library size shown
4. Check for updates

## Games Lagging When SaveState is Running

1. Enable game mode (Settings > Performance)
2. Reduce overlay frequency
3. Disable background scanning
4. Lower quality settings

## High CPU/Memory Usage

- Check for runaway processes
- Restart SaveState
- Clear cache (Settings > Advanced)
- Report issue if persistent
";

    private static string GetResetSettingsContent() => @"
# Reset Settings

## Reset to Defaults

Settings > Advanced > Reset All Settings

This will:
- Restore default preferences
- Keep your game library
- Keep save states
- Keep cloud sync settings

## Complete Reset

To completely reset:
1. Close SaveState
2. Delete config folder:
   - Windows: `%AppData%\SaveState\`
   - Linux: `~/.config/SaveState/`
   - macOS: `~/Library/Application Support/SaveState/`
3. Restart SaveState

## Backup First

Before resetting:
1. Export settings (Settings > Advanced > Export)
2. Backup save states
3. Note your cloud sync settings
";

    #endregion
}

/// <summary>
/// Represents a section in the documentation.
/// </summary>
public class DocumentationSection
{
    public string Title { get; set; } = string.Empty;
    public string Icon { get; set; } = "📄";
    public List<DocumentationArticle> Articles { get; set; } = new();
}

/// <summary>
/// Represents a documentation article.
/// </summary>
public class DocumentationArticle
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? VideoUrl { get; set; }
}
