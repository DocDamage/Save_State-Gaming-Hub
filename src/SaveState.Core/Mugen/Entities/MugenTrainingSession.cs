namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;

/// <summary>
/// Represents a MUGEN training session.
/// Tracks training activities, combos practiced, and performance metrics.
/// </summary>
public class MugenTrainingSession : EntityBase
{
    /// <summary>
    /// The ID of the character being trained.
    /// </summary>
    public Guid CharacterId { get; private set; }

    /// <summary>
    /// The character being trained.
    /// </summary>
    public MugenCharacter Character { get; private set; } = null!;

    /// <summary>
    /// The ID of the opponent/dummy character used in training.
    /// </summary>
    public Guid OpponentCharacterId { get; private set; }

    /// <summary>
    /// The opponent/dummy character used in training.
    /// </summary>
    public MugenCharacter OpponentCharacter { get; private set; } = null!;

    /// <summary>
    /// The user ID who conducted this training session.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The type of training session.
    /// </summary>
    public TrainingSessionType SessionType { get; private set; }

    /// <summary>
    /// When the training session started.
    /// </summary>
    public DateTime StartedAt { get; private set; }

    /// <summary>
    /// When the training session ended (if completed).
    /// </summary>
    public DateTime? EndedAt { get; private set; }

    /// <summary>
    /// The total duration of the training session.
    /// </summary>
    public TimeSpan? Duration => EndedAt.HasValue ? EndedAt - StartedAt : null;

    /// <summary>
    /// Number of rounds practiced in this session.
    /// </summary>
    public int RoundsPracticed { get; private set; }

    /// <summary>
    /// Number of successful combos executed.
    /// </summary>
    public int SuccessfulCombos { get; private set; }

    /// <summary>
    /// Number of failed combo attempts.
    /// </summary>
    public int FailedCombos { get; private set; }

    /// <summary>
    /// Optional notes about the training session.
    /// </summary>
    public string? Notes { get; private set; }

    /// <summary>
    /// The recordings made during this training session.
    /// </summary>
    public ICollection<MugenDummyRecording> Recordings { get; private set; } = new List<MugenDummyRecording>();

    /// <summary>
    /// Creates a new training session.
    /// </summary>
    /// <param name="characterId">Character being trained ID.</param>
    /// <param name="opponentCharacterId">Opponent/dummy character ID.</param>
    /// <param name="userId">User ID conducting the training.</param>
    /// <param name="sessionType">Type of training session.</param>
    /// <returns>A new MugenTrainingSession instance.</returns>
    public static MugenTrainingSession Create(
        Guid characterId,
        Guid opponentCharacterId,
        Guid userId,
        TrainingSessionType sessionType)
    {
        return new MugenTrainingSession
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            OpponentCharacterId = opponentCharacterId,
            UserId = userId,
            SessionType = sessionType,
            StartedAt = DateTime.UtcNow,
            RoundsPracticed = 0,
            SuccessfulCombos = 0,
            FailedCombos = 0
        };
    }

    /// <summary>
    /// Records a combo attempt during training.
    /// </summary>
    /// <param name="successful">Whether the combo was successful.</param>
    public void RecordCombo(bool successful)
    {
        if (EndedAt.HasValue)
            throw new InvalidOperationException("Cannot record combos on a completed session.");

        if (successful)
            SuccessfulCombos++;
        else
            FailedCombos++;
    }

    /// <summary>
    /// Records a round completion during training.
    /// </summary>
    public void RecordRound()
    {
        if (EndedAt.HasValue)
            throw new InvalidOperationException("Cannot record rounds on a completed session.");

        RoundsPracticed++;
    }

    /// <summary>
    /// Ends the training session.
    /// </summary>
    /// <param name="notes">Optional notes about the session.</param>
    public void End(string? notes = null)
    {
        if (EndedAt.HasValue)
            throw new InvalidOperationException("Session is already ended.");

        EndedAt = DateTime.UtcNow;
        Notes = notes;
    }

    /// <summary>
    /// Adds a recording to this training session.
    /// </summary>
    /// <param name="recording">The recording to add.</param>
    public void AddRecording(MugenDummyRecording recording)
    {
        if (recording.TrainingSessionId != Id)
            throw new InvalidOperationException("Recording does not belong to this session.");

        Recordings.Add(recording);
    }

    // EF Core constructor
    private MugenTrainingSession() { }
}

/// <summary>
/// Represents the type of training session.
/// </summary>
public enum TrainingSessionType
{
    ComboPractice,
    DefensePractice,
    SpacingPractice,
    MixupPractice,
    GeneralPractice
}