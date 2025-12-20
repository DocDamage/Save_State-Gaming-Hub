using System.ComponentModel.DataAnnotations;

namespace SaveState.Core.Entities;

public class Game
{
    public Guid Id { get; set; }
    
    [Required]
    public string Title { get; set; } = string.Empty;
    public string? SortTitle { get; set; }
    public string? Description { get; set; }
    public DateTime? ReleaseDate { get; set; }
    
    public Guid PlatformId { get; set; }
    public Platform? Platform { get; set; }
    
    public string? CoverImage { get; set; }
    public string? BackgroundImage { get; set; }
    
    public TimeSpan PlayTime { get; set; }
    public string? Source { get; set; } // e.g., "Steam", "GOG"
    public string? SourceId { get; set; }
    
    public bool IsInstalled { get; set; }
    public string? InstallPath { get; set; }
    public string? LaunchCommand { get; set; }

    public CompletionStatus CompletionStatus { get; set; } = CompletionStatus.NotStarted;
    public int? Rating { get; set; } // 0-100 or 0-5
    public string? UserNotes { get; set; }

    public ICollection<GameImage> Images { get; set; } = new List<GameImage>();
    public ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
    public ICollection<PlaySession> PlaySessions { get; set; } = new List<PlaySession>();
    public ICollection<Collection> Collections { get; set; } = new List<Collection>();
    public ICollection<GameActivity> Activities { get; set; } = new List<GameActivity>();
}
