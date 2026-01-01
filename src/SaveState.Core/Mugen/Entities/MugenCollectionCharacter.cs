namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;

/// <summary>
/// Represents the association between a MUGEN collection and a character.
/// </summary>
public class MugenCollectionCharacter : EntityBase
{
    /// <summary>
    /// The ID of the collection this character belongs to.
    /// </summary>
    public Guid CollectionId { get; private set; }

    /// <summary>
    /// The collection this character belongs to.
    /// </summary>
    public MugenCharacterCollection Collection { get; private set; } = null!;

    /// <summary>
    /// The ID of the character in this collection.
    /// </summary>
    public Guid CharacterId { get; private set; }

    /// <summary>
    /// The character in this collection.
    /// </summary>
    public MugenCharacter Character { get; private set; } = null!;

    /// <summary>
    /// Optional notes about this character in the collection.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// Whether this character is marked as a favorite in the collection.
    /// </summary>
    public bool IsFavorite { get; private set; }

    /// <summary>
    /// When this character was added to the collection.
    /// </summary>
    public DateTime AddedAt { get; private set; }

    /// <summary>
    /// Creates a new collection-character association.
    /// </summary>
    /// <param name="collectionId">The collection ID.</param>
    /// <param name="characterId">The character ID.</param>
    /// <param name="notes">Optional notes.</param>
    /// <returns>A new MugenCollectionCharacter instance.</returns>
    public static MugenCollectionCharacter Create(Guid collectionId, Guid characterId, string? notes = null)
    {
        return new MugenCollectionCharacter
        {
            Id = Guid.NewGuid(),
            CollectionId = collectionId,
            CharacterId = characterId,
            Notes = notes,
            IsFavorite = false,
            AddedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the notes for this character in the collection.
    /// </summary>
    /// <param name="notes">New notes.</param>
    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }

    /// <summary>
    /// Toggles the favorite status of this character in the collection.
    /// </summary>
    public void ToggleFavorite()
    {
        IsFavorite = !IsFavorite;
    }

    /// <summary>
    /// Marks this character as a favorite in the collection.
    /// </summary>
    public void MarkAsFavorite()
    {
        IsFavorite = true;
    }

    /// <summary>
    /// Unmarks this character as a favorite in the collection.
    /// </summary>
    public void UnmarkAsFavorite()
    {
        IsFavorite = false;
    }

    // EF Core constructor
    private MugenCollectionCharacter() { }
}