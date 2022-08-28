using Microsoft.EntityFrameworkCore;

namespace Altairis.SqliteBackup.Demo.Data;

public class DemoDbContext : DbContext {

    public DemoDbContext(DbContextOptions options) : base(options) {
    }

    public DbSet<StartupTime> StartupTimes => this.Set<StartupTime>();

}
