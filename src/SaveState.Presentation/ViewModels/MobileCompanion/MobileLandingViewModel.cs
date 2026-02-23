using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using SaveState.Presentation.Models.Mobile;
using System.Collections.ObjectModel;

namespace SaveState.Presentation.ViewModels.MobileCompanion;

/// <summary>
/// ViewModel for the mobile landing/pairing screen.
/// Entry point for mobile companion app users.
/// </summary>
public partial class MobileLandingViewModel : ObservableObject
{
    private readonly ILogger<MobileLandingViewModel> _logger;
    private readonly IMobileCompanionService? _companionService;

    [ObservableProperty]
    private string _pairingCode = string.Empty;

    [ObservableProperty]
    private bool _isPairing;

    [ObservableProperty]
    private bool _isPaired;

    [ObservableProperty]
    private MobileConnectionStatus _MobileConnectionStatus = MobileConnectionStatus.Disconnected;

    [ObservableProperty]
    private MobileDevice? _pairedDevice;

    [ObservableProperty]
    private ObservableCollection<MobileDevice> _availableDevices = new();

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    public MobileLandingViewModel(
        ILogger<MobileLandingViewModel> logger,
        IMobileCompanionService? companionService = null)
    {
        _logger = logger;
        _companionService = companionService;
        _ = InitializeAsync();
    }

    /// <summary>
    /// Initializes the view model and loads any previously paired devices
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            if (_companionService is not null)
            {
                var devices = await _companionService.GetPairedDevicesAsync();
                foreach (var device in devices)
                {
                    AvailableDevices.Add(device);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize mobile landing");
        }
    }

    /// <summary>
    /// Starts the pairing process and generates a pairing code
    /// </summary>
    [RelayCommand]
    private async Task StartPairingAsync()
    {
        try
        {
            IsPairing = true;
            HasError = false;
            ErrorMessage = string.Empty;
            MobileConnectionStatus = MobileConnectionStatus.Connecting;

            _logger.LogInformation("Starting pairing process");

            if (_companionService is not null)
            {
                PairingCode = await _companionService.GeneratePairingCodeAsync();
                _ = StartPairingTimeoutAsync();
            }
            else
            {
                // Demo mode - generate random code
                PairingCode = GenerateRandomPairingCode();
                _ = StartPairingTimeoutAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start pairing");
            HasError = true;
            ErrorMessage = "Failed to start pairing. Please try again.";
            MobileConnectionStatus = MobileConnectionStatus.Error;
            IsPairing = false;
        }
    }

    /// <summary>
    /// Completes the pairing process after code verification
    /// </summary>
    [RelayCommand]
    private async Task CompletePairingAsync()
    {
        try
        {
            if (_companionService is not null)
            {
                var result = await _companionService.CompletePairingAsync(PairingCode);
                if (result.IsSuccess)
                {
                    IsPaired = true;
                    MobileConnectionStatus = MobileConnectionStatus.Paired;
                    PairedDevice = result.Value;
                    _logger.LogInformation("Successfully paired with device {DeviceName}", PairedDevice.DeviceName);
                }
                else
                {
                    HasError = true;
                    ErrorMessage = result.Error ?? "Pairing failed. Please try again.";
                    MobileConnectionStatus = MobileConnectionStatus.Error;
                }
            }
            else
            {
                // Demo mode
                IsPaired = true;
                MobileConnectionStatus = MobileConnectionStatus.Paired;
                PairedDevice = new MobileDevice
                {
                    DeviceId = Guid.NewGuid().ToString(),
                    DeviceName = "Demo Device",
                    DeviceType = "iPhone",
                    OsVersion = "iOS 17",
                    PairedAt = DateTime.Now,
                    IsOnline = true
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete pairing");
            HasError = true;
            ErrorMessage = "Pairing failed. Please check your code and try again.";
            MobileConnectionStatus = MobileConnectionStatus.Error;
        }
        finally
        {
            IsPairing = false;
        }
    }

    /// <summary>
    /// Initiates QR code scanning for pairing
    /// </summary>
    [RelayCommand]
    private async Task ScanQrCodeAsync()
    {
        try
        {
            _logger.LogInformation("Starting QR code scan");
            // FUTURE: Implement QR code scanning using platform-specific APIs
            // iOS: Vision framework, Android: CameraX when mobile native implementation begins
            await Task.Delay(100); // Placeholder
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QR code scan failed");
            HasError = true;
            ErrorMessage = "QR code scanning is not available on this device.";
        }
    }

    /// <summary>
    /// Connects to a previously paired device
    /// </summary>
    [RelayCommand]
    private async Task ConnectAsync(MobileDevice? device)
    {
        if (device is null) return;

        try
        {
            MobileConnectionStatus = MobileConnectionStatus.Connecting;
            _logger.LogInformation("Connecting to device {DeviceName}", device.DeviceName);

            if (_companionService is not null)
            {
                var result = await _companionService.ConnectAsync(device.DeviceId);
                if (result.IsSuccess)
                {
                    IsPaired = true;
                    MobileConnectionStatus = MobileConnectionStatus.Connected;
                    PairedDevice = device;
                    device.LastConnectedAt = DateTime.Now;
                    device.IsOnline = true;
                }
                else
                {
                    HasError = true;
                    ErrorMessage = "Failed to connect to device. Please ensure it's online.";
                    MobileConnectionStatus = MobileConnectionStatus.Error;
                }
            }
            else
            {
                // Demo mode
                IsPaired = true;
                MobileConnectionStatus = MobileConnectionStatus.Connected;
                PairedDevice = device;
                device.LastConnectedAt = DateTime.Now;
                device.IsOnline = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to device");
            HasError = true;
            ErrorMessage = "Connection failed. Please try again.";
            MobileConnectionStatus = MobileConnectionStatus.Error;
        }
    }

    /// <summary>
    /// Clears any error messages
    /// </summary>
    [RelayCommand]
    private void ClearError()
    {
        HasError = false;
        ErrorMessage = string.Empty;
    }

    /// <summary>
    /// Cancels the current pairing process
    /// </summary>
    [RelayCommand]
    private void CancelPairing()
    {
        IsPairing = false;
        PairingCode = string.Empty;
        MobileConnectionStatus = MobileConnectionStatus.Disconnected;
        _logger.LogInformation("Pairing cancelled by user");
    }

    /// <summary>
    /// Generates a random 6-digit pairing code
    /// </summary>
    private static string GenerateRandomPairingCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    /// <summary>
    /// Starts a timeout for the pairing process
    /// </summary>
    private async Task StartPairingTimeoutAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(5));
            if (IsPairing && !IsPaired)
            {
                IsPairing = false;
                PairingCode = string.Empty;
                MobileConnectionStatus = MobileConnectionStatus.Timeout;
                HasError = true;
                ErrorMessage = "Pairing timed out. Please try again.";
                _logger.LogWarning("Pairing timed out");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pairing timeout");
        }
    }
}

/// <summary>
/// Service interface for mobile companion functionality
/// </summary>
public interface IMobileCompanionService
{
    Task<string> GeneratePairingCodeAsync();
    Task<Core.Common.Result<MobileDevice>> CompletePairingAsync(string pairingCode);
    Task<Core.Common.Result<bool>> ConnectAsync(string deviceId);
    Task<List<MobileDevice>> GetPairedDevicesAsync();
    Task DisconnectAsync(string deviceId);
}


