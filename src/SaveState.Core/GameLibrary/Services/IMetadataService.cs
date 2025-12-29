using SaveState.Core.Common;
using SaveState.Core.GameLibrary.DTOs;

namespace SaveState.Core.GameLibrary.Services;

public interface IMetadataService
{
    Task<GameMetadata> GetGameMetadataAsync(string title, CancellationToken ct = default);
    Task<Result<byte[]>> GetCoverImageAsync(string title, CancellationToken ct = default);
}
