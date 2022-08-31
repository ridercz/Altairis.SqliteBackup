using Altairis.SqliteBackup;
using Altairis.SqliteBackup.AzureStorage;
using Altairis.SqliteBackup.BackupProcessors;
using Altairis.SqliteBackup.Demo.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Setup backup of Sqlite database with upload to local site
builder.Services.AddSqliteBackup(builder.Configuration.GetConnectionString("DefaultConnection"), options => {
    options.BackupInterval = TimeSpan.FromSeconds(10);
    options.CheckInterval = TimeSpan.FromSeconds(3);
    options.FolderName = "App_Data/Backup";
    options.FileExtension = ".bak";
});
builder.Services.AddSingleton<IBackupProcessor>(sp => new GZipProcessor(new GZipBackupProcessorOptions(), sp.GetRequiredService<ILogger<GZipProcessor>>()) { Priority = 0 } );
builder.Services.AddSingleton<IBackupProcessor>(sp => new HttpUploadProcessor(new HttpUploadBackupProcessorOptions(new Uri("http://localhost:5000/receive-file")), sp.GetRequiredService<ILogger<HttpUploadProcessor>>()) { Priority = 1 });
builder.Services.AddSingleton<IBackupProcessor>(sp => new AzureStorageBackupProcessor(new AzureStorageBackupProcessorOptions(builder.Configuration.GetConnectionString("AzureStorageSAS")), sp.GetRequiredService<ILogger<AzureStorageBackupProcessor>>()) { Priority = 2 });
builder.Services.AddSingleton<IBackupProcessor>(sp => new FileCleanupProcessor("*.bak", 0, sp.GetRequiredService<ILogger<FileCleanupProcessor>>()) { Priority = 3 });
builder.Services.AddSingleton<IBackupProcessor>(sp => new FileCleanupProcessor("*.bak.gz", 3, sp.GetRequiredService<ILogger<FileCleanupProcessor>>()) { Priority = 4 });

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
