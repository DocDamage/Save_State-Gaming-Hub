namespace SaveState.Core.GameLibrary.Services.DTOs;

/// <summary>
/// Comprehensive game briefing with AI-generated content.
/// </summary>
public sealed record GameBriefing(
    Guid GameId,
    string LastSessionSummary,
    IReadOnlyList<string> CurrentObjectives,
    IReadOnlyList<string> Tips,
    TimeSpan TimeSinceLastPlayed);