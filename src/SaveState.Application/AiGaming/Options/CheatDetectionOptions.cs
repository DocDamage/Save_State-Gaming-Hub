namespace SaveState.Application.AiGaming.Options;

public class CheatDetectionOptions
{
    public double Sensitivity { get; set; } = 0.8;
    public bool EnablePatternAnalysis { get; set; } = true;
    public bool EnableMemoryAnalysis { get; set; } = true;
    public TimeSpan AnalysisInterval { get; set; } = TimeSpan.FromSeconds(1);
    public int MaxAddressesToAnalyze { get; set; } = 1000;
}
