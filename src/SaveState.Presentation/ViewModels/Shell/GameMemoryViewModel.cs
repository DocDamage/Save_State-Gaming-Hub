using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;
using SaveState.Infrastructure.GameLibrary.Services;
using SaveState.Presentation.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SaveState.Presentation.ViewModels.Shell;

/// <summary>
/// View model for Game Memory Intelligence interface.
/// </summary>
public partial class GameMemoryViewModel : ObservableObject, IDisposable
{
    private readonly IGameMemoryReader _memoryReader;
    private readonly MemoryPatternDatabase _patternDatabase;
    private readonly ILogger<GameMemoryViewModel> _logger;
    private readonly ITimeProvider _timeProvider;
    private System.Timers.Timer? _refreshTimer;

    [ObservableProperty]
    private bool _isAttached;

    [ObservableProperty]
    private string _currentProcessName = "Not Attached";

    [ObservableProperty]
    private string _currentGameState = "Unknown";

    [ObservableProperty]
    private string _pid = "-";

    [ObservableProperty]
    private ObservableCollection<MemoryPatternDisplay> _detectedPatterns = new();

    [ObservableProperty]
    private ObservableCollection<string> _scanLog = new();

    private readonly IDialogService _dialogService;
    private readonly IOverlayService _overlayService;

    [ObservableProperty]
    private int _selectedTabIndex;

    // Cheat Engine / Address Tracking
    [ObservableProperty]
    private ObservableCollection<WatchedAddressViewModel> _watchedAddresses = new();

    // Debugger / Hex View
    [ObservableProperty]
    private string _currentHexAddress = "0x00000000";

    [ObservableProperty]
    private ObservableCollection<HexRowViewModel> _hexViewRows = new();

    public GameMemoryViewModel(
        IGameMemoryReader memoryReader,
        MemoryPatternDatabase patternDatabase,
        IDialogService dialogService,
        IOverlayService overlayService,
        ILogger<GameMemoryViewModel> logger,
        ITimeProvider timeProvider)
    {
        _memoryReader = memoryReader;
        _patternDatabase = patternDatabase;
        _dialogService = dialogService;
        _overlayService = overlayService;
        _logger = logger;
        _timeProvider = timeProvider;

        _memoryReader.StateChanged += OnGameStateChanged;

        _refreshTimer = new System.Timers.Timer(1000);
        _refreshTimer.Elapsed += (s, e) => {
            CheckStatus();
            UpdateWatchedAddresses();
        };
        _refreshTimer.Start();

        // Initialize some dummy hex data for UI visualization
        RefreshHexView();
    }

    private void UpdateWatchedAddresses()
    {
        // Simulate value updates
        foreach (var address in WatchedAddresses)
        {
            if (address.IsFrozen)
            {
                // In real implementation: Write memory
            }
            else
            {
                // In real implementation: Read memory
                // address.Value = _memoryReader.Read(address.Address);
            }
        }
    }

    [RelayCommand]
    private async Task AddAddressAsync()
    {
        // Simple dialog to add address
        // In a real app, use a dedicated dialog viewModel
        var result = await _dialogService.ShowInputDialogAsync("Add Address", "Enter memory address (hex):", "0x");
        if (!string.IsNullOrWhiteSpace(result))
        {
             WatchedAddresses.Add(new WatchedAddressViewModel
             {
                 Address = result,
                 Label = "New Address",
                 Type = "Bytes",
                 Value = "00"
             });
             AddToLog($"Added watch for {result}");
        }
    }

    [RelayCommand]
    private void RemoveAddress(WatchedAddressViewModel address)
    {
        if (address != null && WatchedAddresses.Contains(address))
        {
            WatchedAddresses.Remove(address);
            AddToLog($"Removed watch for {address.Address}");
        }
    }

    [RelayCommand]
    private void RefreshHexView()
    {
        // Mock data for the debugger view
        HexViewRows.Clear();
        var startAddr = 0x00400000; // Example base
        var random = new Random();

        for (int i = 0; i < 16; i++)
        {
            var rowAddr = startAddr + (i * 16);
            var bytes = new byte[16];
            random.NextBytes(bytes);

            var hexString = BitConverter.ToString(bytes).Replace("-", " ");
            var ascii = new string(bytes.Select(b => b >= 32 && b <= 126 ? (char)b : '.').ToArray());

            HexViewRows.Add(new HexRowViewModel
            {
                AddressOffset = $"0x{rowAddr:X8}",
                HexBytes = hexString,
                Ascii = ascii
            });
        }
    }

