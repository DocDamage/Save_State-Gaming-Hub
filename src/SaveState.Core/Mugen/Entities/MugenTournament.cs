namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;

/// <summary>
/// Represents a MUGEN tournament with bracket management and participant tracking.
/// </summary>
public class MugenTournament : EntityBase
{
    /// <summary>
    /// The name of the tournament.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The tournament format (single elimination, double elimination, round robin).
    /// </summary>
    public TournamentFormat Format { get; private set; }

    /// <summary>
    /// The current status of the tournament.
    /// </summary>
    public TournamentStatus Status { get; private set; }

    /// <summary>
    /// The participants in this tournament.
    /// </summary>
    public ICollection<TournamentParticipant> Participants { get; private set; } = new List<TournamentParticipant>();

    /// <summary>
    /// The matches in this tournament.
    /// </summary>
    public ICollection<TournamentMatchEntity> Matches { get; private set; } = new List<TournamentMatchEntity>();

    /// <summary>
    /// When the tournament was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the tournament was completed (if finished).
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>
    /// The ID of the winning participant (if tournament is completed).
    /// </summary>
    public Guid? WinnerId { get; private set; }

    /// <summary>
    /// Creates a new tournament.
    /// </summary>
    /// <param name="name">Tournament name.</param>
    /// <param name="format">Tournament format.</param>
    /// <param name="createdAt">Optional creation timestamp.</param>
    /// <returns>A new MugenTournament instance.</returns>
    public static MugenTournament Create(string name, TournamentFormat format, DateTime? createdAt = null)
    {
        return new MugenTournament
        {
            Id = Guid.NewGuid(),
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Format = format,
            Status = TournamentStatus.Setup,
            CreatedAt = createdAt ?? DateTime.UtcNow
        };
    }

    /// <summary>
    /// Starts the tournament.
    /// </summary>
    public void Start()
    {
        if (Status != TournamentStatus.Setup)
            throw new InvalidOperationException("Tournament can only be started from setup state.");

        Status = TournamentStatus.InProgress;
    }

    /// <summary>
    /// Completes the tournament with a winner.
    /// </summary>
    /// <param name="winnerId">The winning participant ID.</param>
    /// <param name="completedAt">Optional completion timestamp.</param>
    public void Complete(Guid winnerId, DateTime? completedAt = null)
    {
        if (Status != TournamentStatus.InProgress)
            throw new InvalidOperationException("Tournament can only be completed from in-progress state.");

        WinnerId = winnerId;
        CompletedAt = completedAt ?? DateTime.UtcNow;
        Status = TournamentStatus.Completed;
    }

    /// <summary>
    /// Cancels the tournament.
    /// </summary>
    public void Cancel()
    {
        if (Status == TournamentStatus.Completed)
            throw new InvalidOperationException("Cannot cancel a completed tournament.");

        Status = TournamentStatus.Cancelled;
    }

    // EF Core and Mock constructor
    public MugenTournament() { }

    public MugenTournament(Guid id, string name, DateTime? createdAt = null)
    {
        Id = id;
        Name = name;
        Status = TournamentStatus.Setup;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}

/// <summary>
/// Represents the format of a tournament.
/// </summary>
public enum TournamentFormat
{
    SingleElimination,
    DoubleElimination,
    RoundRobin
}

/// <summary>
/// Represents the current status of a tournament.
/// </summary>
public enum TournamentStatus
{
    Setup,
    InProgress,
    Completed,
    Cancelled
}
