using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Application.GameLibrary.DTOs;
using SaveState.Application.GameLibrary.Services;
using SaveState.Core.GameLibrary.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Library;

/// <summary>
/// View model for the library import dialog.
/// </summary>
public partial class LibraryImportDialogViewModel : ObservableObject
{
    private readonly IGameImportService _importService;
    private readonly IEnumerable<IGameProvider> _gameProviders;
    private readonly ILogger<LibraryImportDialogViewModel> _logger;
    private CancellationTokenSource? _cancellationTokenSource;

    [ObservableProperty]
    private ObservableCollection<PlatformProviderViewModel> _providers = new();

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private bool _isComplete;

    [ObservableProperty]
    private string _currentStage = string.Empty;

    [ObservableProperty]
    private string _currentMessage = string.Empty;

    [ObservableProperty]
    private string? _currentProvider;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private string _progressText = "0%";

    [ObservableProperty]
    private bool _skipMetadata;

    [ObservableProperty]
    private bool _overwriteExisting;

    [ObservableProperty]
    private ImportResult? _result;

    [ObservableProperty]
    private string _resultSummary = string.Empty;

    [ObservableProperty]
    private bool _hasErrors;

    public LibraryImportDialogViewModel(
        IGameImportService importService,
        IEnumerable<IGameProvider> providers,
        ILogger<LibraryImportDialogViewModel> logger)
    {
        _importService = importService;
        _gameProviders = providers;
        _logger = logger;

        InitializeProviders();
    }

    private void InitializeProviders()
    {
        // Map of provider names to display information
        var providerInfo = new Dictionary<string, (string DisplayName, string Icon, string Color)>
        {
            ["Steam"] = ("Steam", "🎮", "#1B2838"),
            ["GOG"] = ("GOG Galaxy", "🎯", "#86328A"),
            ["Epic"] = ("Epic Games", "🎪", "#313131"),
            ["Origin/EA"] = ("Origin/EA App", "🎲", "#F07A21"),
            ["Ubisoft"] = ("Ubisoft Connect", "🎨", "#0081CC"),
            ["Battle.net"] = ("Battle.net", "⚔️", "#148EFF"),
            ["Xbox"] = ("Xbox Game Pass", "🎯", "#107C10")
        };

        foreach (var provider in _gameProviders)
        {
            var (displayName, icon, color) = providerInfo.TryGetValue(provider.Name, out var value)
                ? value
                : (provider.Name, "🎮", "#666666");

            var vm = new PlatformProviderViewModel(
                provider.Name,
                displayName,
                icon,
                color,
                provider.Capabilities.HasFlag(ProviderCapabilities.Discovery),
                provider.Capabilities.HasFlag(ProviderCapabilities.Metadata),
                provider.Capabilities.HasFlag(ProviderCapabilities.Launch))
            {
                IsEnabled = true
            };

            Providers.Add(vm);
        }
    }

    [RelayCommand]
    private async Task StartImportAsync()
    {
        try
        {
            IsImporting = true;
            IsComplete = false;
            HasErrors = false;
            OverallProgress = 0;
            Result = null;
            ResultSummary = string.Empty;

            // Reset provider states
            foreach (var provider in Providers)
            {
                provider.UpdateStatus("Pending");
                provider.UpdateGamesFound(0);
            }

            _cancellationTokenSource = new CancellationTokenSource();

            var options = new ImportOptions
            {
                SkipMetadata = SkipMetadata,
                OverwriteExisting = OverwriteExisting,
                ProviderFilter = Providers
                    .Where(p => p.IsEnabled)
                    .Select(p => p.Name)
                    .ToList()
            };

            var progress = new Progress<ImportProgress>(OnProgressChanged);

            var result = await _importService.ImportAllLibrariesAsync(
                options,
                progress,
                _cancellationTokenSource.Token);

            Result = result;
            IsComplete = true;
            HasErrors = !result.IsSuccess;

            // Update provider results
            foreach (var providerResult in result.ProviderResults)
            {
                var providerVm = Providers.FirstOrDefault(p => p.Name == providerResult.Key);
                if (providerVm != null)
                {
                    if (providerResult.Value.Success)
                    {
                        providerVm.UpdateStatus("Complete", true);
                        providerVm.UpdateGamesFound(providerResult.Value.GamesFound);
                    }
                    else
                    {
                        providerVm.UpdateStatus("Failed", false, providerResult.Value.Error);
                    }
                }
            }

            // Generate summary
            ResultSummary = $"Import completed in {result.Duration.TotalSeconds:F1}s\n" +
                          $"✓ {result.GamesImported} games imported\n" +
                          (result.GamesFailed > 0 ? $"✗ {result.GamesFailed} failed\n" : "") +
                          (result.GamesSkipped > 0 ? $"⊘ {result.GamesSkipped} skipped\n" : "");

            _logger.LogInformation("Library import completed: {Imported} imported, {Failed} failed, {Skipped} skipped",
                result.GamesImported, result.GamesFailed, result.GamesSkipped);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Library import was cancelled");
            ResultSummary = "Import cancelled by user";
            IsComplete = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Library import failed");
            ResultSummary = $"Import failed: {ex.Message}";
            IsComplete = true;
            HasErrors = true;
        }
        finally
        {
            IsImporting = false;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }

    [RelayCommand]
    private void CancelImport()
    {
        _cancellationTokenSource?.Cancel();
    }

    [RelayCommand]
    private void ToggleAllProviders()
    {
        var anyEnabled = Providers.Any(p => p.IsEnabled);
        foreach (var provider in Providers)
        {
            provider.IsEnabled = !anyEnabled;
        }
    }

    [RelayCommand]
    private void SelectOnlyProvider(PlatformProviderViewModel provider)
    {
        foreach (var p in Providers)
        {
            p.IsEnabled = p == provider;
        }
    }

    private void OnProgressChanged(ImportProgress progress)
    {
        CurrentStage = progress.Stage.ToString();
        CurrentMessage = progress.Message;
        CurrentProvider = progress.Provider;

        if (progress.Total > 0)
        {
            OverallProgress = progress.Progress * 100;
            ProgressText = $"{progress.Current}/{progress.Total} ({OverallProgress:F0}%)";
        }
        else
        {
            ProgressText = progress.Message;
        }

        // Update provider status
        if (!string.IsNullOrEmpty(progress.Provider))
        {
            var providerVm = Providers.FirstOrDefault(p => p.Name == progress.Provider);
            if (providerVm != null)
            {
                providerVm.UpdateStatus(progress.Stage == ImportStage.Discovery ? "Scanning..." : "Processing...");
            }
        }
    }
}
