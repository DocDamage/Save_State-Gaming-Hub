using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;

namespace SaveState.Infrastructure.RgbSync.Providers;

public class OpenRgbProvider : IRgbProvider, IDisposable
{
    private readonly ILogger<OpenRgbProvider> _logger;
    private TcpClient? _client;
    private NetworkStream? _stream;
    private readonly List<RgbDevice> _devices = new();
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _isInitialized;
    
    public string Id => "openrgb";
    public string Name => "OpenRGB";
    public string? Version => "1.0.0";
    public bool IsAvailable => _isInitialized && _client?.Connected == true;
    
    public OpenRgbProvider(ILogger<OpenRgbProvider> logger)
    {
        _logger = logger;
    }
    
    public async Task InitializeAsync(CancellationToken ct = default)
    {
        try
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            
            if (_isInitialized)
                return;
            
            // Try to connect to OpenRGB server (default port 6742)
            _client = new TcpClient();
            await _client.ConnectAsync("127.0.0.1", 6742, ct).ConfigureAwait(false);
            _stream = _client.GetStream();
            
            // Send client name
            await SendCommandAsync(0, "SaveStateReborn", ct).ConfigureAwait(false);
            
            _isInitialized = true;
            _logger.LogInformation("OpenRGB provider initialized successfully");
            
            // Refresh device list
            await RefreshDevicesAsync(ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to initialize OpenRGB provider. Server may not be running.");
            _client?.Dispose();
            _client = null;
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task ShutdownAsync(CancellationToken ct = default)
    {
        try
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            
            if (!_isInitialized)
                return;
            
            _stream?.Close();
            _client?.Close();
            _devices.Clear();
            _isInitialized = false;
            
            _logger.LogInformation("OpenRGB provider shut down");
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
            throw new InvalidOperationException("OpenRGB provider is not available");
        
        try
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            
            var deviceIndex = _devices.IndexOf(device);
            if (deviceIndex < 0)
                throw new ArgumentException("Device not found", nameof(device));
            
            // Build color data for all LEDs
            var colors = new byte[device.LedCount * 4];
            for (int i = 0; i < device.LedCount; i++)
            {
                colors[i * 4] = color.R;
                colors[i * 4 + 1] = color.G;
                colors[i * 4 + 2] = color.B;
                colors[i * 4 + 3] = 0; // Padding
            }
            
            await SendCommandAsync(1050, deviceIndex, colors, ct).ConfigureAwait(false);
            
            device.Leds.ForEach(led => led.Color = color);
            
            _logger.LogDebug("Set device {DeviceName} color to {Color}", device.Name, color.ToHex());
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public async Task SetDeviceEffectAsync(RgbDevice device, RgbEffect effect, CancellationToken ct = default)
    {
        // OpenRGB uses direct mode for effects - map to color or use OpenRGB's effect system
        if (effect.Type == RgbEffectType.Static && effect.Colors.Count > 0)
        {
            await SetDeviceColorAsync(device, effect.Colors[0], ct).ConfigureAwait(false);
        }
        else
        {
            _logger.LogDebug("Effect type {EffectType} mapped to direct mode for OpenRGB", effect.Type);
            if (effect.Colors.Count > 0)
            {
                await SetDeviceColorAsync(device, effect.Colors[0], ct).ConfigureAwait(false);
            }
        }
    }
    
    public Task UpdateDeviceAsync(RgbDevice device, CancellationToken ct = default)
    {
        // OpenRGB doesn't support live device updates
        return Task.CompletedTask;
    }
    
    private async Task RefreshDevicesAsync(CancellationToken ct)
    {
        if (!IsAvailable)
            return;
        
        try
        {
            _devices.Clear();
            
            // Request controller count
            await SendCommandAsync(0, ct: ct).ConfigureAwait(false);
            var countData = await ReadResponseAsync(ct).ConfigureAwait(false);
            
            if (countData.Length < 4)
                return;
            
            int deviceCount = BitConverter.ToInt32(countData, 0);
            
            for (int i = 0; i < deviceCount; i++)
            {
                await SendCommandAsync(1, i, ct: ct).ConfigureAwait(false);
                var deviceData = await ReadResponseAsync(ct).ConfigureAwait(false);
                
                var device = ParseDeviceData(i, deviceData);
                if (device != null)
                {
                    device.ProviderId = Id;
                    _devices.Add(device);
                }
            }
            
            _logger.LogInformation("Discovered {Count} OpenRGB devices", _devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh OpenRGB devices");
        }
    }
    
    private async Task SendCommandAsync(int command, int deviceId = 0, byte[]? data = null, CancellationToken ct = default)
    {
        if (_stream == null)
            throw new InvalidOperationException("Not connected to OpenRGB server");
        
        data ??= Array.Empty<byte>();
        
        var packet = new List<byte>();
        packet.AddRange(BitConverter.GetBytes(0x4F524247)); // Magic: "ORGB"
        packet.AddRange(BitConverter.GetBytes(deviceId));
        packet.AddRange(BitConverter.GetBytes(command));
        packet.AddRange(BitConverter.GetBytes(data.Length));
        packet.AddRange(data);
        
        await _stream.WriteAsync(packet.ToArray(), ct).ConfigureAwait(false);
    }
    
    private async Task SendCommandAsync(int command, string message, CancellationToken ct)
    {
        var data = Encoding.UTF8.GetBytes(message);
        await SendCommandAsync(command, 0, data, ct).ConfigureAwait(false);
    }
    
    private async Task<byte[]> ReadResponseAsync(CancellationToken ct)
    {
        if (_stream == null)
            return Array.Empty<byte>();
        
        var header = new byte[16];
        await _stream.ReadExactlyAsync(header, ct).ConfigureAwait(false);
        
        int dataLength = BitConverter.ToInt32(header, 12);
        
        if (dataLength == 0)
            return Array.Empty<byte>();
        
        var data = new byte[dataLength];
        await _stream.ReadExactlyAsync(data, ct).ConfigureAwait(false);
        
        return data;
    }
    
    private RgbDevice? ParseDeviceData(int index, byte[] data)
    {
        try
        {
            // Simplified parsing - real implementation would parse full protocol
            var device = new RgbDevice
            {
                Id = Guid.NewGuid(),
                Name = $"OpenRGB Device {index}",
                Vendor = "OpenRGB",
                Type = RgbDeviceType.LedStrip,
                LedCount = 1,
                Leds = new List<RgbLed> { new() { Index = 0, Name = "LED 0" } },
                IsConnected = true,
                SupportsDirectMode = true
            };
            
            return device;
        }
        catch
        {
            return null;
        }
    }
    
    public void Dispose()
    {
        _client?.Dispose();
        _stream?.Dispose();
        _lock.Dispose();
    }
}
