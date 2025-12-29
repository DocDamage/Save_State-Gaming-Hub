using FluentAssertions;
using SaveState.Core.GameLibrary.Entities;
using SaveState.Core.GameLibrary.Enums;
using Xunit;

namespace SaveState.Core.Tests.GameLibrary;

public class GameTests
{
    [Fact]
    public void Create_WithValidTitle_SetsProperties()
    {
        var game = Game.Create("Test Game", coverImagePath: "/images/cover.png");

        Assert.NotEqual(Guid.Empty, game.Id);
        Assert.Equal("Test Game", game.Title);
        Assert.Equal("/images/cover.png", game.CoverImagePath);
        Assert.True(game.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void Update_WithValidTitle_ChangesTitle()
    {
        var game = Game.Create("Original Title");

        game.Update(title: "New Title");

        Assert.Equal("New Title", game.Title);
        Assert.True(game.UpdatedAt >= game.CreatedAt);
    }

    [Fact]
    public void Update_WithValidDescription_UpdatesDescription()
    {
        var game = Game.Create("Test Game");

        game.Update(description: "New description");

        Assert.Equal("New description", game.Description);
        Assert.True(game.UpdatedAt >= game.CreatedAt);
    }

    [Fact]
    public void SetInstallPath_WithValidPath_SetsPath()
    {
        var game = Game.Create("Test Game");

        game.SetInstallPath("/games/test");

        Assert.Equal("/games/test", game.InstallPath);
        Assert.True(game.UpdatedAt >= game.CreatedAt);
    }

    [Fact]
    public void MarkAsRunning_ChangesStatusToRunning()
    {
        var game = Game.Create("Test Game");
        game.SetInstallPath("/games/test"); // This sets Status to Installed

        game.MarkAsRunning();

        Assert.Equal(GameStatus.Running, game.Status);
    }

    [Fact]
    public void MarkAsNotRunning_ChangesStatusToInstalled()
    {
        var game = Game.Create("Test Game");
        game.SetInstallPath("/games/test"); // Sets to Installed
        game.MarkAsRunning(); // Sets to Running

        game.MarkAsNotRunning();

        Assert.Equal(GameStatus.Installed, game.Status);
    }

    [Fact]
    public void MarkAsDeleted_SetsDeletedFlag()
    {
        var game = Game.Create("Test Game");

        game.MarkAsDeleted();

        Assert.True(game.IsDeleted);
        Assert.True(game.DeletedAt <= DateTime.UtcNow);
    }
}
