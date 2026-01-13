using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MediatR;
using SaveState.Application.Performance.Commands;
using SaveState.Application.Performance.Queries;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Presentation.ViewModels.Overlays;

/// <summary>
/// ViewModel for the memory monitor overlay with real-time monitoring.
/// </summary>
public partial class MemoryMonitorOverlayViewModel : ObservableObject, IDisposable
{
    private readonly IMediator? _mediator;
    private readonly ILogger<MemoryMonitorOverlayViewModel>? _logger;
    private CancellationTokenSource? _monitoringCts;
    private Task? _monitoringTask;

    [ObservableProperty]
    private string _title = "Memory Monitor";

    [ObservableProperty]
    private bool _isVisible = true;

    [ObservableProperty]
    private bool _isMonitoring;

    [ObservableProperty]
    private Guid _gameId;

    [ObservableProperty]
    private int _processId;

    [ObservableProperty]
    private ObservableCollection<MemoryAddressViewModel> _watchedAddresses = new();

    // Design-time constructor
    public MemoryMonitorOverlayViewModel()
    {
        // Sample data for designer
        WatchedAddresses.Add(new MemoryAddressViewModel(Guid.NewGuid(), "HP", "0x00FF3420", "100", "Int32", null, null));
        WatchedAddresses.Add(new MemoryAddressViewModel(Guid.NewGuid(), "MP", "0x00FF3424", "50", "Int32", null, null));
        WatchedAddresses.Add(new MemoryAddressViewModel(Guid.NewGuid(), "Lives", "0x00FF3428", "3", "UInt8", null, null));
    }

    // Runtime constructor
    public MemoryMonitorOverlayViewModel(IMediator mediator, ILogger<MemoryMonitorOverlayViewModel> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Initializes monitoring for a specific game.
    /// </summary>
    public async Task InitializeAsync(Guid gameId, int processId, string gameTitle)
    {
        GameId = gameId;
        ProcessId = processId;
        Title = $"Memory Monitor - {gameTitle}";

        await LoadWatchesAsync();
        StartMonitoring();
    }

    /// <summary>
    /// Loads existing watches from the database.
    /// </summary>
    private async Task LoadWatchesAsync()
    {
        if (_mediator == null) return;

        try
        {
            var query = new GetMemoryWatchesQuery(GameId);
            var result = await _mediator.Send(query);

            if (result.IsSuccess && result.Value != null)
            {
                WatchedAddresses.Clear();
                foreach (var watch in result.Value)
                {
                    var vm = new MemoryAddressViewModel(
                        watch.Id,
                        watch.Label,
                        watch.Address.ToHexString(),
                        watch.CurrentValue ?? "---",
                        watch.DataType.ToString(),
                        _mediator,
                        ProcessId)
                    {
                        IsFrozen = watch.IsFrozen
                    };
                    WatchedAddresses.Add(vm);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to load memory watches");
        }
    }

    /// <summary>
    /// Starts real-time monitoring loop.
    /// </summary>
    private void StartMonitoring()
    {
        if (IsMonitoring || _mediator == null) return;

        IsMonitoring = true;
        _monitoringCts = new CancellationTokenSource();

        _monitoringTask = Task.Run(async () =>
        {
            while (!_monitoringCts.Token.IsCancellationRequested)
            {
                try
                {
                    var command = new UpdateMemoryWatchesCommand(GameId, ProcessId);
                    var result = await _mediator.Send(command, _monitoringCts.Token);

                    if (result.IsSuccess)
                    {
                        // Refresh UI values
                        var query = new GetMemoryWatchesQuery(GameId);
                        var watchesResult = await _mediator.Send(query, _monitoringCts.Token);

                        if (watchesResult.IsSuccess)
                        {
                            foreach (var watch in watchesResult.Value!)
                            {
                                var vm = WatchedAddresses.FirstOrDefault(w => w.Id == watch.Id);
                                if (vm != null)
                                {
                                    vm.UpdateValue(watch.CurrentValue ?? "---");
                                }
                            }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error in memory monitoring loop");
                }

                await Task.Delay(500, _monitoringCts.Token);
            }
        }, _monitoringCts.Token);
    }

    /// <summary>
    /// Stops monitoring.
    /// </summary>
    private void StopMonitoring()
    {
        if (!IsMonitoring) return;

        IsMonitoring = false;
        _monitoringCts?.Cancel();
    }

    [RelayCommand]
    private void Close()
    {
        StopMonitoring();
        IsVisible = false;
    }

    [RelayCommand]
    private async Task AddAddressAsync()
    {
        if (_mediator == null)
        {
            WatchedAddresses.Add(new MemoryAddressViewModel(Guid.NewGuid(), "New Address", "0x00000000", "0", "Int32", null, null));
            return;
        }

        // Placeholder for new watch addition (Phase 8A logic)
        var command = new AddMemoryWatchCommand(GameId, "New Watch", 0, MemoryDataType.Int32);
        await _mediator.Send(command);
        await LoadWatchesAsync();
    }

    public void Dispose()
    {
        StopMonitoring();
        _monitoringCts?.Dispose();
    }
}

/// <summary>
/// ViewModel for a single memory address watch.
/// </summary>
public partial class MemoryAddressViewModel : ObservableObject
{
    private readonly IMediator? _mediator;
    private readonly int _processId;

    [ObservableProperty]
    private Guid _id;

    [ObservableProperty]
    private string _label;

    [ObservableProperty]
    private string _address;

    [ObservableProperty]
    private string _value;

    [ObservableProperty]
    private string _type;

    [ObservableProperty]
    private bool _isFrozen;

    [ObservableProperty]
    private bool _hasChanged;

    public MemoryAddressViewModel(Guid id, string label, string address, string value, string type, IMediator? mediator, int? processId)
    {
        Id = id;
        Label = label;
        Address = address;
        Value = value;
        Type = type;
        _mediator = mediator;
        _processId = processId ?? 0;
    }

    public void UpdateValue(string newValue)
    {
        if (Value != newValue)
        {
            Value = newValue;
            HasChanged = true;
            Task.Delay(1000).ContinueWith(_ => HasChanged = false);
        }
    }

    [RelayCommand]
    private async Task ToggleFreezeAsync()
    {
        IsFrozen = !IsFrozen;
        if (_mediator != null)
        {
            var command = new ModifyMemoryWatchCommand(Id, IsFrozen: IsFrozen);
            await _mediator.Send(command);
        }
    }

    [RelayCommand]
    private async Task WriteValueAsync(string newValue)
    {
        if (_mediator != null)
        {
            var command = new WriteMemoryValueCommand(Id, _processId, newValue);
            await _mediator.Send(command);
        }
    }
}

