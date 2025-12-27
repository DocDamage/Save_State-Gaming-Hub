using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SaveState.Core.Services;
using SaveState.Core.Services.Memory;

namespace SaveState.UI.ViewModels;

public partial class TrainerGeneratorViewModel : ViewModelBase
{
    private readonly ITrainerGeneratorService _trainerService;
    private readonly IGameSessionMonitor _monitor;

    [ObservableProperty]
    private string _searchValue = "";

    [ObservableProperty]
    private MemoryValueType _selectedType = MemoryValueType.Int;

    [ObservableProperty]
    private string _statusMessage = "Ready to scan.";

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private int _resultCount;

    [ObservableProperty]
    private string _cheatName = "New Cheat";
    
    public ObservableCollection<MemoryValueType> ValueTypes { get; } = new(Enum.GetValues<MemoryValueType>());

    public TrainerGeneratorViewModel()
    {
        _trainerService = AiServiceProvider.Instance.TrainerGeneratorService;
        _monitor = AiServiceProvider.Instance.GameSessionMonitor;
    }

    [RelayCommand]
    private async Task StartScan()
    {
        if (!_monitor.IsMonitoring)
        {
            StatusMessage = "No active game session found!";
            return;
        }

        IsScanning = true;
        StatusMessage = "Scanning...";
        
        try
        {
            var count = await _trainerService.StartScanAsync(_monitor.CurrentPid, SelectedType, SearchValue);
            ResultCount = count;
            StatusMessage = $"First scan complete. Found {count} results.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task NextScan()
    {
        IsScanning = true;
        StatusMessage = "Filtering...";
        
        try
        {
            var count = await _trainerService.NextScanAsync(SearchValue);
            ResultCount = count;
            StatusMessage = $"Next scan complete. Found {count} results.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Filter failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task SaveCheat()
    {
         if (ResultCount == 0)
         {
             StatusMessage = "No results to save!";
             return;
         }
         
         try 
         {
             var saved = await _trainerService.SaveCheatAsync(_monitor.CurrentGameId, CheatName);
             if (saved)
             {
                 StatusMessage = $"Saved cheat '{CheatName}' successfully!";
             }
             else
             {
                 StatusMessage = "Failed to save cheat.";
             }
         }
         catch (Exception ex)
         {
             StatusMessage = $"Save error: {ex.Message}";
         }
    }
    
    [RelayCommand]
    private void Reset()
    {
        _trainerService.Reset();
        ResultCount = 0;
        StatusMessage = "Reset complete.";
    }
}
