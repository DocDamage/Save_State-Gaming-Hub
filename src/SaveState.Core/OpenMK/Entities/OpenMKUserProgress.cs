using SaveState.Core.Common.Services;

namespace SaveState.Core.OpenMK.Entities;

/// <summary>
/// Stores OpenMK progression data for a user.
/// </summary>
public class OpenMKUserProgress
{
    private OpenMKUserProgress() { }

    public OpenMKUserProgress(Guid userId, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        UserId = userId;
        Koins = 0;
        CreatedAt = timeProvider.UtcNow;
        LastUpdatedAt = CreatedAt;
    }

    public OpenMKUserProgress(Guid userId, DateTime createdAt)
    {
        UserId = userId;
        Koins = 0;
        CreatedAt = createdAt;
        LastUpdatedAt = CreatedAt;
    }



    public Guid UserId { get; private set; }
    public int Koins { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    public void AddKoins(int amount, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        if (amount <= 0)
        {
            return;
        }

        Koins += amount;
        LastUpdatedAt = timeProvider.UtcNow;
    }

    public void AddKoins(int amount, DateTime updatedAt)
    {
        if (amount <= 0)
        {
            return;
        }

        Koins += amount;
        LastUpdatedAt = updatedAt;
    }



    public bool TrySpendKoins(int amount, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        if (amount <= 0)
        {
            return true;
        }

        if (Koins < amount)
        {
            return false;
        }

        Koins -= amount;
        LastUpdatedAt = timeProvider.UtcNow;
        return true;
    }

    public bool TrySpendKoins(int amount, DateTime updatedAt)
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
        LastUpdatedAt = updatedAt;
        return true;
    }


}
