namespace SaveState.Core.Entities;

public class Achievement
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public Game? Game { get; set; }
    
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Points { get; set; }
    public bool IsUnlocked { get; set; }
    public DateTime? UnlockedDate { get; set; }
    public string? IconUrl { get; set; }
}
