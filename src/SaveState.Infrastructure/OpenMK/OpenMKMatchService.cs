using Microsoft.Extensions.Logging;
using SaveState.Core.Common;
using SaveState.Core.Common.Services;
using SaveState.Core.OpenMK.Entities;
using SaveState.Core.OpenMK.Repositories;
using SaveState.Core.OpenMK.Services;
using SaveState.Core.OpenMK.ValueObjects;
using MatchStateRecord = SaveState.Core.OpenMK.Services.OpenMKMatchState;
using OpenMKMatchStateEntity = SaveState.Core.OpenMK.Entities.OpenMKMatchState;

namespace SaveState.Infrastructure.OpenMK;

/// <summary>
/// Implementation of OpenMK match service for gameplay mechanics.
/// </summary>
public partial class OpenMKMatchService : IOpenMKMatchService
{
    private readonly ILogger<OpenMKMatchService> _logger;
    private readonly IOpenMKMatchStateRepository _matchStateRepository;
    private readonly ITimeProvider _timeProvider;
    private readonly Dictionary<Guid, OpenMKMatch> _activeMatches = new();

    public OpenMKMatchService(
        IOpenMKMatchStateRepository matchStateRepository,
        ILogger<OpenMKMatchService> logger,
        ITimeProvider timeProvider)
    {
        _matchStateRepository = matchStateRepository;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public async Task<Result<OpenMKMatch>> StartMatchAsync(
        OpenMKCharacter player1Character,
        OpenMKCharacter player2Character,
        OpenMKMatchType matchType,
        CancellationToken ct = default)
    {
        try
        {
            var matchId = Guid.NewGuid();
            var match = new OpenMKMatch(
                Id: matchId,
                Player1Character: player1Character,
                Player2Character: player2Character,
                MatchType: matchType,
                StartedAt: _timeProvider.UtcNow,
                CurrentState: new MatchStateRecord(
                    RoundNumber: 1,
                    Player1Health: 100,
                    Player2Health: 100,
                    Player1SuperBar: 0,
                    Player2SuperBar: 0,
                    Player1Wins: 0,
                    Player2Wins: 0,
                    RoundTimeRemaining: TimeSpan.FromSeconds(99),
                    Phase: OpenMKMatchPhase.Starting));

            _activeMatches[matchId] = match;
            await _matchStateRepository.AddAsync(BuildMatchStateEntity(match), ct);

            LogMatchStarted(_logger, matchId, player1Character.DisplayName, player2Character.DisplayName, matchType);
            return Result.Success(match);
        }
        catch (Exception ex)
        {
            LogStartMatchFailed(_logger, player1Character.DisplayName, player2Character.DisplayName, ex);
            return Result.Failure<OpenMKMatch>($"Failed to start match: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKMoveResult>> ExecuteSpecialMoveAsync(
        Guid matchId,
        Guid characterId,
        string specialMoveName,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeMatches.TryGetValue(matchId, out var match))
            {
                return Result.Failure<OpenMKMoveResult>("Match not found", ErrorType.NotFound);
            }

            var canExecute = await CanExecuteMoveAsync(matchId, characterId, specialMoveName, ct);
            if (!canExecute.IsSuccess || !canExecute.Value)
            {
                return Result.Failure<OpenMKMoveResult>("Cannot execute special move in current state", ErrorType.Validation);
            }

            // Find the special move
            var character = characterId == match.Player1Character.Id ? match.Player1Character : match.Player2Character;
            var specialMove = character.SpecialMoves.FirstOrDefault(sm => sm.Name == specialMoveName);
            if (specialMove == null)
            {
                return Result.Failure<OpenMKMoveResult>("Special move not found", ErrorType.NotFound);
            }

            // Simulate move execution
            var result = new OpenMKMoveResult(
                Success: true,
                DamageDealt: specialMove.Damage,
                SuperBarGained: 10,
                AnimationPlayed: specialMove.AnimationName,
                SoundPlayed: specialMove.SoundEffect,
                ComboExtended: true);

            LogSpecialMoveExecuted(_logger, matchId, characterId, specialMoveName);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogExecuteSpecialMoveFailed(_logger, matchId, characterId, specialMoveName, ex);
            return Result.Failure<OpenMKMoveResult>($"Failed to execute special move: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKFinisherResult>> ExecuteFatalityAsync(
        Guid matchId,
        Guid winnerCharacterId,
        string fatalityName,
        CancellationToken ct = default)
    {
        try
        {
            if (!_activeMatches.TryGetValue(matchId, out var match))
            {
                return Result.Failure<OpenMKFinisherResult>("Match not found", ErrorType.NotFound);
            }

            if (match.CurrentState.Phase != OpenMKMatchPhase.Finisher)
            {
                return Result.Failure<OpenMKFinisherResult>("Not in finisher phase", ErrorType.Validation);
            }

            var winner = winnerCharacterId == match.Player1Character.Id ? match.Player1Character : match.Player2Character;
            var fatality = winner.Fatalities.FirstOrDefault(f => f.Name == fatalityName);
            if (fatality == null)
            {
                return Result.Failure<OpenMKFinisherResult>("Fatality not found", ErrorType.NotFound);
            }

            var finisherType = fatality.Type switch
            {
                OpenMKFatalityType.Standard => OpenMKFinisherType.Fatality,
                OpenMKFatalityType.Brutality => OpenMKFinisherType.Brutality,
                OpenMKFatalityType.Friendship => OpenMKFinisherType.Friendship,
                OpenMKFatalityType.Babality => OpenMKFinisherType.Babality,
                _ => OpenMKFinisherType.Fatality
            };

            var result = new OpenMKFinisherResult(
                Success: true,
                FinisherType: finisherType,
                AnimationPlayed: fatality.AnimationSequence,
                SoundPlayed: fatality.SoundEffect,
                VoiceLinePlayed: fatality.VoiceLine,
                MatchEnded: true);

            LogFatalityExecuted(_logger, matchId, winnerCharacterId, fatalityName);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogExecuteFatalityFailed(_logger, matchId, winnerCharacterId, fatalityName, ex);
            return Result.Failure<OpenMKFinisherResult>($"Failed to execute fatality: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<MatchStateRecord>> GetMatchStateAsync(Guid matchId, CancellationToken ct = default)
    {
        try
        {
            if (!_activeMatches.TryGetValue(matchId, out var match))
            {
                return Result.Failure<MatchStateRecord>("Match not found", ErrorType.NotFound);
            }

            return Result.Success(match.CurrentState);
        }
        catch (Exception ex)
        {
            LogGetMatchStateFailed(_logger, matchId, ex);
            return Result.Failure<MatchStateRecord>($"Failed to get match state: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> UpdateSuperBarAsync(Guid matchId, Guid characterId, int changeAmount, CancellationToken ct = default)
    {
        try
        {
            if (!_activeMatches.TryGetValue(matchId, out var match))
            {
                return Result.Failure("Match not found", ErrorType.NotFound);
            }

            var updatedState = UpdateSuperBar(match, characterId, changeAmount);
            var updatedMatch = match with { CurrentState = updatedState };
            _activeMatches[matchId] = updatedMatch;
            await PersistMatchStateAsync(updatedMatch, ct);

            LogSuperBarUpdated(_logger, matchId, characterId, changeAmount);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogUpdateSuperBarFailed(_logger, matchId, characterId, ex);
            return Result.Failure($"Failed to update super bar: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result> RecordComboAsync(Guid matchId, Guid characterId, int comboLength, CancellationToken ct = default)
    {
        try
        {
            if (!_activeMatches.TryGetValue(matchId, out var match))
            {
                return Result.Failure("Match not found", ErrorType.NotFound);
            }

            LogComboRecorded(_logger, matchId, characterId, comboLength);
            return Result.Success();
        }
        catch (Exception ex)
        {
            LogRecordComboFailed(_logger, matchId, characterId, ex);
            return Result.Failure($"Failed to record combo: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKRoundResult>> EndRoundAsync(Guid matchId, CancellationToken ct = default)
    {
        try
        {
            if (!_activeMatches.TryGetValue(matchId, out var match))
            {
                return Result.Failure<OpenMKRoundResult>("Match not found", ErrorType.NotFound);
            }

            // Determine winner (simplified - in real implementation would track health)
            var winnerId = match.Player1Character.Id; // Simplified
            var loserId = match.Player2Character.Id;

            var result = new OpenMKRoundResult(
                RoundNumber: match.CurrentState.RoundNumber,
                WinnerCharacterId: winnerId,
                LoserCharacterId: loserId,
                EndReason: OpenMKRoundEndReason.HealthDepleted,
                RoundDuration: TimeSpan.FromSeconds(45));

            // Transition to finisher phase
            var updatedMatch = match with
            {
                CurrentState = match.CurrentState with { Phase = OpenMKMatchPhase.Finisher }
            };
            _activeMatches[matchId] = updatedMatch;
            await PersistMatchStateAsync(updatedMatch, ct);

            LogRoundEnded(_logger, matchId, result.RoundNumber, winnerId);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogEndRoundFailed(_logger, matchId, ex);
            return Result.Failure<OpenMKRoundResult>($"Failed to end round: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<OpenMKMatchResult>> EndMatchAsync(Guid matchId, CancellationToken ct = default)
    {
        try
        {
            if (!_activeMatches.TryGetValue(matchId, out var match))
            {
                return Result.Failure<OpenMKMatchResult>("Match not found", ErrorType.NotFound);
            }

            var winnerId = match.Player1Character.Id; // Simplified
            var loserId = match.Player2Character.Id;

            var result = new OpenMKMatchResult(
                WinnerCharacterId: winnerId,
                LoserCharacterId: loserId,
                WinnerRoundsWon: 2,
                LoserRoundsWon: 0,
                TotalMatchDuration: _timeProvider.UtcNow - match.StartedAt,
                Rounds: new List<OpenMKRoundResult>(),
                Statistics: new List<OpenMKMatchStats>());

            _activeMatches.Remove(matchId);
            await _matchStateRepository.DeleteAsync(matchId, ct);

            LogMatchEnded(_logger, matchId, winnerId);
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            LogEndMatchFailed(_logger, matchId, ex);
            return Result.Failure<OpenMKMatchResult>($"Failed to end match: {ex.Message}", ErrorType.Internal);
        }
    }

    public async Task<Result<bool>> CanExecuteMoveAsync(Guid matchId, Guid characterId, string moveName, CancellationToken ct = default)
    {
        try
        {
            if (!_activeMatches.TryGetValue(matchId, out var match))
            {
                return Result.Failure<bool>("Match not found", ErrorType.NotFound);
            }

            if (match.CurrentState.Phase != OpenMKMatchPhase.Fighting)
            {
                return Result.Success(false);
            }

            // Additional validation logic would go here
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            LogCanExecuteMoveFailed(_logger, matchId, characterId, moveName, ex);
            return Result.Failure<bool>($"Failed to check move execution: {ex.Message}", ErrorType.Internal);
        }
    }

    #region LoggerMessage Definitions

    [LoggerMessage(Level = LogLevel.Information, Message = "Started OpenMK match {MatchId} between {Player1} vs {Player2} ({MatchType})")]
    private static partial void LogMatchStarted(ILogger logger, Guid matchId, string player1, string player2, OpenMKMatchType matchType);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to start match between {Player1} and {Player2}")]
    private static partial void LogStartMatchFailed(ILogger logger, string player1, string player2, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Executed special move '{MoveName}' in match {MatchId} by character {CharacterId}")]
    private static partial void LogSpecialMoveExecuted(ILogger logger, Guid matchId, Guid characterId, string moveName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to execute special move '{MoveName}' in match {MatchId} by character {CharacterId}")]
    private static partial void LogExecuteSpecialMoveFailed(ILogger logger, Guid matchId, Guid characterId, string moveName, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Executed fatality '{FatalityName}' in match {MatchId} by character {CharacterId}")]
    private static partial void LogFatalityExecuted(ILogger logger, Guid matchId, Guid characterId, string fatalityName);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to execute fatality '{FatalityName}' in match {MatchId} by character {CharacterId}")]
    private static partial void LogExecuteFatalityFailed(ILogger logger, Guid matchId, Guid characterId, string fatalityName, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to get match state for match {MatchId}")]
    private static partial void LogGetMatchStateFailed(ILogger logger, Guid matchId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated super bar by {ChangeAmount} for character {CharacterId} in match {MatchId}")]
    private static partial void LogSuperBarUpdated(ILogger logger, Guid matchId, Guid characterId, int changeAmount);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to update super bar for character {CharacterId} in match {MatchId}")]
    private static partial void LogUpdateSuperBarFailed(ILogger logger, Guid matchId, Guid characterId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Recorded combo of length {ComboLength} for character {CharacterId} in match {MatchId}")]
    private static partial void LogComboRecorded(ILogger logger, Guid matchId, Guid characterId, int comboLength);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to record combo for character {CharacterId} in match {MatchId}")]
    private static partial void LogRecordComboFailed(ILogger logger, Guid matchId, Guid characterId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ended round {RoundNumber} in match {MatchId}, winner: {WinnerId}")]
    private static partial void LogRoundEnded(ILogger logger, Guid matchId, int roundNumber, Guid winnerId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to end round in match {MatchId}")]
    private static partial void LogEndRoundFailed(ILogger logger, Guid matchId, Exception ex);

    [LoggerMessage(Level = LogLevel.Information, Message = "Ended match {MatchId}, winner: {WinnerId}")]
    private static partial void LogMatchEnded(ILogger logger, Guid matchId, Guid winnerId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to end match {MatchId}")]
    private static partial void LogEndMatchFailed(ILogger logger, Guid matchId, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to check move execution for '{MoveName}' by character {CharacterId} in match {MatchId}")]
    private static partial void LogCanExecuteMoveFailed(ILogger logger, Guid matchId, Guid characterId, string moveName, Exception ex);

    #endregion

    private static MatchStateRecord UpdateSuperBar(OpenMKMatch match, Guid characterId, int changeAmount)
    {
        if (characterId == match.Player1Character.Id)
        {
            var newValue = Math.Clamp(match.CurrentState.Player1SuperBar + changeAmount, 0, 100);
            return match.CurrentState with { Player1SuperBar = newValue };
        }

        if (characterId == match.Player2Character.Id)
        {
            var newValue = Math.Clamp(match.CurrentState.Player2SuperBar + changeAmount, 0, 100);
            return match.CurrentState with { Player2SuperBar = newValue };
        }

        return match.CurrentState;
    }

    private static OpenMKMatchStateEntity BuildMatchStateEntity(OpenMKMatch match)
    {
        var state = match.CurrentState;
        return new OpenMKMatchStateEntity(
            matchId: match.Id,
            player1CharacterId: match.Player1Character.Id,
            player2CharacterId: match.Player2Character.Id,
            roundNumber: state.RoundNumber,
            player1Health: state.Player1Health,
            player2Health: state.Player2Health,
            player1SuperBar: state.Player1SuperBar,
            player2SuperBar: state.Player2SuperBar,
            player1Wins: state.Player1Wins,
            player2Wins: state.Player2Wins,
            roundTimeRemaining: state.RoundTimeRemaining,
            phase: state.Phase);
    }

    private async Task PersistMatchStateAsync(OpenMKMatch match, CancellationToken ct)
    {
        var state = match.CurrentState;
        var existing = await _matchStateRepository.GetByMatchIdAsync(match.Id, ct);
        if (existing == null)
        {
            await _matchStateRepository.AddAsync(BuildMatchStateEntity(match), ct);
            return;
        }

        existing.UpdateState(
            roundNumber: state.RoundNumber,
            player1Health: state.Player1Health,
            player2Health: state.Player2Health,
            player1SuperBar: state.Player1SuperBar,
            player2SuperBar: state.Player2SuperBar,
            player1Wins: state.Player1Wins,
            player2Wins: state.Player2Wins,
            roundTimeRemaining: state.RoundTimeRemaining,
            phase: state.Phase);

        await _matchStateRepository.UpdateAsync(existing, ct);
    }
}
