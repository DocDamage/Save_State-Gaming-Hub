using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;

namespace SaveState.Infrastructure.RgbSync.Providers;

public class RazerChromaProvider : IRgbProvider, IDisposable
{
    private readonly ILogger<RazerChromaProvider> _logger;
    private readonly List<RgbDevice> _devices = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isInitialized;
    private IntPtr _chromaSdkHandle;
    
    public string Id => "razerchroma";
    public string Name => "Razer Chroma";
    public string? Version => "3.0";
    public bool IsAvailable => _isInitialized && RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    
    // Chroma SDK constants
    private const int RZRESULT_SUCCESS = 0;
    private const int MAX_LEDS = 150;
    
    public RazerChromaProvider(ILogger<RazerChromaProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogInformation("Razer Chroma is only available on Windows");
            return;
        }
        
        try
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            
            if (_isInitialized)
                return;
            
            // Initialize Chroma SDK
            var result = await Task.Run(() => NativeMethods.Init(), ct).ConfigureAwait(false);
            
            if (result != RZRESULT_SUCCESS)
            {
                _logger.LogWarning("Failed to initialize Razer Chroma SDK. Error: {Error}", result);
                return;
            }
            
            _isInitialized = true;
            _logger.LogInformation("Razer Chroma provider initialized successfully");
            
