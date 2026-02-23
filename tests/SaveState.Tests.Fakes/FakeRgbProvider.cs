using SaveState.Core.RgbSync.Models;
using SaveState.Core.RgbSync.Services;

namespace SaveState.Tests.Fakes;

/// <summary>
/// Fake implementation of IRgbProvider for integration testing.
/// Simulates RGB hardware devices without requiring actual hardware.
/// </summary>
public class FakeRgbProvider : IRgbProvider
{
    private readonly List<RgbDevice> _devices = new();
    private bool _isInitialized;

    public FakeRgbProvider()
    {
        // Create some fake devices for testing
        _devices.Add(CreateFakeDevice("Fake Keyboard", RgbDeviceType.Keyboard, 104));
        _devices.Add(CreateFakeDevice("Fake Mouse", RgbDeviceType.Mouse, 4));
        _devices.Add(CreateFakeDevice("Fake Headset", RgbDeviceType.Headset, 2));
    }

    /// <inheritdoc />
    public string Id => "fake_rgb_provider";

    /// <inheritdoc />
    public string Name => "Fake RGB Provider";

    /// <inheritdoc />
    public string? Version => "1.0.0";

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken ct = default)
    {
        _isInitialized = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _isInitialized = false;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<RgbDevice>> GetDevicesAsync(CancellationToken ct = default)
    {
        return Task.FromResult<IReadOnlyList<RgbDevice>>(_devices.ToList());
    }

    /// <inheritdoc />
    public Task SetDeviceColorAsync(RgbDevice device, RgbColor color, CancellationToken ct = default)
    {
        var existingDevice = _devices.FirstOrDefault(d => d.Id == device.Id);
        if (existingDevice != null)
        {
            foreach (var led in existingDevice.Leds)
            {
                led.Color = color;
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SetDeviceEffectAsync(RgbDevice device, RgbEffect effect, CancellationToken ct = default)
    {
        // Simulate applying effect - in a real implementation this would update device state
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UpdateDeviceAsync(RgbDevice device, CancellationToken ct = default)
    {
        var existingDevice = _devices.FirstOrDefault(d => d.Id == device.Id);
        if (existingDevice != null)
        {
            var index = _devices.IndexOf(existingDevice);
            _devices[index] = device;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Adds a fake device to the provider.
    /// </summary>
    public void AddDevice(RgbDevice device)
    {
        _devices.Add(device);
    }

    /// <summary>
    /// Removes a device from the provider.
    /// </summary>
    public void RemoveDevice(Guid deviceId)
    {
        _devices.RemoveAll(d => d.Id == deviceId);
    }

    /// <summary>
    /// Clears all devices from the provider.
    /// </summary>
    public void ClearDevices()
    {
        _devices.Clear();
    }

    private static RgbDevice CreateFakeDevice(string name, RgbDeviceType type, int ledCount)
    {
        return new RgbDevice
        {
            Id = Guid.NewGuid(),
            Name = name,
            Vendor = "Fake Vendor",
            Type = type,
            LedCount = ledCount,
            Leds = Enumerable.Range(0, ledCount)
                .Select(i => new RgbLed { Index = i, Color = RgbColor.White, Brightness = 1.0f })
                .ToList(),
            IsConnected = true,
            SupportsDirectMode = true,
            ProviderId = "fake_rgb_provider"
        };
    }
}
