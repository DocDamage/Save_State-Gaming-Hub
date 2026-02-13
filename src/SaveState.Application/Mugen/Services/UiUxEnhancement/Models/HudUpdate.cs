namespace SaveState.Application.Mugen.Services.UiUxEnhancement;

/// <summary>
/// HUD update data.
/// </summary>
public class HudUpdate
{
    public string SessionId { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
    public IReadOnlyList<HudElementUpdate> ElementUpdates { get; set; } = default!;
    public IReadOnlyList<LayoutAdjustment> LayoutAdjustments { get; set; } = default!;
    public IReadOnlyList<VisualEffect> VisualEffects { get; set; } = default!;
    public IReadOnlyList<PerformanceIndicator> PerformanceIndicators { get; set; } = default!;
}

/// <summary>
/// HUD element update data.
/// </summary>
public class HudElementUpdate
{
    public string ElementId { get; set; } = default!;
    public object Value { get; set; } = default!;
    public ElementAnimation Animation { get; set; } = default!;
}

/// <summary>
/// Layout adjustment data.
/// </summary>
public class LayoutAdjustment
{
    public string ElementId { get; set; } = default!;
    public float NewX { get; set; } = default!;
    public float NewY { get; set; } = default!;
}

/// <summary>
/// Element animation data.
/// </summary>
public class ElementAnimation
{
    public string Type { get; set; } = default!;
    public float Duration { get; set; } = default!;
    public string Easing { get; set; } = default!;
}

/// <summary>
/// Performance indicator data.
/// </summary>
public class PerformanceIndicator
{
    public string Type { get; set; } = default!;
    public float Value { get; set; } = default!;
    public string Color { get; set; } = default!;
}
