namespace SaveState.Application.Mugen.Models.Analytics;

/// <summary>
/// Dashboard data.
/// </summary>
public class DashboardData
{
    public string DashboardId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public IReadOnlyList<DashboardWidget> Widgets { get; set; } = default!;
    public TimeSpan RefreshInterval { get; set; } = default!;
    public DateTime GeneratedAt { get; set; } = default!;
}

/// <summary>
/// Dashboard widget data.
/// </summary>
public class DashboardWidget
{
    public string WidgetId { get; set; } = default!;
    public WidgetType Type { get; set; } = default!;
    public string Title { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Data { get; set; } = default!;
    public WidgetPosition Position { get; set; } = default!;
}

/// <summary>
/// Widget position data.
/// </summary>
public class WidgetPosition
{
    public int X { get; set; } = default!;
    public int Y { get; set; } = default!;
    public int Width { get; set; } = default!;
    public int Height { get; set; } = default!;
}

/// <summary>
/// Dashboard request.
/// </summary>
public class DashboardRequest
{
    public string UserId { get; set; } = default!;
    public IReadOnlyList<string> WidgetTypes { get; set; } = default!;
    public TimeSpan TimeRange { get; set; } = default!;
}
