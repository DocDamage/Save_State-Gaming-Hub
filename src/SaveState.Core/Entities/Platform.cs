namespace SaveState.Core.Entities;

public class Platform
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Icon { get; set; }
    public string? DefaultEmulator { get; set; }
    public string? Specification { get; set; }

    public ICollection<Game> Games { get; set; } = new List<Game>();
}
