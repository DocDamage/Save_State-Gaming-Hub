using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;
using SaveState.Core.GameLibrary.Enums;
using System.Text.Json;

namespace SaveState.Core.GameLibrary.Entities;

public class VirtualCollection : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Icon { get; private set; }
    public CollectionType Type { get; private set; }
    public string? FilterExpression { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsSystemCollection { get; private set; }
    public ICollection<VirtualCollectionGame> Games { get; private set; } = new List<VirtualCollectionGame>();

    private VirtualCollection() { }

    public static VirtualCollection CreateManual(string name, string? icon = null)
    {
        return new VirtualCollection
        {
            Id = Guid.NewGuid(),
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Icon = icon,
            Type = CollectionType.Manual,
            IsSystemCollection = false
        };
    }

    public static VirtualCollection CreateSmart(string name, CollectionFilter filter, string? icon = null)
    {
        return new VirtualCollection
        {
            Id = Guid.NewGuid(),
            Name = name,
            Icon = icon,
            Type = CollectionType.Smart,
            FilterExpression = JsonSerializer.Serialize(filter),
            IsSystemCollection = false
        };
    }

    public static VirtualCollection CreateSystemCollection(string name, CollectionFilter filter, string? icon = null)
    {
        return new VirtualCollection
        {
            Id = Guid.NewGuid(),
            Name = name,
            Icon = icon,
            Type = CollectionType.Smart,
            FilterExpression = JsonSerializer.Serialize(filter),
            IsSystemCollection = true
        };
    }

    public CollectionFilter? GetFilter() =>
        FilterExpression is null ? null : JsonSerializer.Deserialize<CollectionFilter>(FilterExpression);

    public void UpdateFilter(CollectionFilter filter) =>
        FilterExpression = JsonSerializer.Serialize(filter);

    public void Rename(string name) => Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
    public void SetIcon(string? icon) => Icon = icon;
    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;
}

public enum CollectionType
{
    Manual,
    Smart,
    RecentlyPlayed,
    NeverPlayed,
    ShortGames,
    Favorites,
    ByPlatform,
    ByGenre
}

public sealed record CollectionFilter(
    TimeSpan? MaxPlaytime = null,
    TimeSpan? MinPlaytime = null,
    int? MaxDaysSinceLastPlayed = null,
    string? PlatformName = null,
    string? Genre = null,
    GameStatus? Status = null,
    string? Tag = null,
    bool? HasAchievements = null,
    int? MinRating = null,
    int? MinReleaseYear = null,
    int? MaxReleaseYear = null,
    bool? IsCompleted = null,
    bool? IsInBacklog = null);

public class VirtualCollectionGame
{
    public Guid CollectionId { get; set; }
    public VirtualCollection? Collection { get; set; } // Set by EF Core
    public Guid GameId { get; set; }
    public Game? Game { get; set; } // Set by EF Core
    public int SortOrder { get; set; }
    public DateTime AddedAt { get; set; }

    private VirtualCollectionGame() { }

    public static VirtualCollectionGame Create(Guid collectionId, Guid gameId, ITimeProvider timeProvider, int sortOrder = 0)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        return new VirtualCollectionGame
        {
            CollectionId = collectionId,
            GameId = gameId,
            // Collection and Game navigation properties are set by EF Core
            SortOrder = sortOrder,
            AddedAt = timeProvider.UtcNow
        };
    }

    [Obsolete("Use Create(Guid, Guid, ITimeProvider, int) instead")]
    public static VirtualCollectionGame Create(Guid collectionId, Guid gameId, int sortOrder = 0)
    {
        return new VirtualCollectionGame
        {
            CollectionId = collectionId,
            GameId = gameId,
            // Collection and Game navigation properties are set by EF Core
            SortOrder = sortOrder,
            AddedAt = SystemTimeProvider.Instance.UtcNow
        };
    }
}
