using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;

namespace SaveState.Infrastructure.RgbSync.Providers;

public class CorsairCueProvider : IRgbProvider, IDisposable
{
    private readonly ILogger<CorsairCueProvider> _logger;
    private readonly List<RgbDevice> _devices = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isInitialized;
    
    public string Id => "corsaircue";
    public string Name => "Corsair iCUE";
    public string? Version => "4.0";
    public bool IsAvailable => _isInitialized && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    // iCUE SDK constants
    private const int CorsairErrorSuccess = 0;
    private const int CorsairErrorServerNotFound = 1;
    private const int CorsairErrorNoControl = 2;
    private const int CorsairErrorProtocolHandshakeMissing = 3;
    
    public CorsairCueProvider(ILogger<CorsairCueProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogInformation("Corsair iCUE is only available on Windows");
            return;
        }
        
        try
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            
            if (_isInitialized)
                return;
            
            // Perform protocol handshake
            var handshakeResult = await Task.Run(() => NativeMethods.CorsairPerformProtocolHandshake(), ct).ConfigureAwait(false);
            
            if (handshakeResult == IntPtr.Zero)
            {
                _logger.LogWarning("Corsair iCUE protocol handshake failed. Is iCUE running?");
                return;
            }
            
            // Check for errors
            var lastError = await Task.Run(NativeMethods.CorsairGetLastError, ct).ConfigureAwait(false);
            if (lastError != CorsairErrorSuccess)
            {
                _logger.LogWarning("Corsair iCUE initialization error: {Error}", lastError);
                return;
            }
            
            _isInitialized = true;
            _logger.LogInformation("Corsair iCUE provider initialized successfully");
            
