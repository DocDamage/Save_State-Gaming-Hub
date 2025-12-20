namespace SaveState.Core.Entities;

public class Emulator
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public bool IsDefault { get; set; }
    
    // Supported platforms (comma-separated platform names)
    public string SupportedPlatforms { get; set; } = string.Empty;
    
    // ROM file extensions this emulator handles (e.g., ".nes,.sfc,.smc")
    public string SupportedExtensions { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
