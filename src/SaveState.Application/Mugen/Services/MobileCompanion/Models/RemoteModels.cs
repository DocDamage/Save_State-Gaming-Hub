namespace SaveState.Application.Mugen.Services;

/// <summary>
/// Remote command data.
/// </summary>
public class MobileCompanionServiceRemoteCommand
{
    public string CommandId { get; set; } = default!;
    public MobileCompanionServiceCommandType MobileCompanionServiceCommandType { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
    public DateTime Timestamp { get; set; } = default!;
}

/// <summary>
/// Quick action data.
/// </summary>
public class MobileCompanionServiceQuickAction
{
    public string ActionId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public string Icon { get; set; } = default!;
    public MobileCompanionServiceQuickActionType ActionType { get; set; } = default!;
    public IReadOnlyDictionary<string, object> Parameters { get; set; } = default!;
}

/// <summary>
/// Control action data.
/// </summary>
public class MobileCompanionServiceControlAction
{
    public string ActionId { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Description { get; set; } = default!;
    public bool RequiresConfirmation { get; set; }
    public IReadOnlyDictionary<string, object> DefaultParameters { get; set; } = default!;
}