            // Discover devices
            await DiscoverDevicesAsync(ct).ConfigureAwait(false);
        }
        catch (DllNotFoundException)
        {
            _logger.LogWarning("Razer Chroma SDK not found. Please install Razer Synapse.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Razer Chroma provider");
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
            
            // Reset all devices to default
            await Task.Run(NativeMethods.ResetEffects, ct).ConfigureAwait(false);
            
            // Uninitialize SDK
            await Task.Run(NativeMethods.UnInit, ct).ConfigureAwait(false);
            
            _devices.Clear();
            _isInitialized = false;
            
            _logger.LogInformation("Razer Chroma provider shut down");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during Razer Chroma shutdown");
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
            throw new InvalidOperationException("Razer Chroma provider is not available");
        
        try
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            
            var razerColor = (color.R << 16) | (color.G << 8) | color.B;
            
            await Task.Run(() =>
            {
                switch (device.Type)
                {
                    case RgbDeviceType.Keyboard:
                        NativeMethods.CreateKeyboardEffect(0, razerColor, IntPtr.Zero);
                        break;
                    case RgbDeviceType.Mouse:
                        NativeMethods.CreateMouseEffect(0, razerColor, IntPtr.Zero);
                        break;
                    case RgbDeviceType.Headset:
                        NativeMethods.CreateHeadsetEffect(0, razerColor, IntPtr.Zero);
                        break;
                    case RgbDeviceType.Mousepad:
                        NativeMethods.CreateMousepadEffect(0, razerColor, IntPtr.Zero);
                        break;
                    case RgbDeviceType.Keypad:
                        NativeMethods.CreateKeypadEffect(0, razerColor, IntPtr.Zero);
                        break;
                    default:
                        // Use generic chroma link for other devices
                        NativeMethods.CreateChromaLinkEffect(0, razerColor, IntPtr.Zero);
                        break;
                }
            }, ct).ConfigureAwait(false);
            
            device.Leds.ForEach(led => led.Color = color);
            
            _logger.LogDebug("Set Razer device {DeviceName} color to {Color}", device.Name, color.ToHex());
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
        
        // Map effect types to Chroma SDK effects
        var razerEffect = MapEffectType(effect.Type);
        
        if (effect.Colors.Count > 0)
        {
            var color = effect.Colors[0];
            var razerColor = (color.R << 16) | (color.G << 8) | color.B;
            
            await Task.Run(() =>
            {
                switch (device.Type)
                {
                    case RgbDeviceType.Keyboard:
                        NativeMethods.CreateKeyboardEffect(razerEffect, razerColor, IntPtr.Zero);
                        break;
                    case RgbDeviceType.Mouse:
                        NativeMethods.CreateMouseEffect(razerEffect, razerColor, IntPtr.Zero);
                        break;
                    case RgbDeviceType.Headset:
                        NativeMethods.CreateHeadsetEffect(razerEffect, razerColor, IntPtr.Zero);
                        break;
                }
            }, ct).ConfigureAwait(false);
        }
    }
    
    public Task UpdateDeviceAsync(RgbDevice device, CancellationToken ct = default)
    {
        // Chroma SDK doesn't support live device updates
        return Task.CompletedTask;
    }
    
    private async Task DiscoverDevicesAsync(CancellationToken ct)
    {
        if (!IsAvailable)
            return;
        
        try
        {
            _devices.Clear();
            
            // Query connected devices
            var deviceTypes = new[]
            {
                (RgbDeviceType.Keyboard, 1),
                (RgbDeviceType.Mouse, 2),
                (RgbDeviceType.Headset, 3),
                (RgbDeviceType.Mousepad, 4),
                (RgbDeviceType.Keypad, 5),
                (RgbDeviceType.HeadsetStand, 6)
            };
            
            foreach (var (type, id) in deviceTypes)
            {
                var connected = await Task.Run(() => NativeMethods.QueryDevice(id), ct).ConfigureAwait(false);
                
                if (connected)
                {
                    _devices.Add(new RgbDevice
                    {
                        Id = Guid.NewGuid(),
                        Name = $"Razer {type}",
                        Vendor = "Razer",
                        Type = type,
                        LedCount = GetLedCountForDeviceType(type),
                        Leds = Enumerable.Range(0, GetLedCountForDeviceType(type))
                            .Select(i => new RgbLed { Index = i, Name = $"LED {i}" })
                            .ToList(),
                        IsConnected = true,
                        SupportsDirectMode = true,
                        ProviderId = Id
                    });
                }
            }
            
            _logger.LogInformation("Discovered {Count} Razer Chroma devices", _devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to discover Razer Chroma devices");
        }
    }
    
    private static int GetLedCountForDeviceType(RgbDeviceType type)
    {
        return type switch
        {
            RgbDeviceType.Keyboard => 110,
            RgbDeviceType.Mouse => 14,
            RgbDeviceType.Headset => 2,
            RgbDeviceType.Mousepad => 15,
            RgbDeviceType.Keypad => 20,
            RgbDeviceType.HeadsetStand => 8,
            _ => 1
        };
    }
    
    private static int MapEffectType(RgbEffectType type)
    {
        return type switch
        {
            RgbEffectType.Static => 0,
            RgbEffectType.Breathing => 1,
            RgbEffectType.ColorCycle => 2,
            RgbEffectType.Wave => 3,
            RgbEffectType.Rainbow => 4,
            RgbEffectType.Reactive => 5,
            _ => 0
        };
    }
    
    public void Dispose()
    {
        _lock.Dispose();
    }
    
    private static class NativeMethods
    {
        private const string ChromaSDKDll = "RzChromaSDK64.dll";
        
        [DllImport(ChromaSDKDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int Init();
        
        [DllImport(ChromaSDKDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int UnInit();
        
        [DllImport(ChromaSDKDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateKeyboardEffect(int effectType, int color, IntPtr param);
        
        [DllImport(ChromaSDKDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateMouseEffect(int effectType, int color, IntPtr param);
        
        [DllImport(ChromaSDKDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateHeadsetEffect(int effectType, int color, IntPtr param);
        
        [DllImport(ChromaSDKDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateMousepadEffect(int effectType, int color, IntPtr param);
        
        [DllImport(ChromaSDKDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateKeypadEffect(int effectType, int color, IntPtr param);
        
        [DllImport(ChromaSDKDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int CreateChromaLinkEffect(int effectType, int color, IntPtr param);
        
        [DllImport(ChromaSDKDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern int ResetEffects();
        
        [DllImport(ChromaSDKDll, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool QueryDevice(int deviceType);
    }
}
