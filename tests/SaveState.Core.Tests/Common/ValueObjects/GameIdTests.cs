using FluentAssertions;
using SaveState.Core.Common.ValueObjects;
using Xunit;

namespace SaveState.Core.Tests.Common.ValueObjects;

public class GameIdTests
{
    [Fact]
    public void NewId_CreatesUniqueIds()
    {
        var id1 = GameId.NewId();
        var id2 = GameId.NewId();

        id1.Should().NotBe(id2);
        id1.Value.Should().NotBe(Guid.Empty);
        id2.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void From_ValidGuid_CreatesGameId()
    {
        var guid = Guid.NewGuid();
        var gameId = GameId.From(guid);

        gameId.Value.Should().Be(guid);
    }

    [Fact]
    public void From_SameGuid_ReturnsEqualObjects()
    {
        var guid = Guid.NewGuid();
        var id1 = GameId.From(guid);
        var id2 = GameId.From(guid);

        id1.Should().Be(id2);
        id1.GetHashCode().Should().Be(id2.GetHashCode());
    }

    [Fact]
    public void ImplicitOperatorGuid_ReturnsValue()
    {
        var guid = Guid.NewGuid();
        var gameId = GameId.From(guid);

        Guid result = gameId;
        result.Should().Be(guid);
    }

    [Fact]
    public void ExplicitOperatorGameId_ValidGuid_CreatesGameId()
    {
        var guid = Guid.NewGuid();
        var gameId = (GameId)guid;

        gameId.Value.Should().Be(guid);
    }

    [Fact]
    public void ToString_ReturnsGuidString()
    {
        var guid = Guid.NewGuid();
        var gameId = GameId.From(guid);

        gameId.ToString().Should().Be(guid.ToString());
    }
}