    public partial class WatchedAddressViewModel : ObservableObject
    {
        [ObservableProperty] private string _address = "";
        [ObservableProperty] private string _label = "";
        [ObservableProperty] private string _type = "";
        [ObservableProperty] private string _value = "";
        [ObservableProperty] private bool _isFrozen;
    }

    public class HexRowViewModel
    {
        public string AddressOffset { get; set; } = "";
        public string HexBytes { get; set; } = "";
        public string Ascii { get; set; } = "";
    }

    private void CheckStatus()
    {
        if (_memoryReader.IsAttached != IsAttached)
        {
            IsAttached = _memoryReader.IsAttached;
            CurrentProcessName = IsAttached ? "Attached to Game" : "Not Attached"; // Simplified, ideally Reader exposes Process name
        }
    }

    private void OnGameStateChanged(object? sender, GameStateChangedEventArgs e)
    {
        CurrentGameState = e.StateType.ToString();
        IsAttached = _memoryReader.IsAttached;

        // Log the state change
        AddToLog($"State changed to: {e.StateType}");
    }

    [RelayCommand]
    private async Task AttachToProcessAsync()
    {
        // For UI, we might default to scanning known games
        AddToLog("Scanning for known games...");

        // In a real scenario, we might pass a specific process ID/Name
        // But the IGameMemoryReader implementation has internal logic to find a game
        // We'll trust its auto-detection for now or pass a wrapper.
        // Looking at GameMemoryReader.cs (from previous session), AttachToProcessAsync takes a process
        // or we use a separate method. The interface has AttachToProcessAsync(int processId).

        // Let's assume we trigger an auto-scan logic here or ask user for PID.
        // For simplicity in this tool, we'll try to attach to a "mock" or common game if running,
        // or just simulate for the UI if no game found.

        // Actually, let's use the patterns to find a process.
        // Since IGameMemoryReader requires a Process, we'd need a ProcessSelector service or similar.
        // For now, let's just log.
        AddToLog("Auto-attach not fully implemented in UI. Please use CLI 'memory attach' or implement process selector.");
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task DetachAsync()
    {
        await _memoryReader.DetachAsync();
        IsAttached = false;
        CurrentProcessName = "Not Attached";
        AddToLog("Detached.");
    }

    [RelayCommand]
    private async Task ScanPatternsAsync()
    {
        if (!IsAttached)
        {
            AddToLog("Cannot scan: Not attached to any process.");
            return;
        }

        AddToLog("Scanning memory patterns...");
        var result = await _memoryReader.DetectPatternsAsync();

        if (!result.IsSuccess)
        {
             AddToLog($"Scan failed: {result.Error}");
             return;
        }

        if (!result.IsSuccess || result.Value is null)
            return;

        var patterns = result.Value;

        DetectedPatterns.Clear();
        foreach (var p in patterns)
        {
            DetectedPatterns.Add(new MemoryPatternDisplay(p.Name, p.CurrentValue?.ToString() ?? "null", p.Address.ToString("X")));
        }
        AddToLog($"Scan complete. Found {patterns.Count} patterns.");
    }

    [RelayCommand]
    private void ClearLog()
    {
        ScanLog.Clear();
    }

    private void AddToLog(string message)
    {
        // Invoke on UI thread normally, but ObservableCollection usually handles it if bound correctly in Avalonia 11+
        // Safe bet is to lock or use dispatcher if needed. For now simple add.
        ScanLog.Insert(0, $"[{_timeProvider.Now:HH:mm:ss}] {message}");
        if (ScanLog.Count > 100) ScanLog.RemoveAt(ScanLog.Count - 1);
    }

    public void Dispose()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _memoryReader.StateChanged -= OnGameStateChanged;
    }
}

public class MemoryPatternDisplay
{
    public MemoryPatternDisplay(string name, string value, string address)
    {
        Name = name;
        Value = value;
        Address = address;
    }
    public string Name { get; }
    public string Value { get; }
    public string Address { get; }
}
