namespace SaveState.Core.GameLibrary;

using SaveState.Core.Common.Base;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// GameLibrary Bounded Context - handles game discovery, metadata management, and library organization.
/// This context owns entities related to games, platforms, genres, developers, and publishers.
/// </summary>
public static class GameLibraryContext
{
    public const string Name = "GameLibrary";

    // Entities owned by this context
    public static readonly Type[] Entities = {
        typeof(Game),
        typeof(Platform),
        typeof(Genre),
        typeof(Developer),
        typeof(Publisher)
    };

    // Domain services (interfaces defined in DomainServices folder)
    // IGameImportService - handles importing games from various sources
    // IMetadataEnrichmentService - enriches game metadata from external sources
    // IGameOrganizationService - organizes games by categories, tags, etc.

    // Value objects for this context (defined in ValueObjects folder)
    // GameTitle, PlatformName, PlatformShortName
}

// Platform entity is now fully implemented in Entities/Platform.cs

public class Genre : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private Genre() { }

    public static Genre Create(string name, string? description = null)
    {
        return new Genre
        {
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Description = description
        };
    }
}

public class Developer : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Country { get; private set; }
    public DateTime? FoundedDate { get; private set; }

    private Developer() { }

    public static Developer Create(string name, string? country = null, DateTime? foundedDate = null)
    {
        return new Developer
        {
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Country = country,
            FoundedDate = foundedDate
        };
    }
}

public class Publisher : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Country { get; private set; }
    public DateTime? FoundedDate { get; private set; }

    private Publisher() { }

    public static Publisher Create(string name, string? country = null, DateTime? foundedDate = null)
    {
        return new Publisher
        {
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Country = country,
            FoundedDate = foundedDate
        };
    }
}
