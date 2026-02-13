using Ardalis.GuardClauses;
using SaveState.Core.Common.Base;
using SaveState.Core.Performance.ValueObjects;

namespace SaveState.Core.Performance.Entities;

/// <summary>
/// Represents a monitored memory address with real-time value tracking.
/// </summary>
public class MemoryWatch : EntityBase
{
    /// <summary>
    /// Gets the game this watch belongs to.
    /// </summary>
    public Guid GameId { get; private set; }

    /// <summary>
    /// Gets the user-friendly label for this watch.
    /// </summary>
    public string Label { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the memory address being watched.
    /// </summary>
    public MemoryAddress Address { get; private set; } = null!;

    /// <summary>
    /// Gets the data type of the value.
    /// </summary>
    public MemoryDataType DataType { get; private set; }

    /// <summary>
    /// Gets the current value (serialized as JSON).
    /// </summary>
    public string? CurrentValue { get; private set; }

    /// <summary>
    /// Gets the previous value for change detection.
    /// </summary>
    public string? PreviousValue { get; private set; }

    /// <summary>
    /// Gets when the value last changed.
    /// </summary>
    public DateTime? LastChangedAt { get; private set; }

    /// <summary>
    /// Gets whether this watch is currently active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets whether the value should be frozen (read-only flag for now).
    /// </summary>
    public bool IsFrozen { get; private set; }

    /// <summary>
    /// Gets the description/notes for this watch.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Gets the number of times the value has changed.
    /// </summary>
    public int ChangeCount { get; private set; }

    /// <summary>
    /// Gets when this watch was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets when this watch was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    // EF Core constructor
    protected MemoryWatch() { }

    /// <summary>
    /// Creates a new memory watch.
    /// </summary>
    public static MemoryWatch Create(
        Guid gameId,
        string label,
        MemoryAddress address,
        MemoryDataType dataType,
        string? description = null)
    {
        return new MemoryWatch
        {
            Id = Guid.NewGuid(),
            GameId = Guard.Against.Default(gameId, nameof(gameId)),
            Label = Guard.Against.NullOrWhiteSpace(label, nameof(label)),
            Address = Guard.Against.Null(address, nameof(address)),
            DataType = dataType,
            Description = description,
            IsActive = true,
            IsFrozen = false,
            ChangeCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Updates the current value and tracks changes.
    /// </summary>
    public void UpdateValue(string? newValue)
    {
        if (CurrentValue != newValue)
        {
            PreviousValue = CurrentValue;
            CurrentValue = newValue;
            LastChangedAt = DateTime.UtcNow;
            ChangeCount++;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Toggles the frozen state.
    /// </summary>
    public void ToggleFreeze()
    {
        IsFrozen = !IsFrozen;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Activates the watch.
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivates the watch.
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the label.
    /// </summary>
    public void UpdateLabel(string label)
    {
        Label = Guard.Against.NullOrWhiteSpace(label, nameof(label));
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates the description.
    /// </summary>
    public void UpdateDescription(string? description)
    {
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
