using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using SaveState.Core.Data;
using SaveState.Core.Entities;
using SaveState.Core.Interfaces;
using SaveState.Core.Models;
using SaveState.Core.Services;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.UI.ViewModels;

public partial class RomManagerViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger = Log.ForContext<RomManagerViewModel>();

    [ObservableProperty]
    private ObservableCollection<Emulator> _emulators = new();

    [ObservableProperty]
    private ObservableCollection<RomFolder> _romFolders = new();

    [ObservableProperty]
    private Emulator? _selectedEmulator;

    [ObservableProperty]
    private string _newEmulatorName = string.Empty;

    [ObservableProperty]
    private string _newEmulatorPath = string.Empty;

    [ObservableProperty]
    private string _newEmulatorArgs = "\"{rom}\"";

    [ObservableProperty]
    private string _newEmulatorPlatforms = string.Empty;

    [ObservableProperty]
    private string _newRomFolderPath = string.Empty;

    [ObservableProperty]
    private string _selectedPlatform = "NES";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isScanning;

    public ObservableCollection<string> AvailablePlatforms { get; } = new(PlatformDefinitions.Platforms.Keys.OrderBy(k => k));

    public IAsyncRelayCommand AddEmulatorCommand { get; }
    public IAsyncRelayCommand DeleteEmulatorCommand { get; }
    public IAsyncRelayCommand AddRomFolderCommand { get; }
    public IAsyncRelayCommand<RomFolder> ScanRomFolderCommand { get; }
    public IAsyncRelayCommand DeleteRomFolderCommand { get; }
    public IAsyncRelayCommand ScanAllCommand { get; }
    public IRelayCommand BrowseEmulatorCommand { get; }
    public IRelayCommand BrowseRomFolderCommand { get; }

    public RomManagerViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        AddEmulatorCommand = new AsyncRelayCommand(AddEmulatorAsync);
        DeleteEmulatorCommand = new AsyncRelayCommand(DeleteEmulatorAsync);
        AddRomFolderCommand = new AsyncRelayCommand(AddRomFolderAsync);
        ScanRomFolderCommand = new AsyncRelayCommand<RomFolder>(ScanRomFolderAsync);
        DeleteRomFolderCommand = new AsyncRelayCommand(DeleteRomFolderAsync);
        ScanAllCommand = new AsyncRelayCommand(ScanAllFoldersAsync);
        BrowseEmulatorCommand = new RelayCommand(BrowseEmulator);
        BrowseRomFolderCommand = new RelayCommand(BrowseRomFolder);

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();
        
        var emulators = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.Emulators);
        Emulators = new ObservableCollection<Emulator>(emulators);

        var folders = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(db.RomFolders);
        RomFolders = new ObservableCollection<RomFolder>(folders);
    }

    private async Task AddEmulatorAsync()
    {
        if (string.IsNullOrWhiteSpace(NewEmulatorName) || string.IsNullOrWhiteSpace(NewEmulatorPath))
        {
            StatusMessage = "Please enter emulator name and path";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var emulatorService = scope.ServiceProvider.GetRequiredService<IEmulatorService>();

            var emulator = new Emulator
            {
                Name = NewEmulatorName,
                ExecutablePath = NewEmulatorPath,
                Arguments = NewEmulatorArgs,
                SupportedPlatforms = NewEmulatorPlatforms,
                IsDefault = Emulators.Count == 0
            };

            await emulatorService.AddAsync(emulator);
            Emulators.Add(emulator);

            NewEmulatorName = string.Empty;
            NewEmulatorPath = string.Empty;
            NewEmulatorPlatforms = string.Empty;
            StatusMessage = $"Added emulator: {emulator.Name}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to add emulator");
            StatusMessage = "Failed to add emulator";
        }
    }

    private async Task DeleteEmulatorAsync()
    {
        if (SelectedEmulator == null) return;

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var emulatorService = scope.ServiceProvider.GetRequiredService<IEmulatorService>();
            await emulatorService.DeleteAsync(SelectedEmulator.Id);
            Emulators.Remove(SelectedEmulator);
            StatusMessage = "Emulator deleted";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to delete emulator");
        }
    }

    private async Task AddRomFolderAsync()
    {
        if (string.IsNullOrWhiteSpace(NewRomFolderPath) || !Directory.Exists(NewRomFolderPath))
        {
            StatusMessage = "Please enter a valid folder path";
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();

            var folder = new RomFolder
            {
                Id = Guid.NewGuid(),
                Path = NewRomFolderPath,
                PlatformName = SelectedPlatform,
                ScanRecursively = true
            };

            db.RomFolders.Add(folder);
            await db.SaveChangesAsync();
            RomFolders.Add(folder);

            NewRomFolderPath = string.Empty;
            StatusMessage = $"Added ROM folder for {SelectedPlatform}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to add ROM folder");
            StatusMessage = "Failed to add folder";
        }
    }

    private async Task ScanRomFolderAsync(RomFolder? folder)
    {
        if (folder == null) return;

        IsScanning = true;
        StatusMessage = $"Scanning {folder.Path}...";

        try
        {
            var scanner = new RomScannerService();
            var games = await scanner.ScanFolderAsync(folder.Path, folder.PlatformName, folder.ScanRecursively);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<SaveStateDbContext>();
            var gameService = scope.ServiceProvider.GetRequiredService<IGameService>();

            // Get or create platform
            var platform = db.Platforms.FirstOrDefault(p => p.Name == folder.PlatformName);
            if (platform == null)
            {
                platform = new Platform { Id = Guid.NewGuid(), Name = folder.PlatformName };
                db.Platforms.Add(platform);
                await db.SaveChangesAsync();
            }

            var added = 0;
            foreach (var game in games)
            {
                var existing = db.Games.FirstOrDefault(g => g.InstallPath == game.InstallPath);
                if (existing == null)
                {
                    game.Platform = platform;
                    game.PlatformId = platform.Id;
                    await gameService.AddAsync(game);
                    added++;
                }
            }

            folder.LastScanned = DateTime.UtcNow;
            folder.RomCount = games.Count;
            db.RomFolders.Update(folder);
            await db.SaveChangesAsync();

            StatusMessage = $"Added {added} new ROMs from {folder.PlatformName}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to scan folder");
            StatusMessage = "Scan failed";
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task DeleteRomFolderAsync()
    {
        // Would need SelectedRomFolder property
    }

    private async Task ScanAllFoldersAsync()
    {
        IsScanning = true;
        var total = 0;

        foreach (var folder in RomFolders.ToList())
        {
            await ScanRomFolderAsync(folder);
        }

        IsScanning = false;
        StatusMessage = "All folders scanned!";
    }

    private void BrowseEmulator()
    {
        // Would use file picker dialog in actual implementation
        StatusMessage = "Use file picker to select emulator executable";
    }

    private void BrowseRomFolder()
    {
        // Would use folder picker dialog in actual implementation
        StatusMessage = "Use folder picker to select ROM directory";
    }
}
