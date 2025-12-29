using Microsoft.EntityFrameworkCore;
using SaveState.Core.GameLibrary.Entities;

namespace SaveState.Infrastructure.Persistence;

public interface ISaveStateDbContext
{
    DbSet<Game> Games { get; set; }
    DbSet<Achievement> Achievements { get; set; }
    DbSet<UserAchievement> UserAchievements { get; set; }
    DbSet<SaveState.Core.Mugen.Entities.MugenCharacter> MugenCharacters { get; set; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
