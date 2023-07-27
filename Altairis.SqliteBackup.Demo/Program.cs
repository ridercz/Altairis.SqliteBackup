using System.Text.Json;
using Altairis.SqliteBackup;
using Altairis.SqliteBackup.AzureStorage;
using Altairis.SqliteBackup.Demo.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Setup backup of Sqlite database with upload to local site
builder.Services.AddSqliteBackup(builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new Exception("Requied connection string DefaultConnection is not specified."), options => {
    options.BackupInterval = TimeSpan.FromSeconds(10);
    options.CheckInterval = TimeSpan.FromSeconds(3);
    options.FolderName = "App_Data/Backup";
    options.FileExtension = ".bak";
    options.UseChecksum = true;
})
    .WithGZip()
    .WithHttpUpload("http://localhost:5000/receive-file")
    .WithAzureStorageUpload(builder.Configuration.GetConnectionString("AzureStorageSAS") ?? throw new Exception("Requied connection string AzureStorageSAS not specified."))
    .WithFileCleanup("*.bak.gz", 3);

// Add health check
builder.Services.AddSingleton<BackupServiceHealthCheck>();
builder.Services.AddHealthChecks()
    .AddCheck<BackupServiceHealthCheck>("SqliteBackup");

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

// Map health check serialization endpoint
app.MapHealthChecks("/", new() {
    ResponseWriter = async (context, report) => {
        // Prepare report for serialization
        var preparedReport = new {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data
            })
        };

        // Serialize to JSON
        var json = JsonSerializer.Serialize(preparedReport, new JsonSerializerOptions { WriteIndented = true });

        // Set content type and serialize
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(json);
    }
});

// Map controllers and run application
app.MapControllers();
app.Run();
