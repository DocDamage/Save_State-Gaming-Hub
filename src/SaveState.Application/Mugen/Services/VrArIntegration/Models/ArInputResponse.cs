namespace SaveState.Application.Mugen.Services.VrArIntegration;

/// <summary>
/// AR input response data.
/// </summary>
public class ArInputResponse
{
    public string SessionId { get; set; } = default!;
    public bool IsValid { get; set; } = default!;
    public ArInput ProcessedInput { get; set; } = default!;
    public ArGameStateUpdate GameStateUpdate { get; set; } = default!;
    public ArFeedback Feedback { get; set; } = default!;
}

/// <summary>
/// AR game state update data.
/// </summary>
public class ArGameStateUpdate
{
    public bool SurfaceDetected { get; set; } = default!;
    public bool ObjectPlaced { get; set; } = default!;
    public bool GestureRecognized { get; set; } = default!;
}

/// <summary>
/// AR feedback data.
/// </summary>
public class ArFeedback
{
    public bool VisualIndicator { get; set; } = default!;
    public bool AudioConfirmation { get; set; } = default!;
    public bool HapticFeedback { get; set; } = default!;
}
