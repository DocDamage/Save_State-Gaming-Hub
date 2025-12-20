using Microsoft.EntityFrameworkCore;
using SaveState.Core.Data;
using SaveState.Core.Entities;
using SaveState.Core.Services;

namespace SaveState.Tests;

public class CollectionServiceTests
{
    private SaveStateDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SaveStateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new SaveStateDbContext(options);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateNewCollection()
    {
        // Arrange
        using var context = CreateContext();
        var service = new CollectionService(context);
        
        // Act
        var collection = await service.CreateAsync("RPG", "Role Playing Games");
        
        // Assert
        Assert.NotNull(collection);
        Assert.Equal("RPG", collection.Name);
        Assert.Equal("Role Playing Games", collection.Description);
        Assert.NotEqual(Guid.Empty, collection.Id);
        
        var saved = await context.Collections.FindAsync(collection.Id);
        Assert.NotNull(saved);
        Assert.Equal(1, saved.SortOrder);
    }

    [Fact]
    public async Task AddGameToCollection_ShouldAddGame()
    {
        // Arrange
        using var context = CreateContext();
        var service = new CollectionService(context);
        
        var game = new Game { Id = Guid.NewGuid(), Title = "Final Fantasy VII" };
        context.Games.Add(game);
        
        var collection = new Collection { Id = Guid.NewGuid(), Name = "Favorites" };
        context.Collections.Add(collection);
        await context.SaveChangesAsync();
        
        // Act
        await service.AddGameToCollectionAsync(collection.Id, game.Id);
        
        // Assert
        var savedCollection = await context.Collections.Include(c => c.Games).FirstAsync(c => c.Id == collection.Id);
        Assert.Single(savedCollection.Games);
        Assert.Equal("Final Fantasy VII", savedCollection.Games.First().Title);
    }

    [Fact]
    public async Task RemoveGameFromCollection_ShouldRemoveGame()
    {
        // Arrange
        using var context = CreateContext();
        var service = new CollectionService(context);
        
        var game = new Game { Id = Guid.NewGuid(), Title = "Halo" };
        var collection = new Collection { Id = Guid.NewGuid(), Name = "FPS" };
        collection.Games.Add(game);
        
        context.Games.Add(game);
        context.Collections.Add(collection);
        await context.SaveChangesAsync();
        
        // Act
        await service.RemoveGameFromCollectionAsync(collection.Id, game.Id);
        
        // Assert
        var savedCollection = await context.Collections.Include(c => c.Games).FirstAsync(c => c.Id == collection.Id);
        Assert.Empty(savedCollection.Games);
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteCollection()
    {
        // Arrange
        using var context = CreateContext();
        var service = new CollectionService(context);
        
        var collection = new Collection { Id = Guid.NewGuid(), Name = "Trash" };
        context.Collections.Add(collection);
        await context.SaveChangesAsync();
        
        // Act
        await service.DeleteAsync(collection.Id);
        
        // Assert
        var saved = await context.Collections.FindAsync(collection.Id);
        Assert.Null(saved);
    }
}
