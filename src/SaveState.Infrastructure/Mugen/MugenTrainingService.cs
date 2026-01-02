namespace SaveState.Infrastructure.Mugen;

using SaveState.Core.Common;
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services;
using SaveState.Core.Mugen.ValueObjects;

/// <summary>
/// Implementation of the MUGEN training service.
/// Provides enhanced training mode features.
/// </summary>
public class MugenTrainingService : IMugenTrainingService
{
    private readonly SaveState.Core.Mugen.IMugenCharacterRepository _characterRepository;
    private readonly SaveState.Core.Mugen.IMugenTrainingRepository _trainingRepository;

    public MugenTrainingService(
        SaveState.Core.Mugen.IMugenCharacterRepository characterRepository,
        SaveState.Core.Mugen.IMugenTrainingRepository trainingRepository)
    {
        _characterRepository = characterRepository;
        _trainingRepository = trainingRepository;
    }

    public Task<Result> RecordDummyActionsAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            // Dummy action recording is a placeholder for future input recording feature.
            // When implemented, this will capture controller inputs for replay.

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Failed to record dummy actions: {ex.Message}"));
        }
    }

    public Task<Result> PlaybackDummyActionsAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            // Dummy action playback is a placeholder for future input replay feature.
            // When implemented, this will execute previously recorded input sequences.

            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure($"Failed to playback dummy actions: {ex.Message}"));
        }
    }

    public async Task<Result<TrainingSession>> StartSessionAsync(
        Guid characterId,
        TrainingConfig config,
        CancellationToken ct = default)
    {
        try
        {
            // Validate character exists
            var characterResult = await _characterRepository.GetByIdAsync(characterId, ct);
            if (characterResult.IsFailure)
                return Result<TrainingSession>.Failure("Character not found");

            // Validate dummy character exists
            var dummyResult = await _characterRepository.GetByIdAsync(config.DummyCharacterId, ct);
            if (dummyResult.IsFailure)
                return Result<TrainingSession>.Failure("Dummy character not found");

            // Create and persist training session
            var sessionEntity = MugenTrainingSession.Create(
                characterId,
                config.DummyCharacterId,
                Guid.NewGuid(), // Placeholder user ID - will be replaced when user context service is integrated
                TrainingSessionType.GeneralPractice); // Default training type - config mapping can be added later

            await _trainingRepository.AddAsync(sessionEntity, ct);

            // Convert to value object
            var session = new TrainingSession(
                sessionEntity.Id,
                characterId,
                config,
                sessionEntity.StartedAt);

            return Result<TrainingSession>.Success(session);
        }
        catch (Exception ex)
        {
            return Result<TrainingSession>.Failure($"Failed to start training session: {ex.Message}");
        }
    }

    public async Task<Result<TrainingStats>> EndSessionAsync(
        Guid sessionId,
        CancellationToken ct = default)
    {
        try
        {
            // Load session and calculate statistics
            var session = await _trainingRepository.GetByIdAsync(sessionId, ct);
            if (session is null)
                return Result<TrainingStats>.Failure("Training session not found");

            if (session.EndedAt.HasValue)
                return Result<TrainingStats>.Failure("Training session is already ended");

            // End the session
            session.End();
            await _trainingRepository.UpdateAsync(session, ct);

            // Calculate statistics from session data
            var maxComboHits = session.Recordings.Any()
                ? session.Recordings.Max(r => r.ComboHits)
                : 0;

            var maxComboDamage = session.Recordings.Any()
                ? session.Recordings.Max(r => r.ComboDamage)
                : 0;

            var stats = new TrainingStats(
                sessionId,
                session.Duration ?? TimeSpan.Zero,
                session.RoundsPracticed,
                session.SuccessfulCombos,
                maxComboHits,
                maxComboDamage);

            return Result<TrainingStats>.Success(stats);
        }
        catch (Exception ex)
        {
            return Result<TrainingStats>.Failure($"Failed to end training session: {ex.Message}");
        }
    }
}
