namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// VR input response data.
/// </summary>
public class VrInputResponse
{
    public string SessionId { get; set; } = default!;
    public bool IsValid { get; set; } = default!;
    public VrInput ProcessedInput { get; set; } = default!;
    public VrGameStateUpdate GameStateUpdate { get; set; } = default!;
    public VrFeedback Feedback { get; set; } = default!;
}

/// <summary>
/// VR game state update data.
/// </summary>
public class VrGameStateUpdate
{
    public bool PositionChanged { get; set; } = default!;
    public bool RotationChanged { get; set; } = default!;
    public bool ActionTriggered { get; set; } = default!;
}

/// <summary>
/// VR feedback data.
/// </summary>
public class VrFeedback
{
    public bool HapticFeedback { get; set; } = default!;
    public bool AudioFeedback { get; set; } = default!;
    public bool VisualFeedback { get; set; } = default!;
}
