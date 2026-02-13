using System;

namespace SaveState.Core.OpenMK.Entities;

/// <summary>
/// Represents a user unlocking an OpenMK character.
/// </summary>
public class OpenMKCharacterUnlock
{
    private OpenMKCharacterUnlock() { }

    public OpenMKCharacterUnlock(Guid userId, Guid characterId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CharacterId = characterId;
        UnlockedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CharacterId { get; private set; }
    public DateTime UnlockedAt { get; private set; }
}
