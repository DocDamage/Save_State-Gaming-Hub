using FluentAssertions;
using Moq;
using SaveState.Core.Common; // For Result
using SaveState.Core.Mugen.Entities;
using SaveState.Core.Mugen.Services; // For IMugenCharacterRepository
using SaveState.Infrastructure.Mugen; // For MugenStatsService
using SaveState.Core.Mugen.ValueObjects; // For MugenCharacter value objects
using SaveState.Core.Mugen; // For MatchResult enum
using Xunit;

namespace SaveState.Infrastructure.Tests.Mugen;

public class MugenStatsServiceTests
{
    private readonly Mock<IMugenCharacterRepository> _characterRepoMock;
    private readonly Mock<IMugenMatchHistoryRepository> _matchHistoryRepoMock;
    private readonly MugenStatsService _service;

    public MugenStatsServiceTests()
    {
        _characterRepoMock = new Mock<IMugenCharacterRepository>();
        _matchHistoryRepoMock = new Mock<IMugenMatchHistoryRepository>();
        _service = new MugenStatsService(_characterRepoMock.Object, _matchHistoryRepoMock.Object);
    }

    [Fact]
    public async Task GetCharacterStatsAsync_CharacterNotFound_ReturnsFailure()
    {
        // Arrange
        var charId = Guid.NewGuid();
        _characterRepoMock.Setup(x => x.GetByIdAsync(charId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<MugenCharacter>("Not found"));

        // Act
        var result = await _service.GetCharacterStatsAsync(charId);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task GetCharacterStatsAsync_NoMatches_ReturnsEmptyStats()
    {
        // Arrange
        var charId = Guid.NewGuid();
        var character = MugenCharacter.Create("Ryu", "chars/ryu/ryu.def", "chars/ryu");
        // Use reflection or a test helper to set Id if needed, but Create usually sets a new Guid.
        // Assuming we can mock the repo to return this character for ANY Guid or match the one we verify.

        _characterRepoMock.Setup(x => x.GetByIdAsync(charId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<MugenCharacter>(character));

        _matchHistoryRepoMock.Setup(x => x.GetByCharacterAsync(charId, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MugenMatchHistory>());

        // Act
        var result = await _service.GetCharacterStatsAsync(charId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalMatches.Should().Be(0);
        result.Value.Wins.Should().Be(0);
        result.Value.CharacterName.Should().Be("Ryu");
    }

    [Fact]
    public async Task GetCharacterStatsAsync_WithMatches_CalculatesCorrectly()
    {
        // Arrange
        var charId = Guid.NewGuid();
        var opponentId = Guid.NewGuid();
        var character = MugenCharacter.Create("Ryu", "chars/ryu/ryu.def", "chars/ryu");

        _characterRepoMock.Setup(x => x.GetByIdAsync(charId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<MugenCharacter>(character));

        // Create some dummy matches
        var match1 = MugenMatchHistory.Create(charId, opponentId, MatchResult.Player1Win, 2, 0, TimeSpan.FromMinutes(1), GameMode.Versus);
        var match2 = MugenMatchHistory.Create(charId, opponentId, MatchResult.Player2Win, 1, 2, TimeSpan.FromMinutes(1), GameMode.Versus);
        var match3 = MugenMatchHistory.Create(opponentId, charId, MatchResult.Player2Win, 0, 2, TimeSpan.FromMinutes(1), GameMode.Versus); // Opponent (P1) vs Char (P2), Char Wins (P2 Win)

        var matches = new List<MugenMatchHistory> { match1, match2, match3 };

        _matchHistoryRepoMock.Setup(x => x.GetByCharacterAsync(charId, 1000, It.IsAny<CancellationToken>()))
            .ReturnsAsync(matches);

        // Act
        var result = await _service.GetCharacterStatsAsync(charId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalMatches.Should().Be(3);
        result.Value.Wins.Should().Be(2); // Match1 (P1 Win) + Match3 (P2 Win where P2 is char)
        result.Value.Losses.Should().Be(1); // Match2 (P2 Win where P2 is opponent)
        result.Value.WinRate.Should().BeApproximately(0.666f, 0.01f);
    }
}

