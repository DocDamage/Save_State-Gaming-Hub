using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Automation.Services;
using SaveState.Presentation.Services;

namespace SaveState.Presentation.ViewModels.Automation;

/// <summary>
/// View model for the macro marketplace where users can discover, download,
/// upload, and share automation macros.
/// </summary>
public partial class MacroMarketplaceViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly IMacroManager _macroManager;
    private readonly INotificationService _notificationService;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private bool _isUploading;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _selectedCategory = "All";

    [ObservableProperty]
    private string _sortBy = "Popular";

    [ObservableProperty]
    private MarketplaceMacro? _selectedMacro;

    [ObservableProperty]
    private string _statusMessage = "Browse community macros";

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _totalPages = 1;

    public ObservableCollection<MarketplaceMacro> AvailableMacros { get; } = new();
    public ObservableCollection<MarketplaceMacro> InstalledMacros { get; } = new();
    public ObservableCollection<MarketplaceMacro> MyUploads { get; } = new();
    public ObservableCollection<string> Categories { get; } = new()
    {
        "All",
        "Gaming",
        "Productivity",
        "Automation",
        "Utility",
        "Development",
        "Entertainment"
    };
    public ObservableCollection<string> SortOptions { get; } = new()
    {
        "Popular",
        "Recent",
        "Top Rated",
        "Most Downloaded",
        "Name (A-Z)"
    };

    public MacroMarketplaceViewModel(
        IMediator mediator,
        IMacroManager macroManager,
        INotificationService notificationService)
    {
        _mediator = mediator;
        _macroManager = macroManager;
        _notificationService = notificationService;
    }

    public async Task InitializeAsync()
    {
        await LoadMarketplaceMacrosAsync();
        await LoadInstalledMacrosAsync();
        await LoadMyUploadsAsync();
    }

    [RelayCommand]
    private async Task LoadMarketplaceMacrosAsync()
    {
        IsLoading = true;
        StatusMessage = "Loading macros from marketplace...";

        try
        {
            // In a real implementation, this would query a marketplace API
            // For now, we'll create some sample data
            AvailableMacros.Clear();

            var sampleMacros = new[]
            {
                new MarketplaceMacro
                {
                    Id = Guid.NewGuid(),
                    Name = "Auto Save Manager",
                    Description = "Automatically creates save states at key moments in your games",
                    Author = "CommunityUser1",
                    Category = "Gaming",
                    Downloads = 1250,
                    Rating = 4.5,
                    Version = "1.2.0",
                    UpdatedAt = DateTime.UtcNow.AddDays(-5),
                    Tags = new List<string> { "save-state", "automation", "rpg" },
                    IsInstalled = false
                },
                new MarketplaceMacro
                {
                    Id = Guid.NewGuid(),
                    Name = "Achievement Hunter",
                    Description = "Assists with achievement tracking and completion",
                    Author = "AchievementPro",
                    Category = "Gaming",
                    Downloads = 980,
                    Rating = 4.8,
                    Version = "2.0.1",
                    UpdatedAt = DateTime.UtcNow.AddDays(-2),
                    Tags = new List<string> { "achievements", "tracking", "helper" },
                    IsInstalled = false
                },
                new MarketplaceMacro
                {
                    Id = Guid.NewGuid(),
                    Name = "Screenshot Organizer",
                    Description = "Automatically organizes and tags your game screenshots",
                    Author = "MediaMaster",
                    Category = "Utility",
                    Downloads = 750,
                    Rating = 4.3,
                    Version = "1.0.5",
                    UpdatedAt = DateTime.UtcNow.AddDays(-10),
                    Tags = new List<string> { "screenshots", "organization", "media" },
                    IsInstalled = true
                },
                new MarketplaceMacro
                {
                    Id = Guid.NewGuid(),
                    Name = "Session Recorder",
                    Description = "Records detailed statistics for every gaming session",
                    Author = "DataDriven",
                    Category = "Productivity",
                    Downloads = 1100,
                    Rating = 4.6,
                    Version = "1.5.0",
                    UpdatedAt = DateTime.UtcNow.AddDays(-1),
                    Tags = new List<string> { "recording", "statistics", "analytics" },
                    IsInstalled = false
                }
            };

            foreach (var macro in sampleMacros)
            {
                if (string.IsNullOrWhiteSpace(SearchQuery) ||
                    macro.Name.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase) ||
                    macro.Description.Contains(SearchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    if (SelectedCategory == "All" || macro.Category == SelectedCategory)
                    {
                        AvailableMacros.Add(macro);
                    }
                }
            }

            StatusMessage = $"Found {AvailableMacros.Count} macros";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load macros: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadInstalledMacrosAsync()
    {
        try
        {
            var result = await _macroManager.GetMacrosAsync();
            InstalledMacros.Clear();

            if (result.IsSuccess && result.Value != null)
            {
                foreach (var macro in result.Value)
                {
                    InstalledMacros.Add(new MarketplaceMacro
                    {
                        Id = macro.Id,
                        Name = macro.Name,
                        Description = macro.Description ?? "No description",
                        Category = "Local",
                        IsInstalled = true,
                        Version = "1.0.0",
                        UpdatedAt = macro.UpdatedAt
                    });
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load installed macros: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task LoadMyUploadsAsync()
    {
        try
        {
            // In a real implementation, this would query user's uploaded macros
            MyUploads.Clear();
            StatusMessage = "My uploads loaded";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to load uploads: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DownloadMacroAsync(MarketplaceMacro? macro)
    {
        if (macro == null)
        {
            _notificationService.ShowWarning("Please select a macro to download.");
            return;
        }

        IsDownloading = true;
        StatusMessage = $"Downloading {macro.Name}...";

        try
        {
            // Simulate download
            await Task.Delay(1500);

            macro.IsInstalled = true;
            macro.Downloads++;

            StatusMessage = $"{macro.Name} downloaded successfully!";
            _notificationService.ShowSuccess($"{macro.Name} installed!");

            await LoadInstalledMacrosAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Download failed: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
        finally
        {
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private async Task UninstallMacroAsync(MarketplaceMacro? macro)
    {
        if (macro == null)
        {
            _notificationService.ShowWarning("Please select a macro to uninstall.");
            return;
        }

        try
        {
            var result = await _macroManager.DeleteMacroAsync(macro.Id);

            if (result.IsSuccess)
            {
                macro.IsInstalled = false;
                StatusMessage = $"{macro.Name} uninstalled.";
                _notificationService.ShowSuccess($"{macro.Name} uninstalled!");
                await LoadInstalledMacrosAsync();
            }
            else
            {
                StatusMessage = $"Uninstall failed: {result.Error}";
                _notificationService.ShowError(result.Error ?? "Uninstall failed");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task UploadMacroAsync(Macro? macro)
    {
        if (macro == null)
        {
            _notificationService.ShowWarning("Please select a macro to upload.");
            return;
        }

        IsUploading = true;
        StatusMessage = $"Uploading {macro.Name}...";

        try
        {
            // Simulate upload
            await Task.Delay(2000);

            StatusMessage = $"{macro.Name} uploaded successfully!";
            _notificationService.ShowSuccess($"{macro.Name} is now available in the marketplace!");

            await LoadMyUploadsAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Upload failed: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
        finally
        {
            IsUploading = false;
        }
    }

    [RelayCommand]
    private async Task RateMacroAsync(RateMacroArgs? args)
    {
        if (args?.Macro == null) return;

        try
        {
            // In a real implementation, this would submit rating to server
            StatusMessage = $"Rated {args.Macro.Name} with {args.Rating} stars";
            _notificationService.ShowSuccess("Rating submitted!");
        }
        catch (Exception ex)
        {
            StatusMessage = $"Rating failed: {ex.Message}";
            _notificationService.ShowError($"Error: {ex.Message}");
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        _ = LoadMarketplaceMacrosAsync();
    }

    partial void OnSelectedCategoryChanged(string value)
    {
        _ = LoadMarketplaceMacrosAsync();
    }

    partial void OnSortByChanged(string value)
    {
        if (AvailableMacros.Count == 0) return;

        var sorted = value switch
        {
            "Recent" => AvailableMacros.OrderByDescending(m => m.UpdatedAt),
            "Top Rated" => AvailableMacros.OrderByDescending(m => m.Rating),
            "Most Downloaded" => AvailableMacros.OrderByDescending(m => m.Downloads),
            "Name (A-Z)" => AvailableMacros.OrderBy(m => m.Name),
            _ => AvailableMacros.OrderByDescending(m => m.Downloads) // Popular
        };

        AvailableMacros.Clear();
        foreach (var macro in sorted)
        {
            AvailableMacros.Add(macro);
        }
    }
}

/// <summary>
/// Represents a macro available in the marketplace.
/// </summary>
public sealed class MarketplaceMacro
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Downloads { get; set; }
    public double Rating { get; set; }
    public string Version { get; set; } = "1.0.0";
    public DateTime UpdatedAt { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsInstalled { get; set; }
    public string? ThumbnailUrl { get; set; }
    public long FileSize { get; set; }
}

public record RateMacroArgs(MarketplaceMacro Macro, int Rating);
