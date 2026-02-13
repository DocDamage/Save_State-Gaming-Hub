using SaveState.Core.Common;
using SaveState.Core.Mugen.ValueObjects;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SaveState.Core.Mugen.Services;

/// <summary>
/// Service for managing MUGEN character rosters.
/// </summary>
public interface IMugenRosterService
{
    /// <summary>
    /// Gets all characters in the roster.
    /// </summary>
    Task<Result<IReadOnlyList<CharacterInfo>>> GetAllCharactersAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches characters by name.
    /// </summary>
    Task<Result<IReadOnlyList<CharacterInfo>>> SearchCharactersAsync(string searchTerm, CancellationToken ct = default);

    /// <summary>
    /// Gets characters filtered by category.
    /// </summary>
    Task<Result<IReadOnlyList<CharacterInfo>>> GetCharactersByCategoryAsync(string category, CancellationToken ct = default);

    /// <summary>
    /// Adds a character to the roster.
    /// </summary>
    Task<Result> AddCharacterAsync(CharacterInfo character, CancellationToken ct = default);

    /// <summary>
    /// Removes a character from the roster.
    /// </summary>
    Task<Result> RemoveCharacterAsync(string characterId, CancellationToken ct = default);

    /// <summary>
    /// Loads the roster from the select.def file.
    /// </summary>
    Task<Result<MugenRoster>> LoadRosterAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the path to the select.def file.
    /// </summary>
    string? GetSelectDefPath();

    /// <summary>
    /// Saves the roster to the select.def file.
    /// </summary>
    Task<Result> SaveRosterAsync(MugenRoster roster, CancellationToken ct = default);
}

/// <summary>
/// Basic character information for roster display.
/// </summary>
public record CharacterInfo(
    string Id,
    string Name,
    string DisplayName,
    string? Category,
    string? Author,
    string? Version,
    string? ThumbnailPath);
