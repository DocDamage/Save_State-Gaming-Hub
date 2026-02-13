using System.Runtime.InteropServices;

namespace SaveState.Infrastructure.Performance.Audio;

/// <summary>
/// Windows Core Audio API interop layer for audio device management.
/// </summary>
internal static class WindowsCoreAudio
{
    /// <summary>
    /// Sets the default audio endpoint device.
    /// </summary>
    /// <param name="deviceId">The device ID to set as default.</param>
    /// <returns>True if successful; otherwise false.</returns>
    public static bool SetDefaultAudioDevice(string deviceId)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return false;
        }

        try
        {
            // Use PolicyConfig to set default device
            var policyConfig = new PolicyConfigClient();

            // Set as default for all roles (Console, Multimedia, Communications)
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eConsole);
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eMultimedia);
            policyConfig.SetDefaultEndpoint(deviceId, ERole.eCommunications);

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets all active audio render (output) devices.
    /// </summary>
    public static List<WindowsAudioDeviceInfo> GetAudioDevices()
    {
        var devices = new List<WindowsAudioDeviceInfo>();

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return devices;
        }

        try
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            var collection = deviceEnumerator.EnumerateAudioEndPoints(EDataFlow.eRender, DeviceState.ACTIVE);

            var count = collection.GetCount();
            for (uint i = 0; i < count; i++)
            {
                var device = collection.Item(i);
                var id = device.GetId();
                var state = device.GetState();

                var propertyStore = device.OpenPropertyStore(StorageAccessMode.Read);
                var friendlyName = propertyStore.GetValue(PropertyKeys.PKEY_Device_FriendlyName);
                var description = propertyStore.GetValue(PropertyKeys.PKEY_Device_DeviceDesc);

                devices.Add(new WindowsAudioDeviceInfo
                {
                    Id = id,
                    FriendlyName = friendlyName ?? "Unknown Device",
                    Description = description ?? string.Empty,
                    State = state,
                    IsActive = state == DeviceState.ACTIVE
                });

                Marshal.ReleaseComObject(propertyStore);
                Marshal.ReleaseComObject(device);
            }

            Marshal.ReleaseComObject(collection);
            Marshal.ReleaseComObject(deviceEnumerator);
        }
        catch
        {
            // Silently fail and return empty list
        }

        return devices;
    }

    /// <summary>
    /// Gets the current default audio device ID.
    /// </summary>
    public static string? GetDefaultAudioDeviceId()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return null;
        }

        try
        {
            var deviceEnumerator = new MMDeviceEnumerator();
            var defaultDevice = deviceEnumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia);
            var id = defaultDevice.GetId();

            Marshal.ReleaseComObject(defaultDevice);
            Marshal.ReleaseComObject(deviceEnumerator);

            return id;
        }
        catch
        {
           return null;
        }
    }
}

/// <summary>
/// Information about a Windows audio device.
/// </summary>
internal record WindowsAudioDeviceInfo
{
    public required string Id { get; init; }
    public required string FriendlyName { get; init; }
    public required string Description { get; init; }
    public DeviceState State { get; init; }
    public bool IsActive { get; init; }
}

#region Core Audio COM Interfaces

