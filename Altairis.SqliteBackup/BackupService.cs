using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup;

public class BackupService : BackgroundService {
    private const string TimestampFormat = "yyyyMMddHHmmss";
    private const string HashFileExtension = ".lastHash";
    private const string TimestampFileExtension = ".lastTime";

    private readonly BackupServiceOptions options;
    private readonly ILogger<BackupService> logger;
    private readonly BackupServiceHealthCheck? healthCheck;
    private readonly string backupFolder;
    private readonly string backupFileNamePrefix;
    private readonly string readOnlyConnectionString;
    private readonly IOrderedEnumerable<IBackupProcessor> orderedBackupProcessors;
    private bool lastBackupSuccessful = false;

    // Constructors

    public BackupService(BackupServiceOptions options, ILogger<BackupService> logger, IServiceProvider serviceProvider, BackupServiceHealthCheck? healthCheck = null) {
        // Read options
        this.options = options;
        this.logger = logger;
        this.healthCheck = healthCheck;

        // Create read-only connection string
        this.readOnlyConnectionString = new SqliteConnectionStringBuilder(this.options.ConnectionString) { Mode = SqliteOpenMode.ReadOnly }.ToString();

        // Setup parameters based on Sqlite DB filename
        using var db = new SqliteConnection(this.readOnlyConnectionString);
        db.Open();
        this.backupFolder = this.options.FolderName ?? Path.GetDirectoryName(db.DataSource) ?? ".";
        this.backupFileNamePrefix = Path.GetFileNameWithoutExtension(db.DataSource) ?? "backup";
        db.Close();
        logger.LogInformation("Initializing backup service using folder '{backupFolder}' and prefix '{backupFileNamePrefix}'.", this.backupFolder, this.backupFileNamePrefix);

        // Create directory if it does not already exist
        Directory.CreateDirectory(this.backupFolder);

        // Get backup processors
        this.orderedBackupProcessors = serviceProvider.GetServices<IBackupProcessor>().OrderBy(x => x.Priority);
    }

    // Background service implementation

    protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        this.logger.LogInformation("Starting backup loop; check interval is {checkInterval}, backup interval is {backupInterval}.", this.options.CheckInterval, this.options.BackupInterval);
        while (!stoppingToken.IsCancellationRequested) {
            // Get last timestamp
            var lastBackupTimeFileName = Path.Combine(this.backupFolder, this.backupFileNamePrefix + TimestampFileExtension);
            var lastBackupTimeString = File.Exists(lastBackupTimeFileName) ? await File.ReadAllTextAsync(lastBackupTimeFileName, stoppingToken) : DateTime.MinValue.ToString("s");
            var lastBackupTime = DateTime.ParseExact(lastBackupTimeString, "s", CultureInfo.InvariantCulture);

            // Check if it's time to backup
            var nextBackupTime = lastBackupTime.Add(this.options.BackupInterval);
            var timeToBackup = nextBackupTime.Subtract(DateTime.Now);
            this.logger.LogDebug("Last backup is from {lastBackupTime}, next backup scheduled for {nextBackupTime} (in {timeToBackup}).", lastBackupTime, nextBackupTime, timeToBackup);
            if (timeToBackup <= TimeSpan.Zero) {
                // Backup database and get backup file name
                var fileName = await this.PerformBackup(stoppingToken);
                if (fileName != null) {
                    // Perform checksum logic
                    var continueWithBackup = !this.lastBackupSuccessful || !this.options.UseChecksum || await this.PerformChecksumCheck(fileName, stoppingToken);
                    if (continueWithBackup) {
                        // Call configured backup processors
                        this.lastBackupSuccessful = true;
                        foreach (var bp in this.orderedBackupProcessors) {
                            try {
                                fileName = await bp.ProcessBackupFile(fileName, stoppingToken);
                            } catch (Exception ex) {
                                this.logger.LogError(ex, "Backup processor {processorType} with priority {priority} didn't completed successfully. Subsequent processors are skipped.", bp.GetType().ToString(), bp.Priority);
                                this.healthCheck?.Update(false, $"Backup processor {bp.GetType()} with priority {bp.Priority} didn't completed successfully.", ex);
                                this.lastBackupSuccessful = false;
                                break;
                            }
                        }
                        if (this.lastBackupSuccessful) this.healthCheck?.Update(true, "Backup completed successfully.");
                    } else {
                        // Delete the backup, is not changed from last one
                        File.Delete(fileName);
                        this.logger.LogInformation("File {fileName} was deleted because is not different from last backup.", fileName);
                        this.healthCheck?.Update(true, "Backup file is not different from last backup.");
                        this.lastBackupSuccessful = true;
                    }
                }
            }
            await Task.Delay(this.options.CheckInterval, stoppingToken);
        }
        this.logger.LogInformation("Backup loop stopped.");
    }

    // Helper methods

    private async Task<bool> PerformChecksumCheck(string fileName, CancellationToken stoppingToken) {
        // Get last hash
        var hashFileName = Path.Combine(this.backupFolder, this.backupFileNamePrefix + HashFileExtension);
        var lastHash = File.Exists(hashFileName) ? await File.ReadAllTextAsync(hashFileName, stoppingToken) : string.Empty;

        // Compute SHA256 hash of backup file
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(fileName);
        var currentHashBytes = await sha.ComputeHashAsync(stream, stoppingToken);
        var currentHash = string.Join(string.Empty, currentHashBytes.Select(x => x.ToString("X2")));
        await File.WriteAllTextAsync(hashFileName, currentHash, stoppingToken);

        // Compare last hash with current one
        this.logger.LogDebug("Last known hash is \"{lastHash}\", current hash is \"{currentHash}\".", lastHash, currentHash);
        return !lastHash.Equals(currentHash, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string?> PerformBackup(CancellationToken stoppingToken) {
        // Create backup file name
        var ts = this.options.UseLocalTime ? DateTime.Now : DateTime.UtcNow;
        var backupFileName = Path.Combine(this.backupFolder, this.backupFileNamePrefix + "_" + ts.ToString(TimestampFormat) + this.options.FileExtension);

        // Perform backup
        try {
            this.logger.LogInformation("Performing backup into file {backupFileName}.", backupFileName);
            using var db = new SqliteConnection(this.readOnlyConnectionString);
            await db.OpenAsync(stoppingToken);
            var cmd = db.CreateCommand();
            cmd.CommandText = "VACUUM INTO @FileName";
            cmd.Parameters.AddWithValue("@FileName", backupFileName);
            await cmd.ExecuteNonQueryAsync(stoppingToken);
            await db.CloseAsync();

            // Update last backup time
            var lastBackupTimeFileName = Path.Combine(this.backupFolder, this.backupFileNamePrefix + TimestampFileExtension);
            await File.WriteAllTextAsync(lastBackupTimeFileName, DateTime.Now.ToString("s"), stoppingToken);
            return backupFileName;
        } catch (Exception ex) {
            this.logger.LogError(ex, "Exception while performing database backup.");
            this.healthCheck?.Update(false, "Exception while performing database backup.", ex);
            return null;
        }
    }

}
