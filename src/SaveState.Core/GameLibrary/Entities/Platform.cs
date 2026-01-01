using SaveState.Core.Common.Base;
using SaveState.Core.GameLibrary.ValueObjects;
using SaveState.Core.GameLibrary.Enums;

namespace SaveState.Core.GameLibrary.Entities;

public class Platform : EntityBase
{
    public PlatformName Name { get; private set; } = null!;
    public PlatformShortName ShortName { get; private set; } = null!;
    public PlatformType Type { get; private set; }
    public string? Manufacturer { get; private set; }
    public DateTime? ReleasedAt { get; private set; }
    public string? Description { get; private set; }

    protected Platform() { } // EF Core

    public Platform(PlatformName name, PlatformShortName shortName, PlatformType type)
    {
        Name = Guard.Against.Null(name, nameof(name));
        ShortName = Guard.Against.Null(shortName, nameof(shortName));
        Type = type;
    }

    public void SetManufacturer(string manufacturer)
    {
        Manufacturer = Guard.Against.NullOrWhiteSpace(manufacturer, nameof(manufacturer));
    }

    public void SetReleaseDate(DateTime releaseDate)
    {
        ReleasedAt = releaseDate;
    }

    public void SetDescription(string description)
    {
        Description = Guard.Against.NullOrWhiteSpace(description, nameof(description));
    }
}
