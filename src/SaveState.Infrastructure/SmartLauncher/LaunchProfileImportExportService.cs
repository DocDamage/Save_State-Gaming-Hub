// Copyright (c) 2026 SaveStateReborn. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.SmartLauncher;

namespace SaveState.Infrastructure.SmartLauncher;

/// <summary>
/// Implementation of launch profile import/export service.
/// </summary>
public sealed class LaunchProfileImportExportService : ILaunchProfileImportExportService
{
    private readonly ILaunchProfileRepository _profileRepository;
    private readonly ILogger<LaunchProfileImportExportService> _logger;
    private readonly ITimeProvider _timeProvider;
    private readonly JsonSerializerOptions _jsonOptions;

    public LaunchProfileImportExportService(
        ILaunchProfileRepository profileRepository,
        ILogger<LaunchProfileImportExportService> logger,
        ITimeProvider timeProvider)
    {
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task<string> ExportProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        try
        {
            var profileResult = await _profileRepository.GetProfileAsync(profileId, ct);
            if (!profileResult.IsSuccess)
            {
                throw new InvalidOperationException($"Profile {profileId} not found: {profileResult.Error}");
            }

            var dto = new LaunchProfileDto
            {
                Version = "1.0",
                ExportedAt = _timeProvider.UtcNow,
                Profiles = new List<LaunchProfileExportData> { MapToExportData(profileResult.Value) }
            };

            return JsonSerializer.Serialize(dto, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export profile {ProfileId}", profileId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> ExportAllProfilesAsync(CancellationToken ct = default)
    {
        try
        {
            var profiles = await _profileRepository.GetProfilesAsync(null, ct);

            var dto = new LaunchProfileDto
            {
                Version = "1.0",
                ExportedAt = _timeProvider.UtcNow,
                Profiles = profiles.Select(MapToExportData).ToList()
            };

            return JsonSerializer.Serialize(dto, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to export all profiles");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ImportResult> ImportProfileAsync(string json, bool overwriteExisting = false, CancellationToken ct = default)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<LaunchProfileDto>(json, _jsonOptions);
            if (dto?.Profiles == null || !dto.Profiles.Any())
            {
                return new ImportResult
                {
                    Success = false,
                    ErrorMessage = "No profiles found in import data"
                };
            }

            var profileData = dto.Profiles.First();
            var profile = MapFromExportData(profileData);

            // Check for existing profile with same name
            var existingProfiles = await _profileRepository.GetProfilesAsync(null, ct);
            var existing = existingProfiles.FirstOrDefault(p => 
                p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                if (!overwriteExisting)
                {
                    return new ImportResult
                    {
                        Success = false,
                        ErrorMessage = $"Profile '{profile.Name}' already exists",
                        WasExisting = true
                    };
                }

                // Update existing profile
                profile.Id = existing.Id;
                profile.CreatedAt = existing.CreatedAt;
            }

            await _profileRepository.SaveProfileAsync(profile, ct);

            _logger.LogInformation("Successfully imported profile: {ProfileName}", profile.Name);

            return new ImportResult
            {
                Success = true,
                ImportedProfile = profile,
                WasExisting = existing != null
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse profile JSON");
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Invalid JSON format: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import profile");
            return new ImportResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc />
    public async Task<BatchImportResult> ImportProfilesAsync(string json, bool overwriteExisting = false, CancellationToken ct = default)
    {
        var result = new BatchImportResult();

        try
        {
            var dto = JsonSerializer.Deserialize<LaunchProfileDto>(json, _jsonOptions);
            if (dto?.Profiles == null)
            {
                result.Errors.Add(new ImportError
                {
                    ProfileName = "Unknown",
                    ErrorMessage = "No profiles found in import data"
                });
                return result;
            }

            result.TotalProfiles = dto.Profiles.Count;

            foreach (var profileData in dto.Profiles)
            {
                try
                {
                    var importResult = await ImportSingleProfileAsync(profileData, overwriteExisting, ct);
                    
                    if (importResult.Success)
                    {
                        if (importResult.WasExisting)
                        {
                            result.SkippedImports++;
                        }
                        else
                        {
                            result.SuccessfulImports++;
                        }
                    }
                    else
                    {
                        result.FailedImports++;
                        result.Errors.Add(new ImportError
                        {
                            ProfileName = profileData.Name,
                            ErrorMessage = importResult.ErrorMessage ?? "Unknown error"
                        });
                    }
                }
                catch (Exception ex)
                {
                    result.FailedImports++;
                    result.Errors.Add(new ImportError
                    {
                        ProfileName = profileData.Name,
                        ErrorMessage = ex.Message
                    });
                }
            }

            return result;
        }
        catch (JsonException ex)
        {
            result.Errors.Add(new ImportError
            {
                ProfileName = "All",
                ErrorMessage = $"Invalid JSON format: {ex.Message}"
            });
            return result;
        }
    }

    /// <inheritdoc />
    public Result<ValidationResult> ValidateProfileJson(string json)
    {
        var result = new ValidationResult();

        try
        {
            var dto = JsonSerializer.Deserialize<LaunchProfileDto>(json, _jsonOptions);
            
            if (dto == null)
            {
                result.Errors.Add("Invalid JSON structure");
                return Result.Success(result);
            }

            if (dto.Profiles == null || !dto.Profiles.Any())
            {
                result.Errors.Add("No profiles found in import data");
                return Result.Success(result);
            }

            result.ProfileCount = dto.Profiles.Count;

            // Validate each profile
            for (int i = 0; i < dto.Profiles.Count; i++)
            {
                var profile = dto.Profiles[i];
                if (string.IsNullOrWhiteSpace(profile.Name))
                {
                    result.Errors.Add($"Profile {i + 1}: Name is required");
                }

                if (!Enum.TryParse<ProcessPriority>(profile.Priority, out _))
                {
                    result.Errors.Add($"Profile '{profile.Name}': Invalid priority '{profile.Priority}'");
                }
            }

            result.IsValid = !result.Errors.Any();
            return Result.Success(result);
        }
        catch (JsonException ex)
        {
            result.Errors.Add($"Invalid JSON: {ex.Message}");
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating profile JSON");
            return Result.Failure<ValidationResult>($"Failed to validate profile JSON: {ex.Message}", ErrorType.Internal);
        }
    }

    private async Task<ImportResult> ImportSingleProfileAsync(LaunchProfileExportData data, bool overwriteExisting, CancellationToken ct)
    {
        var profile = MapFromExportData(data);

        var existingProfiles = await _profileRepository.GetProfilesAsync(null, ct);
        var existing = existingProfiles.FirstOrDefault(p => 
            p.Name.Equals(profile.Name, StringComparison.OrdinalIgnoreCase));

        if (existing != null && !overwriteExisting)
        {
            return new ImportResult
            {
                Success = false,
                ErrorMessage = $"Profile '{profile.Name}' already exists",
                WasExisting = true
            };
        }

        if (existing != null)
        {
            profile.Id = existing.Id;
            profile.CreatedAt = existing.CreatedAt;
        }

        await _profileRepository.SaveProfileAsync(profile, ct);

        return new ImportResult
        {
            Success = true,
            ImportedProfile = profile,
            WasExisting = existing != null
        };
    }

    private static LaunchProfileExportData MapToExportData(LaunchProfile profile)
    {
        return new LaunchProfileExportData
        {
            Name = profile.Name,
            Description = profile.Description,
            Priority = profile.Priority.ToString(),
            DisableGameMode = profile.DisableGameMode,
            DisableFullscreenOptimizations = profile.DisableFullscreenOptimizations,
            RunAsAdministrator = profile.RunAsAdministrator,
            ProcessesToSuspend = profile.ProcessesToSuspend,
            ServicesToStop = profile.ServicesToStop,
            PowerPlanGuid = profile.PowerPlanGuid,
            EstimatedPerformanceGain = profile.EstimatedPerformanceGain,
            PerformanceSettings = new PerformanceSettingsExportData
            {
                EnableMemoryOptimization = profile.PerformanceSettings.EnableMemoryOptimization,
                EnableCPUParking = profile.PerformanceSettings.EnableCPUParking,
                DisableVisualEffects = profile.PerformanceSettings.DisableVisualEffects,
                ClearStandbyList = profile.PerformanceSettings.ClearStandbyList,
                TargetFPS = profile.PerformanceSettings.TargetFPS,
                EnableHardwareGPUScheduling = profile.PerformanceSettings.EnableHardwareGPUScheduling
            },
            DisplaySettings = profile.DisplaySettings != null ? new DisplaySettingsExportData
            {
                ResolutionWidth = profile.DisplaySettings.ResolutionWidth,
                ResolutionHeight = profile.DisplaySettings.ResolutionHeight,
                RefreshRate = profile.DisplaySettings.RefreshRate,
                EnableHDR = profile.DisplaySettings.EnableHDR,
                DisableFullscreenOptimizations = profile.DisplaySettings.DisableFullscreenOptimizations,
                OverrideDPIScaling = profile.DisplaySettings.OverrideDPIScaling
            } : null
        };
    }

    private LaunchProfile MapFromExportData(LaunchProfileExportData data)
    {
        var profile = new LaunchProfile
        {
            Id = Guid.NewGuid(),
            Name = data.Name,
            Description = data.Description,
            Priority = Enum.TryParse<ProcessPriority>(data.Priority, out var priority) ? priority : ProcessPriority.High,
            DisableGameMode = data.DisableGameMode,
            DisableFullscreenOptimizations = data.DisableFullscreenOptimizations,
            RunAsAdministrator = data.RunAsAdministrator,
            ProcessesToSuspend = data.ProcessesToSuspend ?? new List<string>(),
            ServicesToStop = data.ServicesToStop ?? new List<string>(),
            PowerPlanGuid = data.PowerPlanGuid,
            EstimatedPerformanceGain = data.EstimatedPerformanceGain,
            PerformanceSettings = new PerformanceSettings
            {
                EnableMemoryOptimization = data.PerformanceSettings?.EnableMemoryOptimization ?? true,
                EnableCPUParking = data.PerformanceSettings?.EnableCPUParking ?? true,
                DisableVisualEffects = data.PerformanceSettings?.DisableVisualEffects ?? false,
                ClearStandbyList = data.PerformanceSettings?.ClearStandbyList ?? false,
                TargetFPS = data.PerformanceSettings?.TargetFPS,
                EnableHardwareGPUScheduling = data.PerformanceSettings?.EnableHardwareGPUScheduling ?? true
            },
            CreatedAt = _timeProvider.UtcNow
        };

        if (data.DisplaySettings != null)
        {
            profile.DisplaySettings = new DisplaySettings
            {
                ResolutionWidth = data.DisplaySettings.ResolutionWidth,
                ResolutionHeight = data.DisplaySettings.ResolutionHeight,
                RefreshRate = data.DisplaySettings.RefreshRate,
                EnableHDR = data.DisplaySettings.EnableHDR,
                DisableFullscreenOptimizations = data.DisplaySettings.DisableFullscreenOptimizations,
                OverrideDPIScaling = data.DisplaySettings.OverrideDPIScaling
            };
        }

        return profile;
    }
}
