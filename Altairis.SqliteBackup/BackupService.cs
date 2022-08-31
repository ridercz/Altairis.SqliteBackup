using System.Globalization;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup;

public class BackupService : BackgroundService {
    private const string TimestampFormat = "yyyyMMddHHmmss";
    private const string HashFileExtension = ".lastHash";

    private readonly BackupServiceOptions options;
    private readonly ILogger<BackupService> logger;
    private readonly string backupFolder;
    private readonly string backupFileNamePrefix;
    private readonly string readOnlyConnectionString;
    private readonly IOrderedEnumerable<IBackupProcessor> orderedBackupProcessors;

    public BackupService(BackupServiceOptions options, ILogger<BackupService> logger, IServiceProvider serviceProvider) {
        // Read options
        this.options = options;
        this.logger = logger;

        // Create read-only connection string
        this.readOnlyConnectionString = new SqliteConnectionStringBuilder(this.options.ConnectionString) { Mode = SqliteOpenMode.ReadOnly }.ToString();

        // Setup parameters based on Sqlite DB filename
        using var db = new SqliteConnection(this.readOnlyConnectionString);
        db.Open();
        this.backupFolder = this.options.FolderName ?? Path.GetDirectoryName(db.DataSource) ?? ".";
        this.backupFileNamePrefix = Path.GetFileNameWithoutExtension(db.DataSource) ?? "backup";
        db.Close();
        logger.LogInformation("Initializing backup service using folder '{backupFolder}' and prefix '{backupFileNamePrefix}'", this.backupFolder, this.backupFileNamePrefix);

        // Create directory if it does not already exist
        Directory.CreateDirectory(this.backupFolder);

        // Get backup processors
        this.orderedBackupProcessors = serviceProvider.GetServices<IBackupProcessor>().OrderBy(x => x.Priority);
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

                if (fileName != null) {
                    // Perform checksum logic
                    var continueWithBackup = !this.options.UseChecksum || await this.PerformChecksumCheck(fileName, stoppingToken);
                    if (continueWithBackup) {
                        // Call configured backup processors
                        foreach (var bp in this.orderedBackupProcessors) {
                            try {
                                fileName = await bp.ProcessBackupFile(fileName, stoppingToken);
                            } catch (Exception ex) {
                                this.logger.LogError(ex, "Backup processor {processorType} with priority {priority} didn't completed successfully. Subsequent processors are skipped.", bp.GetType().ToString(), bp.Priority);
                                break;
                            }
                        }
                    } else {
                        // Delete the backup, is not changed from last one
                        File.Delete(fileName);
                        this.logger.LogInformation("File {fileName} was deleted because is not different from last backup.", fileName);
                    }
                }
            }
            await Task.Delay(this.options.CheckInterval, stoppingToken);
        }
    }

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
            return backupFileName;
        } catch (Exception ex) {
            this.logger.LogError(ex, "Exception while performing database backup.");
            return null;
        }
    }

    private DateTime GetLastBackupTime() {
        // Get list of existing backup files
        var backupFileNamePattern = this.backupFileNamePrefix + "_" + new string('?', TimestampFormat.Length) + this.options.FileExtension;
        var existingFiles = new DirectoryInfo(this.backupFolder).GetFiles(backupFileNamePattern, SearchOption.TopDirectoryOnly);
        if (!existingFiles.Any()) return DateTime.MinValue;

        // Get newest timestamp
        try {
            var newestFileName = existingFiles.OrderByDescending(x => x.Name).First().Name;
            var newestTimestamp = newestFileName[(this.backupFileNamePrefix.Length + 1)..^this.options.FileExtension.Length];
            return DateTime.ParseExact(newestTimestamp, TimestampFormat, CultureInfo.InvariantCulture, this.options.UseLocalTime ? DateTimeStyles.AssumeLocal : DateTimeStyles.AssumeUniversal);
        } catch (Exception) {
            return DateTime.MinValue;
        }
    }

}
