namespace SaveState.Application.Mugen.Models.BalanceTuning;

/// <summary>
/// Balance test results.
/// </summary>
public class TestResults
{
    public int TestsRun { get; set; }
    public int TestsPassed { get; set; }
    public List<string> Failures { get; set; } = default!;
    public int PassedTests { get; set; } = default!;
    public int FailedTests { get; set; } = default!;
    public TimeSpan TestDuration { get; set; } = default!;
    public DateTime TestTimestamp { get; set; } = default!;
}
