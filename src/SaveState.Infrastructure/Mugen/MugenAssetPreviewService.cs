namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Lists sprite, animation, and sound assets for a MUGEN character.
/// </summary>
public class MugenAssetPreviewService : IMugenAssetPreviewService
{
    public Task<Result<IReadOnlyList<MugenAssetEntry>>> GetAssetsAsync(
        MugenCharacter character,
        CancellationToken ct = default)
    {
        if (character == null)
            return Task.FromResult(Result.Failure<IReadOnlyList<MugenAssetEntry>>("Character is required."));

        var directory = character.CharacterDirectory;
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            return Task.FromResult(Result.Failure<IReadOnlyList<MugenAssetEntry>>("Character directory not found."));

        var entries = new List<MugenAssetEntry>();
        var extensions = new Dictionary<string, MugenAssetType>(StringComparer.OrdinalIgnoreCase)
        {
            [".sff"] = MugenAssetType.Sprite,
            [".air"] = MugenAssetType.Animation,
            [".snd"] = MugenAssetType.Sound
        };

        try
        {
            foreach (var file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(file);
                if (!extensions.TryGetValue(ext, out var assetType))
                    continue;

                var info = new FileInfo(file);
                entries.Add(new MugenAssetEntry(info.Name, file, info.Length, assetType));
            }

            return Task.FromResult(Result.Success<IReadOnlyList<MugenAssetEntry>>(entries));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<IReadOnlyList<MugenAssetEntry>>($"Asset scan failed: {ex.Message}"));
        }
    }
}
