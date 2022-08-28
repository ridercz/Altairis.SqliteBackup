using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup;

public class BackupService : BackgroundService {
    private const string TimestampFormat = "yyyyMMddHHmmss";

    private readonly BackupServiceOptions options;
    private readonly ILogger<BackupService> logger;
    private readonly string backupFolder;
    private readonly string backupFileNamePrefix;
    private readonly string readOnlyConnectionString;

    public BackupService(BackupServiceOptions options, ILogger<BackupService> logger) {
        // Read options
        this.options = options;
        this.logger = logger;

        // Create read-only connection string
        this.readOnlyConnectionString = new SqliteConnectionStringBuilder(this.options.ConnectionString) { Mode = SqliteOpenMode.ReadOnly }.ToString();

        // Setup parameters based on Sqlite DB filename
        using var db = new SqliteConnection(this.readOnlyConnectionString);
        db.Open();
        this.backupFolder = this.options.BackupFolder ?? Path.GetDirectoryName(db.DataSource) ?? ".";
        this.backupFileNamePrefix = Path.GetFileNameWithoutExtension(db.DataSource) ?? "backup";
        db.Close();
        logger.LogInformation("Initializing backup service using folder '{backupFolder}' and prefix '{backupFileNamePrefix}'", this.backupFolder, this.backupFileNamePrefix);

        // Create directory if it does not already exist
        Directory.CreateDirectory(this.backupFolder);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        while (!stoppingToken.IsCancellationRequested) {
            var lastBackupTime = this.GetLastBackupTime();
            var nextBackupTime = lastBackupTime.Add(this.options.BackupInterval);
            var timeToBackup = nextBackupTime.Subtract(DateTime.Now);
            this.logger.LogDebug("Last backup is from {lastBackupTime}, next backup scheduled for {nextBackupTime} (in {timeToBackup}).", lastBackupTime, nextBackupTime, timeToBackup);
            if (timeToBackup <= TimeSpan.Zero) {
                // Backup database and get backup file name
                var fileName = await this.PerformBackup(stoppingToken);
                // Perform after-backup action
                this.options.AfterBackupAction?.Invoke(fileName);
                // Delete extra files
                this.PerformCleanup();
            }
            await Task.Delay(this.options.CheckInterval, stoppingToken);
        }
    }

    private async Task<string?> PerformBackup(CancellationToken stoppingToken) {
        // Create connection string
        var backupFileName = Path.Combine(this.backupFolder, this.backupFileNamePrefix + "_" + DateTime.Now.ToString(TimestampFormat) + this.options.BackupFileExtension);
        var backupConnectionString = new SqliteConnectionStringBuilder() { DataSource = backupFileName }.ToString();

        // Perform backup
        this.logger.LogInformation("Performing backup into file {backupFileName}.", backupFileName);
        try {
            using var oldDb = new SqliteConnection(this.readOnlyConnectionString);
            using var newDb = new SqliteConnection(backupConnectionString);
            await oldDb.OpenAsync(stoppingToken);
            oldDb.BackupDatabase(newDb);
            await oldDb.CloseAsync();
            return backupFileName;
        } catch (Exception ex) {
            this.logger.LogError(ex, "Exception while performing database backup.");
            return null;
        }
    }

    private void PerformCleanup() {
        if (this.options.NumberOfBackupFiles <= 0) return;

        // Get list of existing backup files
        var existingFiles = this.GetExistingBackupFiles();
        if (!existingFiles.Any()) return;

        // Get list of files to delete and delete them
        var filesToDelete = existingFiles.OrderByDescending(f => f.Name).Skip(this.options.NumberOfBackupFiles);
        foreach (var file in filesToDelete) {
            this.logger.LogInformation("Deleting old backup file {fileName}.", file.FullName);
            file.Delete();
        }
    }

    private FileInfo[] GetExistingBackupFiles() {
        var backupFileNamePattern = this.backupFileNamePrefix + "_" + new string('?', TimestampFormat.Length) + this.options.BackupFileExtension;
        return new DirectoryInfo(this.backupFolder).GetFiles(backupFileNamePattern, SearchOption.TopDirectoryOnly);
    }

    private DateTime GetLastBackupTime() {
        // Get list of existing backup files
        var existingFiles = this.GetExistingBackupFiles();
        if (!existingFiles.Any()) return DateTime.MinValue;

        // Get newest timestamp
        try {
            var newestFileName = existingFiles.OrderByDescending(x => x.Name).First().Name;
            var newestTimestamp = newestFileName[(this.backupFileNamePrefix.Length + 1)..^this.options.BackupFileExtension.Length];
            return DateTime.ParseExact(newestTimestamp, TimestampFormat, CultureInfo.InvariantCulture);
        } catch (Exception) {
            return DateTime.MinValue;
        }
    }

}
