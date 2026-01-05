namespace SaveState.Core.GameLibrary;

using SaveState.Core.Common.ValueObjects;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Repository interface for managing game notes.
/// </summary>
public interface IGameNoteRepository
{
    /// <summary>
    /// Retrieves all notes for a specific game and user.
    /// </summary>
    Task<IReadOnlyList<GameNote>> GetByGameIdAsync(GameId gameId, UserId userId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a specific note by ID.
    /// </summary>
    Task<GameNote?> GetByIdAsync(Guid noteId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new note.
    /// </summary>
    Task<GameNote> AddAsync(GameNote note, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing note.
    /// </summary>
    Task UpdateAsync(GameNote note, CancellationToken ct = default);

    /// <summary>
    /// Deletes a note.
    /// </summary>
    Task DeleteAsync(Guid noteId, CancellationToken ct = default);

    /// <summary>
    /// Searches notes by content or title.
    /// </summary>
    Task<IReadOnlyList<GameNote>> SearchAsync(UserId userId, string searchTerm, CancellationToken ct = default);
}