[ComImport]
[Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
internal class MMDeviceEnumerator
{
}

[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceEnumerator
{
    int EnumAudioEndpoints(EDataFlow dataFlow, DeviceState stateMask, out IMMDeviceCollection devices);
    int GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice endpoint);
    int GetDevice(string id, out IMMDevice device);
    int RegisterEndpointNotificationCallback(IntPtr client);
    int UnregisterEndpointNotificationCallback(IntPtr client);
}

[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDeviceCollection
{
    int GetCount(out uint count);
    int Item(uint index, out IMMDevice device);
}

[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMMDevice
{
    int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, out IntPtr interfacePointer);
    int OpenPropertyStore(StorageAccessMode stgmAccess, out IPropertyStore properties);
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    int GetState(out DeviceState state);
}

[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPropertyStore
{
    int GetCount(out uint count);
    int GetAt(uint index, out PropertyKey key);
    int GetValue(ref PropertyKey key, out PropVariant value);
    int SetValue(ref PropertyKey key, ref PropVariant value);
    int Commit();
}

[ComImport]
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IPolicyConfig
{
    [PreserveSig]
    int GetMixFormat(string deviceId, IntPtr format);
    [PreserveSig]
    int GetDeviceFormat(string deviceId, bool isDefault, IntPtr format);
    [PreserveSig]
    int ResetDeviceFormat(string deviceId);
    [PreserveSig]
    int SetDeviceFormat(string deviceId, IntPtr format, IntPtr mixFormat);
    [PreserveSig]
    int GetProcessingPeriod(string deviceId, bool isDefault, IntPtr defaultPeriod, IntPtr minimumPeriod);
    [PreserveSig]
    int SetProcessingPeriod(string deviceId, IntPtr period);
    [PreserveSig]
    int GetShareMode(string deviceId, IntPtr mode);
    [PreserveSig]
    int SetShareMode(string deviceId, IntPtr mode);
    [PreserveSig]
    int GetPropertyValue(string deviceId, ref PropertyKey key, out PropVariant value);
    [PreserveSig]
    int SetPropertyValue(string deviceId, ref PropertyKey key, ref PropVariant value);
    [PreserveSig]
    int SetDefaultEndpoint(string deviceId, ERole role);
    [PreserveSig]
    int SetEndpointVisibility(string deviceId, bool isVisible);
}

[ComImport]
[Guid("568b9108-44bf-40b4-9006-86afe5b5a620")]
internal class PolicyConfigClient
{
}

#endregion

#region Enums and Structs

internal enum EDataFlow
{
    eRender,
    eCapture,
    eAll
}

internal enum ERole
{
    eConsole,
    eMultimedia,
    eCommunications
}

internal enum DeviceState : uint
{
    ACTIVE = 0x00000001,
    DISABLED = 0x00000002,
    NOTPRESENT = 0x00000004,
    UNPLUGGED = 0x00000008,
    MASK_ALL = 0x0000000F
}

internal enum StorageAccessMode
{
    Read = 0,
    Write = 1,
    ReadWrite = 2
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropertyKey
{
    public Guid fmtid;
    public uint pid;
}

[StructLayout(LayoutKind.Explicit)]
internal struct PropVariant
{
    [FieldOffset(0)] public ushort vt;
    [FieldOffset(8)] public IntPtr pwszVal;
    [FieldOffset(8)] public uint uintVal;
}

internal static class PropertyKeys
{
    public static readonly PropertyKey PKEY_Device_FriendlyName = new()
    {
        fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0),
        pid = 14
    };

    public static readonly PropertyKey PKEY_Device_DeviceDesc = new()
    {
        fmtid = new Guid(0xa45c254e, 0xdf1c, 0x4efd, 0x80, 0x20, 0x67, 0xd1, 0x46, 0xa8, 0x50, 0xe0),
        pid = 2
    };
}

#endregion

#region Extension Methods

internal static class MMDeviceExtensions
{
    public static IMMDeviceCollection EnumerateAudioEndPoints(this MMDeviceEnumerator enumerator, EDataFlow dataFlow, DeviceState stateMask)
    {
        var iEnumerator = (IMMDeviceEnumerator)enumerator;
        iEnumerator.EnumAudioEndpoints(dataFlow, stateMask, out var collection);
        return collection;
    }

    public static IMMDevice GetDefaultAudioEndpoint(this MMDeviceEnumerator enumerator, EDataFlow dataFlow, ERole role)
    {
        var iEnumerator = (IMMDeviceEnumerator)enumerator;
        iEnumerator.GetDefaultAudioEndpoint(dataFlow, role, out var device);
        return device;
    }

    public static uint GetCount(this IMMDeviceCollection collection)
    {
        collection.GetCount(out var count);
        return count;
    }

    public static IMMDevice Item(this IMMDeviceCollection collection, uint index)
    {
        collection.Item(index, out var device);
        return device;
    }

    public static string GetId(this IMMDevice device)
    {
        device.GetId(out var id);
        return id;
    }

    public static DeviceState GetState(this IMMDevice device)
    {
        device.GetState(out var state);
        return state;
    }

    public static IPropertyStore OpenPropertyStore(this IMMDevice device, StorageAccessMode mode)
    {
        device.OpenPropertyStore(mode, out var store);
        return store;
    }

    public static string? GetValue(this IPropertyStore store, PropertyKey key)
    {
        try
        {
            var propKey = key;
            store.GetValue(ref propKey, out var value);
            if (value.vt == 31) // VT_LPWSTR
            {
                return Marshal.PtrToStringUni(value.pwszVal);
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public static void SetDefaultEndpoint(this PolicyConfigClient config, string deviceId, ERole role)
    {
        var iPolicyConfig = (IPolicyConfig)config;
        iPolicyConfig.SetDefaultEndpoint(deviceId, role);
    }
}

#endregion
