using System.Runtime.InteropServices;
using SaveState.Core.Common;
using SaveState.Core.Input.Services;
using SaveState.Core.Input.Entities;

namespace SaveState.Infrastructure.Input;

public class SteamDeckManager : ISteamDeckManager
{
    private readonly IControllerProfileService _controllerProfileService;
    private bool _isSteamDeckModeActive;
    private SteamDeckProfile? _activeProfile;

    public event EventHandler<SteamDeckModeChangedEventArgs>? SteamDeckModeChanged;

    public SteamDeckManager(IControllerProfileService controllerProfileService)
    {
        _controllerProfileService = controllerProfileService;
    }

    public bool IsSteamDeckModeActive => _isSteamDeckModeActive;

    public async Task<Result<bool>> DetectSteamDeckAsync(CancellationToken ct = default)
    {
        try
        {
            // Check for Steam Deck hardware identifiers
            var isSteamDeck = await DetectSteamDeckHardwareAsync(ct);

            // Check for Steam Deck specific environment variables
            var hasSteamDeckEnvironment = HasSteamDeckEnvironment();

            // Check for Steam Deck specific processes
            var hasSteamDeckProcesses = await HasSteamDeckProcessesAsync(ct);

            var detectionConfidence = (isSteamDeck ? 1 : 0) +
                                    (hasSteamDeckEnvironment ? 1 : 0) +
                                    (hasSteamDeckProcesses ? 1 : 0);

            var isDetected = detectionConfidence >= 2; // Require at least 2 indicators

            return Result<bool>.Success(isDetected);
        }
        catch (Exception ex)
        {
            return Result<bool>.Failure($"Failed to detect Steam Deck: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> EnableSteamDeckModeAsync(CancellationToken ct = default)
    {
        try
        {
            if (_isSteamDeckModeActive)
            {
                return Result.Success(); // Already active
            }

            // Create default Steam Deck controller profile if it doesn't exist
            var steamDeckProfiles = await _controllerProfileService.GetProfilesByTypeAsync(ControllerType.SteamDeck, ct);
            if (!steamDeckProfiles.Value.Any())
            {
                await _controllerProfileService.CreateProfileAsync(
                    "Steam Deck Default",
                    ControllerType.SteamDeck,
                    null,
                    ct);
            }

            // Apply Steam Deck optimizations
            await ApplySteamDeckOptimizationsAsync(ct);

            _isSteamDeckModeActive = true;

            // Create default profile if none exists
            if (_activeProfile == null)
            {
                var defaultConfig = new SteamDeckConfig(
                    GyroSensitivity.Medium,
                    TouchSensitivity.High,
                    EnableHaptics: true,
                    OptimizeForBattery: true,
                    EnableSteamInput: true,
                    ForceDesktopMode: false);

                var createResult = await CreateProfileAsync(defaultConfig, ct);
                if (createResult.IsSuccess)
                {
                    _activeProfile = createResult.Value;
                }
            }

            SteamDeckModeChanged?.Invoke(this, new SteamDeckModeChangedEventArgs
            {
                IsActive = true,
                ActiveConfig = _activeProfile?.Config
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to enable Steam Deck mode: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> DisableSteamDeckModeAsync(CancellationToken ct = default)
    {
        try
        {
            if (!_isSteamDeckModeActive)
            {
                return Result.Success(); // Already disabled
            }

            // Restore original settings
            await RestoreOriginalSettingsAsync(ct);

            _isSteamDeckModeActive = false;
            _activeProfile = null;

            SteamDeckModeChanged?.Invoke(this, new SteamDeckModeChangedEventArgs
            {
                IsActive = false,
                ActiveConfig = null
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure($"Failed to disable Steam Deck mode: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<SteamDeckProfile>> CreateProfileAsync(SteamDeckConfig config, CancellationToken ct = default)
    {
        try
        {
            // Create controller profile for Steam Deck
            var controllerProfileResult = await _controllerProfileService.CreateProfileAsync(
                $"Steam Deck Profile - {DateTime.Now:yyyy-MM-dd HH:mm}",
                ControllerType.SteamDeck,
                null,
                ct);

            if (controllerProfileResult.IsFailure)
            {
                return Result<SteamDeckProfile>.Failure(controllerProfileResult.Error, ErrorType.Internal);
            }

            var profile = new SteamDeckProfile(
                Id: Guid.NewGuid(),
                Name: $"Steam Deck Profile {DateTime.Now:yyyy-MM-dd}",
                Config: config,
                CreatedAt: DateTime.UtcNow,
                IsActive: false);

            return Result<SteamDeckProfile>.Success(profile);
        }
        catch (Exception ex)
        {
            return Result<SteamDeckProfile>.Failure($"Failed to create Steam Deck profile: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result<SteamDeckProfile?>> GetCurrentProfileAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Result<SteamDeckProfile?>.Success(_activeProfile));
    }

    public Task<Result<IReadOnlyList<SteamDeckProfile>>> GetAllProfilesAsync(CancellationToken ct = default)
    {
        try
        {
            // For now, return empty list as we're not persisting profiles yet
            // In a full implementation, this would query a repository
            var profiles = new List<SteamDeckProfile>();

            if (_activeProfile != null)
            {
                profiles.Add(_activeProfile);
            }

            return Task.FromResult(Result<IReadOnlyList<SteamDeckProfile>>.Success((IReadOnlyList<SteamDeckProfile>)profiles));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IReadOnlyList<SteamDeckProfile>>.Failure($"Failed to get Steam Deck profiles: {ex.Message}", ErrorType.Internal));
        }
    }

    private Task<bool> DetectSteamDeckHardwareAsync(CancellationToken ct)
    {
        try
        {
            // Check for Steam Deck specific hardware identifiers
            // This is a simplified implementation - real detection would use hardware queries

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // Check for Steam Deck specific files/directories
                var steamDeckIndicators = new[]
                {
                    "/etc/os-release", // Contains SteamOS information
                    "/home/deck",      // Default Steam Deck user directory
                    "/usr/bin/steamos-session-select"
                };

                foreach (var indicator in steamDeckIndicators)
                {
                    if (File.Exists(indicator))
                    {
                        return Task.FromResult(true);
                    }
                }
            }

            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private bool HasSteamDeckEnvironment()
    {
        try
        {
            // Check for Steam Deck environment variables
            var steamDeckEnvVars = new[]
            {
                "SteamDeck",
                "STEAMOS",
                "JUPITER" // Valve's codename for Steam Deck
            };

            foreach (var envVar in steamDeckEnvVars)
            {
                if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVar)))
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private Task<bool> HasSteamDeckProcessesAsync(CancellationToken ct)
    {
        try
        {
            // Check for Steam Deck specific processes
            var steamDeckProcesses = new[]
            {
                "steam",
                "gamescope",
                "steamos-session",
                "jupiter-biosupdate"
            };

            foreach (var processName in steamDeckProcesses)
            {
                var processes = System.Diagnostics.Process.GetProcessesByName(processName);
                if (processes.Length > 0)
                {
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private Task ApplySteamDeckOptimizationsAsync(CancellationToken ct)
    {
        // Apply Steam Deck specific optimizations
        // This would include:
        // - Setting appropriate refresh rates
        // - Configuring input settings
        // - Adjusting performance profiles
        // - Setting up Steam Input configuration

        return Task.CompletedTask; // Placeholder for actual implementation
    }

    private Task RestoreOriginalSettingsAsync(CancellationToken ct)
    {
        // Restore original system settings
        return Task.CompletedTask; // Placeholder for actual implementation
    }
}
