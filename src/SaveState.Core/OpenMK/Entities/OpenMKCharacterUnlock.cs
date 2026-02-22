using SaveState.Core.Common.Services;

namespace SaveState.Core.OpenMK.Entities;

/// <summary>
/// Represents a user unlocking an OpenMK character.
/// </summary>
public class OpenMKCharacterUnlock
{
    private OpenMKCharacterUnlock() { }

    public OpenMKCharacterUnlock(Guid userId, Guid characterId, ITimeProvider timeProvider)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        CharacterId = characterId;
        UnlockedAt = timeProvider.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public Guid CharacterId { get; private set; }
    public DateTime UnlockedAt { get; private set; }
}
