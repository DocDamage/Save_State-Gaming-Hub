namespace SaveState.Core.AiGaming.Events;

using SaveState.Core.Common.Events;

/// <summary>
/// Event raised when cheating activity is detected in a process.
/// </summary>
public class CheatDetectedEvent : EventBase
{
    public int ProcessId { get; }
    public string CheatType { get; }
    public float Confidence { get; }
    public IReadOnlyList<long> AffectedAddresses { get; }

    public CheatDetectedEvent(int processId, string cheatType, float confidence, IEnumerable<long> affectedAddresses)
    {
        ProcessId = processId;
        CheatType = cheatType;
        Confidence = confidence;
        AffectedAddresses = affectedAddresses.ToList().AsReadOnly();
    }
}