            // Discover devices
            await DiscoverDevicesAsync(ct).ConfigureAwait(false);
        }
        catch (DllNotFoundException)
        {
            _logger.LogWarning("Corsair iCUE SDK not found. Please install iCUE software.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Corsair iCUE provider");
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return;
        
        try
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            
            if (!_isInitialized)
                return;
            
            // Reset all LEDs
            await Task.Run(() => NativeMethods.CorsairRequestControl(CorsairAccessMode.ExclusiveLightingControl), ct).ConfigureAwait(false);
            
            _devices.Clear();
            _isInitialized = false;
            
            _logger.LogInformation("Corsair iCUE provider shut down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Corsair iCUE shutdown");
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task<IReadOnlyList<RgbDevice>> GetDevicesAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return _devices.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task SetDeviceColorAsync(RgbDevice device, RgbColor color, CancellationToken ct = default)
    {
        if (!IsAvailable)
            throw new InvalidOperationException("Corsair iCUE provider is not available");
        
        try
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            
            // Request exclusive control
            await Task.Run(() => NativeMethods.CorsairRequestControl(CorsairAccessMode.ExclusiveLightingControl), ct).ConfigureAwait(false);
            
            // Find device index
            var deviceIndex = _devices.IndexOf(device);
            if (deviceIndex < 0)
                throw new ArgumentException("Device not found", nameof(device));
            
            // Set all LEDs to the color
            var corsairColor = new CorsairColor { R = color.R, G = color.G, B = color.B };
            
            await Task.Run(() =>
            {
                var positions = NativeMethods.CorsairGetLedPositionsByDeviceIndex(deviceIndex);
                if (positions == IntPtr.Zero)
                    return;
                
                var ledCount = Marshal.ReadInt32(positions);
                var ledPositions = Marshal.ReadIntPtr(positions + IntPtr.Size);
                
                for (int i = 0; i < ledCount; i++)
                {
                    var ledId = Marshal.ReadInt32(ledPositions + i * 8);
                    NativeMethods.CorsairSetLedsColors(1, new[] { new CorsairLedColor { LedId = ledId, R = corsairColor.R, G = corsairColor.G, B = corsairColor.B } });
                }
            }, ct).ConfigureAwait(false);
            
            device.Leds.ForEach(led => led.Color = color);
            
            _logger.LogDebug("Set Corsair device {DeviceName} color to {Color}", device.Name, color.ToHex());
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task SetDeviceEffectAsync(RgbDevice device, RgbEffect effect, CancellationToken ct = default)
    {
        if (!IsAvailable)
            return;
        
        // iCUE doesn't support direct effect setting through SDK
        // We would need to create custom color sequences
        if (effect.Type == RgbEffectType.Static && effect.Colors.Count > 0)
        {
            await SetDeviceColorAsync(device, effect.Colors[0], ct).ConfigureAwait(false);
        }
    }
    
    public Task UpdateDeviceAsync(RgbDevice device, CancellationToken ct = default)
    {
        // iCUE SDK doesn't support live device updates
        return Task.CompletedTask;
    }
    
    private async Task DiscoverDevicesAsync(CancellationToken ct)
    {
        if (!IsAvailable)
            return;
        
        try
        {
            _devices.Clear();
            
            var deviceCount = await Task.Run(NativeMethods.CorsairGetDeviceCount, ct).ConfigureAwait(false);
            
            for (int i = 0; i < deviceCount; i++)
            {
                var deviceInfo = await Task.Run(() => NativeMethods.CorsairGetDeviceInfo(i), ct).ConfigureAwait(false);
                
                if (deviceInfo == IntPtr.Zero)
                    continue;
                
                var type = Marshal.ReadInt32(deviceInfo);
                var model = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(deviceInfo + IntPtr.Size));
                
                var rgbDevice = new RgbDevice
                {
                    Id = Guid.NewGuid(),
                    Name = model ?? $"Corsair Device {i}",
                    Vendor = "Corsair",
                    Type = MapCorsairDeviceType(type),
                    LedCount = GetLedCountForCorsairType(type),
                    Leds = Enumerable.Range(0, GetLedCountForCorsairType(type))
                        .Select(idx => new RgbLed { Index = idx, Name = $"LED {idx}" })
                        .ToList(),
                    IsConnected = true,
                    SupportsDirectMode = true,
                    ProviderId = Id
                };
                
                _devices.Add(rgbDevice);
            }
            
            _logger.LogInformation("Discovered {Count} Corsair iCUE devices", _devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover Corsair iCUE devices");
        }
    }
    
    private static RgbDeviceType MapCorsairDeviceType(int corsairType)
    {
        return corsairType switch
        {
            1 => RgbDeviceType.Keyboard,
            2 => RgbDeviceType.Mouse,
            3 => RgbDeviceType.Headset,
            4 => RgbDeviceType.Mousepad,
            5 => RgbDeviceType.Keypad,
            6 => RgbDeviceType.Cooler,
            7 => RgbDeviceType.Memory,
            8 => RgbDeviceType.Fan,
            9 => RgbDeviceType.Case,
            _ => RgbDeviceType.LedStrip
        };
    }
    
    private static int GetLedCountForCorsairType(int corsairType)
    {
        return corsairType switch
        {
            1 => 144,  // Keyboard
            2 => 4,    // Mouse
            3 => 2,    // Headset
            4 => 15,   // Mousepad
            5 => 20,   // Keypad
            6 => 16,   // Cooler
            7 => 12,   // Memory (4 sticks x 3)
            8 => 8,    // Fan
            9 => 30,   // Case
            _ => 1
        };
    }
    
    public void Dispose()
    {
        _lock.Dispose();
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct CorsairColor
    {
        public byte R;
        public byte G;
        public byte B;
    }
    
    [StructLayout(LayoutKind.Sequential)]
    private struct CorsairLedColor
    {
        public int LedId;
        public byte R;
        public byte G;
        public byte B;
    }
    
    private enum CorsairAccessMode
    {
        SharedLightingControl = 0,
        ExclusiveLightingControl = 1
    }
    
    private static class NativeMethods
    {
        private const string CorsairSdkDll = "CUESDK.x64_2017.dll";
        
        [DllImport(CorsairSdkDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CorsairPerformProtocolHandshake();
        
        [DllImport(CorsairSdkDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CorsairGetLastError();
        
        [DllImport(CorsairSdkDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CorsairGetDeviceCount();
        
        [DllImport(CorsairSdkDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CorsairGetDeviceInfo(int deviceIndex);
        
        [DllImport(CorsairSdkDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr CorsairGetLedPositionsByDeviceIndex(int deviceIndex);
        
        [DllImport(CorsairSdkDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CorsairSetLedsColors(int size, CorsairLedColor[] ledColors);
        
        [DllImport(CorsairSdkDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CorsairRequestControl(CorsairAccessMode accessMode);
    }
}
