using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SaveState.Core.Common.Services;
using SaveState.Core.Mugen.TournamentEvents;
using SaveState.Infrastructure.Mugen.TournamentEvents.Services;
using SaveState.Infrastructure.Persistence;

namespace SaveState.Infrastructure.Tests.Mugen;

public class TournamentEventServiceFacadeTests
{
    [Fact]
    public async Task CreateTournamentAsync_ThenStartTournamentAsync_TransitionsToInProgress()
    {
        await using var dbContext = CreateDbContext();
        var sut = new TournamentEventService(dbContext, NullLogger<TournamentEventService>.Instance, SystemTimeProvider.Instance);

        var createResult = await sut.CreateTournamentAsync(new CreateTournamentRequest
        {
            Name = "Facade Split Cup",
            Organizer = "test",
            MaxParticipants = 8
        });

        var startResult = await sut.StartTournamentAsync(createResult.Value!.Id);

        createResult.IsSuccess.Should().BeTrue();
        startResult.IsSuccess.Should().BeTrue();
        startResult.Value!.Status.Should().Be(TournamentStatus.InProgress);
    }

    [Fact]
    public async Task GenerateBracketAsync_WithRegisteredParticipants_CreatesMatchesAndRounds()
    {
        await using var dbContext = CreateDbContext();
        var sut = new TournamentEventService(dbContext, NullLogger<TournamentEventService>.Instance, SystemTimeProvider.Instance);

        var tournamentResult = await sut.CreateTournamentAsync(new CreateTournamentRequest
        {
            Name = "Bracket Split Test",
            Organizer = "test",
            MaxParticipants = 4
        });

        var tournamentId = tournamentResult.Value!.Id;
        await sut.RegisterParticipantAsync(tournamentId, new RegisterParticipantRequest { Name = "P1" });
        await sut.RegisterParticipantAsync(tournamentId, new RegisterParticipantRequest { Name = "P2" });
        await sut.RegisterParticipantAsync(tournamentId, new RegisterParticipantRequest { Name = "P3" });
        await sut.RegisterParticipantAsync(tournamentId, new RegisterParticipantRequest { Name = "P4" });

        var bracketResult = await sut.GenerateBracketAsync(tournamentId, SeedingMethod.RegistrationOrder);

        bracketResult.IsSuccess.Should().BeTrue();
        bracketResult.Value!.Matches.Should().NotBeEmpty();
        bracketResult.Value.Rounds.Should().NotBeEmpty();
    }

    private static SaveStateDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseInMemoryDatabase($"tournament-event-tests-{Guid.NewGuid():N}")
            .Options;

        return new SaveStateDbContext(options);
    }
}
