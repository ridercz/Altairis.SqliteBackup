using System.IO.Compression;
using Altairis.SqliteBackup;
using Altairis.SqliteBackup.Demo.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Setup backup of Sqlite database with upload to local site
builder.Services.AddSqliteBackup(builder.Configuration.GetConnectionString("DefaultConnection"), options => {
    options.BackupInterval = TimeSpan.FromSeconds(10);
    options.CheckInterval = TimeSpan.FromSeconds(3);
    options.BackupFolder = "App_Data/Backup";
    options.NumberOfBackupFiles = 3;
    options.CompressionLevel = System.IO.Compression.CompressionLevel.Optimal;
    options.BackupFileExtension = ".bak.gz";
});
builder.Services.AddSqliteBackupHttpUpload(new Uri("http://localhost:5000/receive-file"));

// Register DB context
builder.Services.AddDbContext<DemoDbContext>(options => {
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Register MVC controllers
builder.Services.AddControllers();

// Build app and migrate database to latest version and record startup time just so we have some data
var app = builder.Build();
using var scope = app.Services.CreateScope();
using var dc = scope.ServiceProvider.GetRequiredService<DemoDbContext>();
dc.Database.Migrate();
dc.StartupTimes.Add(new StartupTime { Time = DateTime.Now });
dc.SaveChanges();

// Map controllers and run application
app.MapControllers();
app.Run();
