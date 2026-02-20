namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

/// <summary>
/// Represents a user-created collection of MUGEN characters.
/// </summary>
public class MugenCollection : EntityBase
{
    private static DateTime UtcNow => SystemTimeProvider.Instance.UtcNow;

    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description of the collection.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Optional icon for the collection.
    /// </summary>
    public string? Icon { get; private set; }

    /// <summary>
    /// The user ID who owns this collection.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// When the collection was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the collection was last modified.
    /// </summary>
    public DateTime LastModified { get; private set; }

    /// <summary>
    /// Whether this collection is public.
    /// </summary>
    public bool IsPublic { get; private set; }

    /// <summary>
    /// The characters in this collection.
    /// </summary>
    public ICollection<MugenCollectionCharacter> Characters { get; private set; } = new List<MugenCollectionCharacter>();

    /// <summary>
    /// Creates a new MUGEN character collection.
    /// </summary>
    /// <param name="name">Collection name.</param>
    /// <param name="userId">User ID who owns the collection.</param>
    /// <param name="description">Optional description.</param>
    /// <param name="icon">Optional icon.</param>
    /// <param name="isPublic">Whether the collection is public.</param>
    /// <returns>A new MugenCollection instance.</returns>
    public static MugenCollection Create(
        string name,
        Guid userId,
        string? description = null,
        string? icon = null,
        bool isPublic = false)
    {
        return new MugenCollection
        {
            Id = Guid.NewGuid(),
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Description = description,
            Icon = icon,
            UserId = userId,
            IsPublic = isPublic,
            CreatedAt = UtcNow,
            LastModified = UtcNow
        };
    }

    /// <summary>
    /// Updates the collection's metadata.
    /// </summary>
    /// <param name="name">New name.</param>
    /// <param name="description">New description.</param>
    /// <param name="icon">New icon.</param>
    /// <param name="isPublic">New public status.</param>
    public void Update(string name, string? description, string? icon, bool isPublic)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Description = description;
        Icon = icon;
        IsPublic = isPublic;
        LastModified = UtcNow;
    }

    /// <summary>
    /// Adds a character to this collection.
    /// </summary>
    /// <param name="characterId">The character ID to add.</param>
    /// <param name="notes">Optional notes about this character in the collection.</param>
    public void AddCharacter(Guid characterId, string? notes = null)
    {
        if (Characters.Any(c => c.CharacterId == characterId))
            throw new InvalidOperationException("Character is already in this collection.");

        Characters.Add(MugenCollectionCharacter.Create(Id, characterId, notes));
        LastModified = UtcNow;
    }

    /// <summary>
    /// Removes a character from this collection.
    /// </summary>
    /// <param name="characterId">The character ID to remove.</param>
    public void RemoveCharacter(Guid characterId)
    {
        var character = Characters.FirstOrDefault(c => c.CharacterId == characterId);
        if (character == null)
            throw new InvalidOperationException("Character is not in this collection.");

        Characters.Remove(character);
        LastModified = UtcNow;
    }

    // EF Core constructor
    private MugenCollection() { }
}
