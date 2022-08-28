using Altairis.SqliteBackup;
using Altairis.SqliteBackup.Demo.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Setup backup of Sqlite database
builder.Services.AddSingleton(new BackupServiceOptions(builder.Configuration.GetConnectionString("DefaultConnection")) {
    BackupInterval = TimeSpan.FromMinutes(1),   // Backup every minute
    CheckInterval = TimeSpan.FromSeconds(10),   // Check for backup every 10 seconds
    NumberOfBackupFiles = 3,                    // Keep last 3 backup files
});
builder.Services.AddHostedService<BackupService>();

// Register DB context
builder.Services.AddDbContext<DemoDbContext>(options => {
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Build app and migrate database to latest version and record startup time just so we have some data
var app = builder.Build();
using var scope = app.Services.CreateScope();
using var dc = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
dc.Database.Migrate();
dc.StartupTimes.Add(new StartupTime { Time = DateTime.Now });
dc.SaveChanges();

// Show simple message and run application
app.MapGet("/", () => "This web app does not do anything, just backs up the Sqlite database -- see console log.");
app.Run();
