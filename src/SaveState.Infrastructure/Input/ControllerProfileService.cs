using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Input;
using SaveState.Core.Input.Entities;
using SaveState.Core.Input.Services;

namespace SaveState.Infrastructure.Input;

/// <summary>
/// Service for managing controller profiles and input configurations.
/// Handles mapping of controller inputs to game actions.
/// </summary>
public class ControllerProfileService : IControllerProfileService
{
    private readonly IControllerProfileRepository _profileRepository;
    private readonly ILogger<ControllerProfileService> _logger;

    public ControllerProfileService(
        IControllerProfileRepository profileRepository,
        ILogger<ControllerProfileService> logger)
    {
        _profileRepository = profileRepository;
        _logger = logger;
    }

    public async Task<Result<ControllerProfile>> CreateProfileAsync(
        string name,
        ControllerType type,
        Guid? gameId = null,
        CancellationToken ct = default)
    {
        try
        {
            var profile = ControllerProfile.Create(name, type, gameId);

            // Set default mappings based on controller type
            var defaultMappings = GetDefaultMappingsForType(type);
            profile.SetMappings(defaultMappings);

            await _profileRepository.AddAsync(profile, ct);

            _logger.LogInformation("Created controller profile '{Name}' of type {Type} for game {GameId}",
                name, type, gameId);

            return Result.Success<ControllerProfile>(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create controller profile '{Name}'", name);
            return Result.Failure<ControllerProfile>($"Failed to create profile: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ControllerProfile>> GetProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId, ct);
            if (profile == null)
                return Result.Failure<ControllerProfile>("Controller profile not found", ErrorType.NotFound);

            return Result.Success<ControllerProfile>(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get controller profile {ProfileId}", profileId);
            return Result.Failure<ControllerProfile>($"Failed to get profile: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<ControllerProfile>>> GetProfilesForGameAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var profiles = await _profileRepository.GetByGameIdAsync(gameId, ct);
            return Result.Success<IReadOnlyList<ControllerProfile>>(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profiles for game {GameId}", gameId);
            return Result.Failure<IReadOnlyList<ControllerProfile>>($"Failed to get profiles: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<ControllerProfile>>> GetAllProfilesAsync(
        CancellationToken ct = default)
    {
        try
        {
            var profiles = await _profileRepository.GetAllAsync(ct);
            return Result.Success<IReadOnlyList<ControllerProfile>>(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all controller profiles");
            return Result.Failure<IReadOnlyList<ControllerProfile>>($"Failed to get profiles: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> UpdateMappingsAsync(
        Guid profileId,
        IReadOnlyDictionary<string, string> mappings,
        CancellationToken ct = default)
    {
        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId, ct);
            if (profile == null)
                return Result.Failure("Controller profile not found", ErrorType.NotFound);

            profile.SetMappings(mappings);
            profile.RecordUsage();

            await _profileRepository.UpdateAsync(profile, ct);

            _logger.LogInformation("Updated mappings for controller profile {ProfileId}", profileId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update mappings for profile {ProfileId}", profileId);
            return Result.Failure($"Failed to update mappings: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> DeleteProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId, ct);
            if (profile == null)
                return Result.Failure("Controller profile not found", ErrorType.NotFound);

            await _profileRepository.DeleteAsync(profileId, ct);

            _logger.LogInformation("Deleted controller profile '{Name}' ({ProfileId})",
                profile.Name, profileId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete controller profile {ProfileId}", profileId);
            return Result.Failure($"Failed to delete profile: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<ControllerProfile?>> GetDefaultProfileForGameAsync(
        Guid gameId,
        CancellationToken ct = default)
    {
        try
        {
            var profile = await _profileRepository.GetDefaultForGameAsync(gameId, ct);
            return Result.Success<ControllerProfile?>(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get default profile for game {GameId}", gameId);
            return Result.Failure<ControllerProfile?>($"Failed to get default profile: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> SetAsDefaultAsync(Guid profileId, CancellationToken ct = default)
    {
        try
        {
            var profile = await _profileRepository.GetByIdAsync(profileId, ct);
            if (profile == null)
                return Result.Failure("Controller profile not found", ErrorType.NotFound);

            if (!profile.GameId.HasValue)
                return Result.Failure("Only game-specific profiles can be set as default", ErrorType.Validation);

            // Clear any existing default for this game
            var existingDefaults = await _profileRepository.GetByGameIdAsync(profile.GameId.Value, ct);
            foreach (var existingProfile in existingDefaults.Where(p => p.IsDefault))
            {
                existingProfile.ClearDefault();
                await _profileRepository.UpdateAsync(existingProfile, ct);
            }

            // Set this profile as default
            profile.SetAsDefault();
            profile.RecordUsage();
            await _profileRepository.UpdateAsync(profile, ct);

            _logger.LogInformation("Set controller profile '{Name}' as default for game {GameId}",
                profile.Name, profile.GameId);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set profile {ProfileId} as default", profileId);
            return Result.Failure($"Failed to set as default: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<IReadOnlyList<ControllerProfile>>> GetProfilesByTypeAsync(
        ControllerType type,
        CancellationToken ct = default)
    {
        try
        {
            var profiles = await _profileRepository.GetByTypeAsync(type, ct);
            return Result.Success<IReadOnlyList<ControllerProfile>>(profiles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profiles by type {Type}", type);
            return Result.Failure<IReadOnlyList<ControllerProfile>>($"Failed to get profiles: {ex.Message}", ErrorType.Internal);
        }
    }

    public Task<Result> DetectControllersAsync(CancellationToken ct = default)
    {
        try
        {
            // Placeholder for controller detection
            // In a real implementation, this would detect connected controllers
            // and potentially create profiles for them

            _logger.LogInformation("Controller detection completed (placeholder implementation)");
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect controllers");
            return Task.FromResult(Result.Failure($"Controller detection failed: {ex.Message}", ErrorType.Internal));
        }
    }

    private static IReadOnlyDictionary<string, string> GetDefaultMappingsForType(ControllerType type)
    {
        return type switch
        {
            ControllerType.Xbox => new Dictionary<string, string>
            {
                ["A"] = "Confirm",
                ["B"] = "Cancel",
                ["X"] = "Action1",
                ["Y"] = "Action2",
                ["LeftStick"] = "Move",
                ["RightStick"] = "Camera",
                ["LeftTrigger"] = "Sprint",
                ["RightTrigger"] = "Attack",
                ["LeftShoulder"] = "PrevWeapon",
                ["RightShoulder"] = "NextWeapon",
                ["DPadUp"] = "MenuUp",
                ["DPadDown"] = "MenuDown",
                ["DPadLeft"] = "MenuLeft",
                ["DPadRight"] = "MenuRight",
                ["Start"] = "Pause",
                ["Back"] = "Map"
            },
            ControllerType.PlayStation => new Dictionary<string, string>
            {
                ["Cross"] = "Confirm",
                ["Circle"] = "Cancel",
                ["Square"] = "Action1",
                ["Triangle"] = "Action2",
                ["LeftStick"] = "Move",
                ["RightStick"] = "Camera",
                ["L2"] = "Sprint",
                ["R2"] = "Attack",
                ["L1"] = "PrevWeapon",
                ["R1"] = "NextWeapon",
                ["DPadUp"] = "MenuUp",
                ["DPadDown"] = "MenuDown",
                ["DPadLeft"] = "MenuLeft",
                ["DPadRight"] = "MenuRight",
                ["Options"] = "Pause",
                ["Share"] = "Map"
            },
            ControllerType.Nintendo => new Dictionary<string, string>
            {
                ["A"] = "Confirm",
                ["B"] = "Cancel",
                ["X"] = "Action1",
                ["Y"] = "Action2",
                ["LeftStick"] = "Move",
                ["RightStick"] = "Camera",
                ["ZL"] = "Sprint",
                ["ZR"] = "Attack",
                ["L"] = "PrevWeapon",
                ["R"] = "NextWeapon",
                ["DPadUp"] = "MenuUp",
                ["DPadDown"] = "MenuDown",
                ["DPadLeft"] = "MenuLeft",
                ["DPadRight"] = "MenuRight",
                ["Plus"] = "Pause",
                ["Minus"] = "Map"
            },
            ControllerType.Keyboard => new Dictionary<string, string>
            {
                ["WASD"] = "Move",
                ["Mouse"] = "Camera",
                ["Space"] = "Jump",
                ["Shift"] = "Sprint",
                ["Ctrl"] = "Crouch",
                ["E"] = "Interact",
                ["Q"] = "PrevWeapon",
                ["R"] = "NextWeapon",
                ["Tab"] = "Inventory",
                ["Esc"] = "Pause",
                ["M"] = "Map"
            },
            _ => new Dictionary<string, string>
            {
                ["Button1"] = "Confirm",
                ["Button2"] = "Cancel",
                ["Button3"] = "Action1",
                ["Button4"] = "Action2"
            }
        };
    }
}


