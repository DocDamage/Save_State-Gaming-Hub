namespace SaveState.Infrastructure.Persistence.Seeders;

using Microsoft.EntityFrameworkCore;
using SaveState.Core.GameLibrary.Entities;

/// <summary>
/// Seeds the database with initial test data.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(SaveStateDbContext context)
    {
        // Only seed if no games exist
        if (await context.Games.AnyAsync().ConfigureAwait(false))
        {
            return;
        }

        var testGame = Game.Create("Test Game", null, null, "/images/test-game-cover.png");

        await context.Games.AddAsync(testGame).ConfigureAwait(false);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
