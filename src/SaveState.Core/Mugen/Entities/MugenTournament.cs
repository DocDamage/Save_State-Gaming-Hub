namespace SaveState.Core.Mugen.Entities;

using SaveState.Core.Common.Base;
using SaveState.Core.Common.Services;

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
    /// <param name="timeProvider">The time provider for timestamp generation.</param>
    /// <returns>A new MugenTournament instance.</returns>
    public static MugenTournament Create(string name, TournamentFormat format, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        return new MugenTournament
        {
            Id = Guid.NewGuid(),
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Format = format,
            Status = TournamentStatus.Setup,
            CreatedAt = timeProvider.UtcNow
        };
    }

    /// <summary>
    /// Creates a new tournament with explicit timestamp.
    /// </summary>
    /// <param name="name">Tournament name.</param>
    /// <param name="format">Tournament format.</param>
    /// <param name="createdAt">Creation timestamp.</param>
    /// <returns>A new MugenTournament instance.</returns>
    public static MugenTournament Create(string name, TournamentFormat format, DateTime createdAt)
    {
        return new MugenTournament
        {
            Id = Guid.NewGuid(),
            Name = Guard.Against.NullOrWhiteSpace(name, nameof(name)),
            Format = format,
            Status = TournamentStatus.Setup,
            CreatedAt = createdAt
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
    /// <param name="timeProvider">The time provider for timestamp generation.</param>
    public void Complete(Guid winnerId, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        if (Status != TournamentStatus.InProgress)
            throw new InvalidOperationException("Tournament can only be completed from in-progress state.");

        WinnerId = winnerId;
        CompletedAt = timeProvider.UtcNow;
        Status = TournamentStatus.Completed;
    }

    /// <summary>
    /// Completes the tournament with a winner and explicit timestamp.
    /// </summary>
    /// <param name="winnerId">The winning participant ID.</param>
    /// <param name="completedAt">Completion timestamp.</param>
    public void Complete(Guid winnerId, DateTime completedAt)
    {
        if (Status != TournamentStatus.InProgress)
            throw new InvalidOperationException("Tournament can only be completed from in-progress state.");

        WinnerId = winnerId;
        CompletedAt = completedAt;
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

    public MugenTournament(Guid id, string name, ITimeProvider timeProvider)
    {
        Guard.Against.Null(timeProvider, nameof(timeProvider));
        Id = id;
        Name = name;
        Status = TournamentStatus.Setup;
        CreatedAt = timeProvider.UtcNow;
    }

    public MugenTournament(Guid id, string name, DateTime createdAt)
    {
        Id = id;
        Name = name;
        Status = TournamentStatus.Setup;
        CreatedAt = createdAt;
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
