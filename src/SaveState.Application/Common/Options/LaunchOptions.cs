namespace SaveState.Application.Common.Options;

public class LaunchOptions
{
    public string? Arguments { get; set; }
    public string? WorkingDirectory { get; set; }
    public bool WaitForExit { get; set; } = false;
    public TimeSpan? Timeout { get; set; }
}
