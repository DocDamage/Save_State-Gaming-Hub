using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SaveState.Core.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SaveStateDbContext>
{
    public SaveStateDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SaveStateDbContext>();
        optionsBuilder.UseSqlite("Data Source=savestate_design.db");

        return new SaveStateDbContext(optionsBuilder.Options);
    }
}
