using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Services;

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
    private readonly IMemoryPatternDatabase _patternDatabase;
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
    private string _currentHexAddress = "0x00400000";

    [ObservableProperty]
    private ObservableCollection<HexRowViewModel> _hexViewRows = new();

    private const int HexViewPageSize = 256; // 16 rows x 16 bytes

    public GameMemoryViewModel(
        IGameMemoryReader memoryReader,
        IMemoryPatternDatabase patternDatabase,
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
        // Update values for watched addresses that are not frozen
        // (Frozen values are handled by the memory reader's freeze loop)
        foreach (var address in WatchedAddresses)
        {
            if (!address.IsFrozen)
            {
                // In real implementation: Read memory
                // address.Value = _memoryReader.Read(address.Address);
            }
            // If frozen, the memory reader's freeze loop handles continuous writing
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
            // Unfreeze before removing
            if (address.IsFrozen)
            {
                _ = ToggleFreezeAsync(address);
            }

            WatchedAddresses.Remove(address);
            AddToLog($"Removed watch for {address.Address}");
        }
    }

    [RelayCommand]
    private async Task ToggleFreezeAsync(WatchedAddressViewModel address)
    {
        if (address == null)
            return;

        if (!_memoryReader.IsAttached)
        {
            AddToLog("Cannot toggle freeze: Not attached to any process.");
            address.IsFrozen = false;
            return;
        }

        // Parse the address
        if (!TryParseAddress(address.Address, out var addressPtr))
        {
            AddToLog($"Invalid address format: {address.Address}");
            address.IsFrozen = false;
            return;
        }

        // Toggle freeze state
        address.IsFrozen = !address.IsFrozen;

        if (address.IsFrozen)
        {
            // Parse the current value
            if (!TryParseValue(address.Value, address.Type, out var value))
            {
                AddToLog($"Cannot freeze: Invalid value format '{address.Value}' for type '{address.Type}'");
                address.IsFrozen = false;
                return;
            }

            // Start freezing
            var result = await _memoryReader.FreezeValueAsync(addressPtr, value);

            if (result.IsSuccess)
            {
                AddToLog($"FROZEN: {address.Label} ({address.Address}) = {address.Value}");
                _logger.LogInformation("User froze address {Address} with value {Value}", address.Address, address.Value);
            }
            else
            {
                AddToLog($"Failed to freeze {address.Address}: {result.Error}");
                address.IsFrozen = false;
            }
        }
        else
        {
            // Stop freezing
            var result = await _memoryReader.UnfreezeValueAsync(addressPtr);

            if (result.IsSuccess)
            {
                AddToLog($"UNFROZEN: {address.Label} ({address.Address})");
                _logger.LogInformation("User unfroze address {Address}", address.Address);
            }
            else
            {
                AddToLog($"Failed to unfreeze {address.Address}: {result.Error}");
                // Keep IsFrozen as false since we're trying to unfreeze
            }
        }
    }

    /// <summary>
    /// Parses a hex address string to IntPtr.
    /// </summary>
    private static bool TryParseAddress(string addressStr, out IntPtr address)
    {
        address = IntPtr.Zero;

        if (string.IsNullOrWhiteSpace(addressStr))
            return false;

        try
        {
            // Remove 0x prefix if present
            var hexString = addressStr.Trim().Replace("0x", "").Replace("0X", "");

            // Try parsing as hex
            if (long.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var longValue))
            {
                address = (IntPtr)longValue;
                return true;
            }

            // Try parsing as decimal
            if (long.TryParse(hexString, out var decimalValue))
            {
                address = (IntPtr)decimalValue;
                return true;
            }
        }
        catch
        {
            // Parsing failed
        }

        return false;
    }

    /// <summary>
    /// Parses a value string based on its type.
    /// </summary>
    private static bool TryParseValue(string valueStr, string type, out object value)
    {
        value = new object();

        if (string.IsNullOrWhiteSpace(valueStr))
            return false;

        try
        {
            var normalizedType = type.ToLowerInvariant();

            switch (normalizedType)
            {
                case "int32":
                case "int":
                case "integer":
                    if (int.TryParse(valueStr, out var intValue))
                    {
                        value = intValue;
                        return true;
                    }
                    break;

                case "float":
                case "single":
                    if (float.TryParse(valueStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                    {
                        value = floatValue;
                        return true;
                    }
                    break;

                case "bytes":
                case "byte":
                    // Default to int for bytes type when freezing
                    if (int.TryParse(valueStr, out var byteAsInt))
                    {
                        value = byteAsInt;
                        return true;
                    }
                    break;

                default:
                    // Default to int
                    if (int.TryParse(valueStr, out var defaultValue))
                    {
                        value = defaultValue;
                        return true;
                    }
                    break;
            }
        }
        catch
        {
            // Parsing failed
        }

        return false;
    }

    [RelayCommand]
    private async Task RefreshHexView()
    {
        if (!IsAttached)
        {
            // Show mock data when not attached
            ShowMockHexData();
            return;
        }

        try
        {
            // Parse current hex address
            var addressStr = CurrentHexAddress.Replace("0x", "").Replace("0X", "");
            if (!long.TryParse(addressStr, NumberStyles.HexNumber, null, out var startAddr))
            {
                // Try to get module base address as default
                var baseResult = await _memoryReader.GetModuleBaseAddressAsync(null);
                if (baseResult.IsSuccess)
                {
                    startAddr = baseResult.Value;
                }
                else
                {
                    startAddr = 0x00400000; // Fallback default base
                }
            }

            // Read 256 bytes (16 rows x 16 bytes)
            var result = await _memoryReader.ReadMemoryBytesAsync((IntPtr)startAddr, HexViewPageSize);
            if (!result.IsSuccess)
            {
                AddToLog($"Failed to read memory: {result.Error}");
                return;
            }

            var bytes = result.Value;
            HexViewRows.Clear();

            for (int i = 0; i < 16; i++)
            {
                var rowAddr = startAddr + (i * 16);
                var rowBytes = bytes.Skip(i * 16).Take(16).ToArray();

                var hexString = BitConverter.ToString(rowBytes).Replace("-", " ");
                var ascii = new string(rowBytes.Select(b => b >= 32 && b <= 126 ? (char)b : '.').ToArray());

                HexViewRows.Add(new HexRowViewModel
                {
                    AddressOffset = $"0x{rowAddr:X8}",
                    HexBytes = hexString,
                    Ascii = ascii
                });
            }

            AddToLog($"Read {bytes.Length} bytes from 0x{startAddr:X8}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing hex view");
            AddToLog($"Error: {ex.Message}");
        }
    }

    private void ShowMockHexData()
    {
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

    [RelayCommand]
    private async Task GoToAddressAsync()
    {
        var result = await _dialogService.ShowInputDialogAsync(
            "Go to Address",
            "Enter memory address (hex, e.g., 0x00400000):",
            CurrentHexAddress);

        if (!string.IsNullOrWhiteSpace(result))
        {
            CurrentHexAddress = result.Trim();
            await RefreshHexView();
        }
    }

    [RelayCommand]
    private async Task NextPageAsync()
    {
        // Parse current address and advance by page size
        var addressStr = CurrentHexAddress.Replace("0x", "").Replace("0X", "");
        if (long.TryParse(addressStr, NumberStyles.HexNumber, null, out var currentAddr))
        {
            currentAddr += HexViewPageSize;
            CurrentHexAddress = $"0x{currentAddr:X8}";
            await RefreshHexView();
        }
    }

    [RelayCommand]
    private async Task PreviousPageAsync()
    {
        // Parse current address and go back by page size
        var addressStr = CurrentHexAddress.Replace("0x", "").Replace("0X", "");
        if (long.TryParse(addressStr, NumberStyles.HexNumber, null, out var currentAddr))
        {
            currentAddr = Math.Max(0, currentAddr - HexViewPageSize);
            CurrentHexAddress = $"0x{currentAddr:X8}";
            await RefreshHexView();
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
        var processId = await _dialogService.ShowProcessSelectorAsync();
        if (processId.HasValue)
        {
            var result = await _memoryReader.AttachToProcessAsync(processId.Value);
            if (result.IsSuccess)
            {
                AddToLog($"Attached to process {processId.Value}");
            }
            else
            {
                AddToLog($"Failed to attach: {result.Error}");
            }
        }
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
