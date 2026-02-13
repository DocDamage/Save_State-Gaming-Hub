namespace SaveState.Application.Mugen.Services;

// Type aliases for backward compatibility
// These allow existing code to continue using the old names while we transition to the new structure

public class SpectatorModeServiceEnhancedSpectatorSession : SpectatorSession { }
public class SpectatorModeServiceSpectatorControl : SpectatorControl { }
public class SpectatorModeServiceMatchSpectatorData : MatchSpectatorData { }
public class SpectatorModeServiceSpectatorChatMessage : ChatMessage { }
public class SpectatorModeServiceMatchStatistics : MatchStatistics { }
public class SpectatorModeServiceMatchHighlights : MatchHighlights { }
public class SpectatorModeServiceSpectatorHighlightMoment : SpectatorHighlightMoment { }
public class SpectatorModeServiceSpectatorReplayRequest : ReplayRequest { }

public enum SpectatorModeServiceSpectatorMessageType
{
    Chat = SpectatorMessageType.Chat,
    System = SpectatorMessageType.System,
    Highlight = SpectatorMessageType.Highlight,
    Reaction = SpectatorMessageType.Reaction
}

public enum SpectatorModeServiceHighlightType
{
    Combo = HighlightType.Combo,
    Comeback = HighlightType.Comeback,
    Finisher = HighlightType.Finisher,
    SpecialMove = HighlightType.SpecialMove,
    Throw = HighlightType.Throw,
    Counter = HighlightType.Counter,
    Perfect = HighlightType.Perfect
}

public enum SpectatorModeServiceReplayStatus
{
    Queued = ReplayStatus.Queued,
    Processing = ReplayStatus.Processing,
    Ready = ReplayStatus.Ready,
    Failed = ReplayStatus.Failed
}

// Interface alias for backward compatibility
public interface SpectatorModeServiceISpectatorModeService : ISpectatorModeService { }
