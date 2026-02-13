using System;

namespace SaveState.Core.OpenMK.Entities;

/// <summary>
/// Stores OpenMK progression data for a user.
/// </summary>
public class OpenMKUserProgress
{
    private OpenMKUserProgress() { }

    public OpenMKUserProgress(Guid userId)
    {
        UserId = userId;
        Koins = 0;
        CreatedAt = DateTime.UtcNow;
        LastUpdatedAt = CreatedAt;
    }

    public Guid UserId { get; private set; }
    public int Koins { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    public void AddKoins(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Koins += amount;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public bool TrySpendKoins(int amount)
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
        LastUpdatedAt = DateTime.UtcNow;
        return true;
    }
}
