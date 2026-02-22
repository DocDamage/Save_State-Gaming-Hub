using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;
using System.Text.Json;

namespace SaveState.Core.Input.Entities;

public class ControllerProfile : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public Guid? GameId { get; private set; }
    public string? ControllerId { get; private set; }
    public ControllerType Type { get; private set; }
    public string MappingsJson { get; private set; } = "{}";
    public bool IsDefault { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private ControllerProfile() { }

    public static ControllerProfile Create(string name, ControllerType type, ITimeProvider timeProvider, Guid? gameId = null)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        return new ControllerProfile
        {
            Id = Guid.NewGuid(),
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Type = type,
            GameId = gameId,
            CreatedAt = timeProvider.UtcNow,
            IsDefault = false
        };
    }

    public static ControllerProfile Create(string name, ControllerType type, DateTime createdAt, Guid? gameId = null)
    {
        return new ControllerProfile
        {
            Id = Guid.NewGuid(),
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Type = type,
            GameId = gameId,
            CreatedAt = createdAt,
            IsDefault = false
        };
    }

    [Obsolete("Use Create(string, ControllerType, ITimeProvider, Guid?) or Create(string, ControllerType, DateTime, Guid?) instead")]
    public static ControllerProfile Create(string name, ControllerType type, Guid? gameId = null)
    {
        return new ControllerProfile
        {
            Id = Guid.NewGuid(),
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Type = type,
            GameId = gameId,
            CreatedAt = SystemTimeProvider.Instance.UtcNow,
            IsDefault = false
        };
    }

    public IReadOnlyDictionary<string, string> GetMappings() =>
        JsonSerializer.Deserialize<Dictionary<string, string>>(MappingsJson) ?? new Dictionary<string, string>();

    public void SetMappings(IReadOnlyDictionary<string, string> mappings) =>
        MappingsJson = JsonSerializer.Serialize(mappings);

    public void SetAsDefault() => IsDefault = true;
    public void ClearDefault() => IsDefault = false;

    public void RecordUsage(ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        LastUsedAt = timeProvider.UtcNow;
    }

    public void RecordUsage(DateTime timestamp)
    {
        LastUsedAt = timestamp;
    }

    [Obsolete("Use RecordUsage(ITimeProvider) or RecordUsage(DateTime) instead")]
    public void RecordUsage() => LastUsedAt = SystemTimeProvider.Instance.UtcNow;

    public void Rename(string name) => Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
    public void SetControllerId(string? controllerId) => ControllerId = controllerId;
}

public enum ControllerType
{
    Xbox,
    PlayStation,
    Nintendo,
    Generic,
    Keyboard,
    SteamDeck
}