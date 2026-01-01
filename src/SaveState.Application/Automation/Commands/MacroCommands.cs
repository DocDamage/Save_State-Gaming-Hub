using MediatR;
using SaveState.Core.Automation.Services.DTOs;
using SaveState.Core.Common;

namespace SaveState.Application.Automation.Commands;

/// <summary>
/// Start macro recording.
/// </summary>
public record StartMacroRecordingCommand(
    Guid GameId,
    string Name,
    string Description,
    RecordingMode Mode = RecordingMode.Manual) : IRequest<Result<MacroRecordingSession>>;

/// <summary>
/// Stop macro recording.
/// </summary>
public record StopMacroRecordingCommand(
    Guid SessionId) : IRequest<Result<Macro>>;

/// <summary>
/// Cancel macro recording.
/// </summary>
public record CancelMacroRecordingCommand(
    Guid SessionId) : IRequest<Result>;

/// <summary>
/// Record an action in a macro.
/// </summary>
public record RecordMacroActionCommand(
    Guid SessionId,
    MacroAction Action) : IRequest<Result>;

/// <summary>
/// Pause macro recording.
/// </summary>
public record PauseMacroRecordingCommand(
    Guid SessionId) : IRequest<Result>;

/// <summary>
/// Resume macro recording.
/// </summary>
public record ResumeMacroRecordingCommand(
    Guid SessionId) : IRequest<Result>;

/// <summary>
/// Get macro recording status.
/// </summary>
public record GetMacroRecordingStatusCommand(
    Guid SessionId) : IRequest<Result<RecordingStatus>>;

/// <summary>
/// Get active recording sessions.
/// </summary>
public record GetActiveRecordingSessionsCommand : IRequest<Result<IReadOnlyList<MacroRecordingSession>>>;

/// <summary>
/// Start macro playback.
/// </summary>
public record StartMacroPlaybackCommand(
    Guid MacroId,
    MacroPlaybackConfig Config) : IRequest<Result<MacroPlaybackSession>>;

/// <summary>
/// Stop macro playback.
/// </summary>
public record StopMacroPlaybackCommand(
    Guid SessionId) : IRequest<Result>;

/// <summary>
/// Pause macro playback.
/// </summary>
public record PauseMacroPlaybackCommand(
    Guid SessionId) : IRequest<Result>;

/// <summary>
/// Resume macro playback.
/// </summary>
public record ResumeMacroPlaybackCommand(
    Guid SessionId) : IRequest<Result>;

/// <summary>
/// Get macro playback status.
/// </summary>
public record GetMacroPlaybackStatusCommand(
    Guid SessionId) : IRequest<Result<PlaybackStatus>>;

/// <summary>
/// Get active playback sessions.
/// </summary>
public record GetActivePlaybackSessionsCommand : IRequest<Result<IReadOnlyList<MacroPlaybackSession>>>;

/// <summary>
/// Validate macro for playback.
/// </summary>
public record ValidateMacroCommand(
    Guid MacroId) : IRequest<Result<MacroValidationResult>>;

/// <summary>
/// Get macro by ID.
/// </summary>
public record GetMacroCommand(
    Guid MacroId) : IRequest<Result<Macro>>;

/// <summary>
/// Get macros for a game.
/// </summary>
public record GetMacrosForGameCommand(
    Guid GameId) : IRequest<Result<IReadOnlyList<Macro>>>;

/// <summary>
/// Update macro metadata.
/// </summary>
public record UpdateMacroCommand(
    Guid MacroId,
    MacroMetadata Metadata) : IRequest<Result>;

/// <summary>
/// Delete macro.
/// </summary>
public record DeleteMacroCommand(
    Guid MacroId) : IRequest<Result>;

/// <summary>
/// Import macro.
/// </summary>
public record ImportMacroCommand(
    Stream MacroData,
    string Format) : IRequest<Result<Macro>>;

/// <summary>
/// Export macro.
/// </summary>
public record ExportMacroCommand(
    Guid MacroId,
    string Format) : IRequest<Result<Stream>>;

/// <summary>
/// Get macro categories.
/// </summary>
public record GetMacroCategoriesCommand : IRequest<Result<MacroCategories>>;

/// <summary>
/// Search macros.
/// </summary>
public record SearchMacrosCommand(
    string Query,
    MacroSearchFilters Filters) : IRequest<Result<IReadOnlyList<Macro>>>;

/// <summary>
/// Get macro statistics.
/// </summary>
public record GetMacroStatisticsCommand : IRequest<Result<MacroStatistics>>;