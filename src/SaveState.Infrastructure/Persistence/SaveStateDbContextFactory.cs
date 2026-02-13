using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SaveState.Infrastructure.Persistence;

/// <summary>
/// Design-time DbContext factory for EF Core tooling.
/// </summary>
public class SaveStateDbContextFactory : IDesignTimeDbContextFactory<SaveStateDbContext>
{
    public SaveStateDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Data Source=savestate.db";

        var optionsBuilder = new DbContextOptionsBuilder<SaveStateDbContext>();
        optionsBuilder.UseSqlite(connectionString, sqliteOptions =>
        {
            sqliteOptions.CommandTimeout(30);
        });

        return new SaveStateDbContext(optionsBuilder.Options);
    }
}
