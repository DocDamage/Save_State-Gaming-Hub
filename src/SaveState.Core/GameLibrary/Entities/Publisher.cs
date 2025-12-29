using SaveState.Core.Common.Base;

namespace SaveState.Core.GameLibrary.Entities;

public class Publisher : EntityBase
{
    public string Name { get; private set; } = string.Empty;
    public string? Country { get; private set; }
    public DateTime? FoundedDate { get; private set; }
    public string? Website { get; private set; }
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    protected Publisher() { } // EF Core

    public Publisher(string name, string? country = null, DateTime? foundedDate = null)
    {
        Name = Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Country = country;
        FoundedDate = foundedDate;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string? country, DateTime? foundedDate, string? website, string? description)
    {
        Country = country;
        FoundedDate = foundedDate;
        Website = website;
        Description = description;
    }
}
