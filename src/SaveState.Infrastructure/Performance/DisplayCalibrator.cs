using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Performance.Services;
using System.Runtime.InteropServices;

namespace SaveState.Infrastructure.Performance;

/// <summary>
/// Service for display calibration and profile management.
/// </summary>
public class DisplayCalibrator : IDisplayCalibrator
{
    private readonly ILogger<DisplayCalibrator> _logger;
    private readonly Dictionary<Guid, DisplayProfile> _profiles = new();
    private DisplaySettings? _originalSettings;
    private readonly object _profilesLock = new();

    public DisplayCalibrator(ILogger<DisplayCalibrator> logger)
    {
        _logger = logger;
    }

    public Task<Result<DisplaySettings>> GetCurrentSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            _logger.LogDebug("Getting current display settings");

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsDisplaySettingsAsync(ct);
            }

            // Fallback for non-Windows
            var defaultSettings = new DisplaySettings(
                Width: 1920,
                Height: 1080,
                RefreshRate: 60,
                VSync: true,
                HdrEnabled: false,
                GSync: false,
                FullscreenOptimizations: true,
                BitDepth: 32);

            return Task.FromResult(Result.Success<DisplaySettings>(defaultSettings));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get display settings");
            return Task.FromResult(Result.Failure<DisplaySettings>($"Failed to get display settings: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<DisplayProfile>> CreateGameProfileAsync(Guid gameId, DisplaySettings settings, CancellationToken ct = default)
    {
        try
        {
            var profile = DisplayProfile.Create(gameId, $"Game Profile - {gameId:N}", settings, false);

            lock (_profilesLock)
            {
                _profiles[profile.Id] = profile;
            }

            _logger.LogInformation("Created display profile {ProfileId} for game {GameId}", profile.Id, gameId);

            return Task.FromResult(Result.Success<DisplayProfile>(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create display profile for game {GameId}", gameId);
            return Task.FromResult(Result.Failure<DisplayProfile>($"Failed to create profile: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<DisplayProfile>> GetProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        lock (_profilesLock)
        {
            if (_profiles.TryGetValue(profileId, out var profile))
            {
                return Task.FromResult(Result.Success<DisplayProfile>(profile));
            }
        }

        return Task.FromResult(Result.Failure<DisplayProfile>($"Profile {profileId} not found", ErrorType.NotFound));
    }

    public Task<Result<IReadOnlyList<DisplayProfile>>> GetProfilesForGameAsync(Guid gameId, CancellationToken ct = default)
    {
        lock (_profilesLock)
        {
            var profiles = _profiles.Values
                .Where(p => p.GameId == gameId)
                .ToList();

            return Task.FromResult(Result.Success<IReadOnlyList<DisplayProfile>>((IReadOnlyList<DisplayProfile>)profiles));
        }
    }

    public async Task<Result> ApplyProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        try
        {
            DisplayProfile? profile;
            lock (_profilesLock)
            {
                if (!_profiles.TryGetValue(profileId, out profile))
                {
                    return Result.Failure($"Profile {profileId} not found", ErrorType.NotFound);
                }
            }

            // Store original settings if not already stored
            if (_originalSettings == null)
            {
                var currentResult = await GetCurrentSettingsAsync(ct);
                if (currentResult.IsSuccess)
                {
                    _originalSettings = currentResult.Value;
                }
            }

            // Apply the settings
            var applyResult = await ApplyDisplaySettingsAsync(profile.Settings, ct);
            if (!applyResult.IsSuccess)
            {
                return applyResult;
            }

            profile.MarkApplied();

            _logger.LogInformation("Applied display profile {ProfileId}", profileId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply display profile {ProfileId}", profileId);
            return Result.Failure($"Failed to apply profile: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> RevertSettingsAsync(CancellationToken ct = default)
    {
        try
        {
            if (_originalSettings == null)
            {
                return Result.Failure("No original settings to revert to", ErrorType.Validation);
            }

            var result = await ApplyDisplaySettingsAsync(_originalSettings, ct);
            if (result.IsSuccess)
            {
                _originalSettings = null;
                _logger.LogInformation("Reverted display settings to original");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revert display settings");
            return Result.Failure($"Failed to revert settings: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<IReadOnlyList<int>>> GetAvailableRefreshRatesAsync(CancellationToken ct = default)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var rates = GetWindowsRefreshRates();
                return Task.FromResult(Result.Success<IReadOnlyList<int>>(rates));
            }

            // Common refresh rates as fallback
            var defaultRates = new List<int> { 60, 75, 120, 144, 165, 240 };
            return Task.FromResult(Result.Success<IReadOnlyList<int>>((IReadOnlyList<int>)defaultRates));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available refresh rates");
            return Task.FromResult(Result.Failure<IReadOnlyList<int>>($"Failed to get refresh rates: {ex.Message}", ErrorType.Internal));
        }
    }

    public Task<Result<IReadOnlyList<DisplayResolution>>> GetAvailableResolutionsAsync(CancellationToken ct = default)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var resolutions = GetWindowsResolutions();
                return Task.FromResult(Result.Success<IReadOnlyList<DisplayResolution>>(resolutions));
            }

            // Common resolutions as fallback
            var defaultResolutions = new List<DisplayResolution>
            {
                new(1920, 1080, "16:9"),
                new(2560, 1440, "16:9"),
                new(3840, 2160, "16:9"),
                new(1920, 1200, "16:10"),
                new(2560, 1080, "21:9"),
                new(3440, 1440, "21:9")
            };

            return Task.FromResult(Result.Success<IReadOnlyList<DisplayResolution>>((IReadOnlyList<DisplayResolution>)defaultResolutions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available resolutions");
            return Task.FromResult(Result.Failure<IReadOnlyList<DisplayResolution>>($"Failed to get resolutions: {ex.Message}", ErrorType.Internal));
        }
    }

    #region Windows-Specific Implementation

    private Task<Result<DisplaySettings>> GetWindowsDisplaySettingsAsync(CancellationToken ct)
    {
        try
        {
            var devMode = new DEVMODE();
            devMode.dmSize = (ushort)Marshal.SizeOf(devMode);

            if (EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode))
            {
                var settings = new DisplaySettings(
                    Width: devMode.dmPelsWidth,
                    Height: devMode.dmPelsHeight,
                    RefreshRate: devMode.dmDisplayFrequency,
                    VSync: true, // Can't detect VSync via Win32 API
                    HdrEnabled: false, // Would need more advanced APIs
                    GSync: false, // Would need NVIDIA API
                    FullscreenOptimizations: true,
                    BitDepth: devMode.dmBitsPerPel);

                return Task.FromResult(Result.Success<DisplaySettings>(settings));
            }

            return Task.FromResult(Result.Failure<DisplaySettings>("Failed to enumerate display settings", ErrorType.Internal));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<DisplaySettings>($"Windows API error: {ex.Message}", ErrorType.Internal));
        }
    }

    private Task<Result> ApplyDisplaySettingsAsync(DisplaySettings settings, CancellationToken ct)
    {
        try
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogWarning("Display settings changes only supported on Windows");
                return Task.FromResult(Result.Success()); // Silent success on non-Windows
            }

            var devMode = new DEVMODE();
            devMode.dmSize = (ushort)Marshal.SizeOf(devMode);

            if (!EnumDisplaySettings(null, ENUM_CURRENT_SETTINGS, ref devMode))
            {
                return Task.FromResult(Result.Failure("Failed to get current display mode", ErrorType.Internal));
            }

            // Only apply settings that are different
            bool changed = false;

            if (devMode.dmPelsWidth != settings.Width || devMode.dmPelsHeight != settings.Height)
            {
                devMode.dmPelsWidth = settings.Width;
                devMode.dmPelsHeight = settings.Height;
                devMode.dmFields |= DM_PELSWIDTH | DM_PELSHEIGHT;
                changed = true;
            }

            if (devMode.dmDisplayFrequency != settings.RefreshRate)
            {
                devMode.dmDisplayFrequency = settings.RefreshRate;
                devMode.dmFields |= DM_DISPLAYFREQUENCY;
                changed = true;
            }

            if (devMode.dmBitsPerPel != settings.BitDepth)
            {
                devMode.dmBitsPerPel = settings.BitDepth;
                devMode.dmFields |= DM_BITSPERPEL;
                changed = true;
            }

            if (!changed)
            {
                _logger.LogDebug("Display settings unchanged, skipping apply");
                return Task.FromResult(Result.Success());
            }

            var result = ChangeDisplaySettings(ref devMode, CDS_UPDATEREGISTRY);
            if (result != DISP_CHANGE_SUCCESSFUL)
            {
                return Task.FromResult(Result.Failure($"Failed to change display settings (error code: {result})", ErrorType.Internal));
            }

            _logger.LogInformation("Applied display settings: {Width}x{Height}@{Hz}",
                settings.Width, settings.Height, settings.RefreshRate);

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Failed to apply display settings: {ex.Message}", ErrorType.Internal));
        }
    }

    private List<int> GetWindowsRefreshRates()
    {
        var rates = new HashSet<int>();
        var devMode = new DEVMODE();
        devMode.dmSize = (ushort)Marshal.SizeOf(devMode);

        int modeNum = 0;
        while (EnumDisplaySettings(null, modeNum++, ref devMode))
        {
            if (devMode.dmDisplayFrequency > 0)
            {
                rates.Add(devMode.dmDisplayFrequency);
            }
        }

        return rates.OrderBy(r => r).ToList();
    }

    private List<DisplayResolution> GetWindowsResolutions()
    {
        var resolutions = new Dictionary<(int, int), DisplayResolution>();
        var devMode = new DEVMODE();
        devMode.dmSize = (ushort)Marshal.SizeOf(devMode);

        int modeNum = 0;
        while (EnumDisplaySettings(null, modeNum++, ref devMode))
        {
            var key = (devMode.dmPelsWidth, devMode.dmPelsHeight);
            if (!resolutions.ContainsKey(key) && devMode.dmPelsWidth >= 800)
            {
                var aspectRatio = CalculateAspectRatio(devMode.dmPelsWidth, devMode.dmPelsHeight);
                resolutions[key] = new DisplayResolution(
                    devMode.dmPelsWidth,
                    devMode.dmPelsHeight,
                    aspectRatio);
            }
        }

        return resolutions.Values
            .OrderByDescending(r => r.Width)
            .ThenByDescending(r => r.Height)
            .ToList();
    }

    private static string CalculateAspectRatio(int width, int height)
    {
        var gcd = GCD(width, height);
        var w = width / gcd;
        var h = height / gcd;

        // Common aspect ratios
        if (w == 16 && h == 9) return "16:9";
        if (w == 16 && h == 10) return "16:10";
        if (w == 4 && h == 3) return "4:3";
        if (w == 21 && h == 9) return "21:9";
        if (w == 32 && h == 9) return "32:9";

        return $"{w}:{h}";
    }

    private static int GCD(int a, int b) => b == 0 ? a : GCD(b, a % b);

    #endregion

    #region Win32 API Declarations

    private const int ENUM_CURRENT_SETTINGS = -1;
    private const int CDS_UPDATEREGISTRY = 0x01;
    private const int DISP_CHANGE_SUCCESSFUL = 0;
    private const int DM_BITSPERPEL = 0x40000;
    private const int DM_PELSWIDTH = 0x80000;
    private const int DM_PELSHEIGHT = 0x100000;
    private const int DM_DISPLAYFREQUENCY = 0x400000;

    [DllImport("user32.dll")]
    private static extern bool EnumDisplaySettings(string? deviceName, int modeNum, ref DEVMODE devMode);

    [DllImport("user32.dll")]
    private static extern int ChangeDisplaySettings(ref DEVMODE devMode, int flags);

    [StructLayout(LayoutKind.Sequential)]
    private struct DEVMODE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmDeviceName;
        public ushort dmSpecVersion;
        public ushort dmDriverVersion;
        public ushort dmSize;
        public ushort dmDriverExtra;
        public uint dmFields;
        public int dmPositionX;
        public int dmPositionY;
        public uint dmDisplayOrientation;
        public uint dmDisplayFixedOutput;
        public short dmColor;
        public short dmDuplex;
        public short dmYResolution;
        public short dmTTOption;
        public short dmCollate;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string dmFormName;
        public ushort dmLogPixels;
        public int dmBitsPerPel;
        public int dmPelsWidth;
        public int dmPelsHeight;
        public uint dmDisplayFlags;
        public int dmDisplayFrequency;
        public int dmICMMethod;
        public int dmICMIntent;
        public int dmMediaType;
        public int dmDitherType;
        public int dmReserved1;
        public int dmReserved2;
        public int dmPanningWidth;
        public int dmPanningHeight;
    }

    #endregion
}

