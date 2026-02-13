using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Microsoft.Extensions.Logging;
using SaveState.Core.Performance.Services;
using SaveState.Core.Performance.ValueObjects;
using SaveState.Application.Performance.Commands;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the memory scanner UI.
/// </summary>
public partial class MemoryScannerViewModel : ObservableObject
{
    private readonly IMemoryScanner _scanner;
    private readonly IMediator _mediator;
    private readonly ILogger<MemoryScannerViewModel> _logger;

    [ObservableProperty]
    private Guid _gameId;

    [ObservableProperty]
    private int _processId;

    [ObservableProperty]
    private MemoryDataType _selectedDataType = MemoryDataType.Int32;

    [ObservableProperty]
    private ScanType _selectedScanType = ScanType.ExactValue;

    [ObservableProperty]
    private string _searchValue = "0";

    [ObservableProperty]
    private int _resultCount;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _canNarrowDown;

    public ObservableCollection<ScanResultItemViewModel> Results { get; } = new();

    public IEnumerable<MemoryDataType> AvailableDataTypes => Enum.GetValues<MemoryDataType>();
    public IEnumerable<ScanType> AvailableScanTypes => Enum.GetValues<ScanType>();

    public MemoryScannerViewModel(
        IMemoryScanner scanner,
        IMediator mediator,
        ILogger<MemoryScannerViewModel> logger)
    {
        _scanner = scanner;
        _mediator = mediator;
        _logger = logger;
    }

    [RelayCommand]
    private async Task StartNewScanAsync()
    {
        if (IsScanning) return;

        IsScanning = true;
        try
        {
            object value = ParseValue(SearchValue, SelectedDataType);
            var result = await _scanner.StartNewScanAsync(ProcessId, SelectedDataType, SelectedScanType, value);

            if (result.IsSuccess)
            {
                ResultCount = result.Value;
                CanNarrowDown = true;
                await UpdateResultsListAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start memory scan");
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    private async Task NextScanAsync()
    {
        if (IsScanning || !CanNarrowDown) return;

        IsScanning = true;
        try
        {
            object value = ParseValue(SearchValue, SelectedDataType);
            var result = await _scanner.NextScanAsync(ProcessId, SelectedScanType, value);

            if (result.IsSuccess)
            {
                ResultCount = result.Value;
                await UpdateResultsListAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to perform next scan");
        }
        finally
        {
            IsScanning = false;
        }
    }

    private async Task UpdateResultsListAsync()
    {
        var result = await _scanner.GetResultsAsync(0, 50); // Show first 50 results
        if (result.IsSuccess)
        {
            Results.Clear();
            foreach (var addr in result.Value)
            {
                Results.Add(new ScanResultItemViewModel(addr, SelectedDataType.ToString(), _mediator, GameId, ProcessId));
            }
        }
    }

    private object ParseValue(string value, MemoryDataType type)
    {
        // For MVP, just simple parsing
        return type switch
        {
            MemoryDataType.Int32 => int.Parse(value),
            MemoryDataType.Float => float.Parse(value),
            _ => (object)value
        };
    }
}

public partial class ScanResultItemViewModel : ObservableObject
{
    [ObservableProperty]
    private long _address;

    [ObservableProperty]
    private string _addressHex;

    [ObservableProperty]
    private string _type;

    [ObservableProperty]
    private string _value = "---";

    private readonly Guid _gameId;
    private readonly int _processId;
    private readonly IMediator _mediator;

    public ScanResultItemViewModel(long address, string type, IMediator mediator, Guid gameId, int processId)
    {
        Address = address;
        AddressHex = $"0x{address:X8}";
        Type = type;
        _mediator = mediator;
        _gameId = gameId;
        _processId = processId;
    }

    [RelayCommand]
    private async Task AddToWatchAsync()
    {
        if (_mediator == null) return;

        var command = new AddMemoryWatchCommand(
            _gameId,
            $"Scanner Result {AddressHex}",
            Address,
            Enum.Parse<MemoryDataType>(Type));

        await _mediator.Send(command);
    }
}
