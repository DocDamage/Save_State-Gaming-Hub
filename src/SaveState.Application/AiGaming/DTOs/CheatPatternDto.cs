using SaveState.Core.Common.Enums;

namespace SaveState.Application.AiGaming.DTOs;

public class CheatPatternDto
{
    public string PatternId { get; set; } = string.Empty;
    public string GameTitle { get; set; } = string.Empty;
    public CheatType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public IReadOnlyList<long> AffectedAddresses { get; set; } = Array.Empty<long>();
    public double DetectionThreshold { get; set; }
    public DateTime LastDetected { get; set; }
    public int DetectionCount { get; set; }
}
