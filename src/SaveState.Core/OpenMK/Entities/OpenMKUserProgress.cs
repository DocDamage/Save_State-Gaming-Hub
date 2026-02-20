using System;

namespace SaveState.Core.OpenMK.Entities;

/// <summary>
/// Stores OpenMK progression data for a user.
/// </summary>
public class OpenMKUserProgress
{
    private OpenMKUserProgress() { }

    public OpenMKUserProgress(Guid userId, DateTime? createdAt = null)
    {
        UserId = userId;
        Koins = 0;
        CreatedAt = createdAt ?? DateTime.UtcNow;
        LastUpdatedAt = CreatedAt;
    }

    public Guid UserId { get; private set; }
    public int Koins { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    public void AddKoins(int amount, DateTime? updatedAt = null)
    {
        if (amount <= 0)
        {
            return;
        }

        Koins += amount;
        LastUpdatedAt = updatedAt ?? DateTime.UtcNow;
    }

    public bool TrySpendKoins(int amount, DateTime? updatedAt = null)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (Koins < amount)
        {
            return false;
        }

        Koins -= amount;
        LastUpdatedAt = updatedAt ?? DateTime.UtcNow;
        return true;
    }
}
