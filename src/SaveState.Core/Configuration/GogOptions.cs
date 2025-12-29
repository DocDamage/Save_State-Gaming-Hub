namespace SaveState.Core.Configuration;

public class GogOptions
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // In a real app, this would be an OAuth token
}
