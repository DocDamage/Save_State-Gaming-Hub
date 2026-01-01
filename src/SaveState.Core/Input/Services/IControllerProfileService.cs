using SaveState.Core.Common;
using SaveState.Core.Input.Entities;

namespace SaveState.Core.Input.Services;

public interface IControllerProfileService
{
    Task<Result<ControllerProfile>> CreateProfileAsync(
        string name,
        ControllerType type,
        Guid? gameId = null,
        CancellationToken ct = default);

    Task<Result<ControllerProfile>> GetProfileAsync(Guid profileId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ControllerProfile>>> GetProfilesForGameAsync(
        Guid gameId,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<ControllerProfile>>> GetAllProfilesAsync(
        CancellationToken ct = default);

    Task<Result> UpdateMappingsAsync(
        Guid profileId,
        IReadOnlyDictionary<string, string> mappings,
        CancellationToken ct = default);

    Task<Result> DeleteProfileAsync(Guid profileId, CancellationToken ct = default);

    Task<Result<ControllerProfile?>> GetDefaultProfileForGameAsync(
        Guid gameId,
        CancellationToken ct = default);

    Task<Result> SetAsDefaultAsync(Guid profileId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<ControllerProfile>>> GetProfilesByTypeAsync(
        ControllerType type,
        CancellationToken ct = default);

    Task<Result> DetectControllersAsync(CancellationToken ct = default);
}