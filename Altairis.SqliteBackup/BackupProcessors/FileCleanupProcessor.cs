using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup.BackupProcessors;

public class FileCleanupProcessor : IBackupProcessor {
    private readonly string mask;
    private readonly int fileCount;
    private readonly ILogger<FileCleanupProcessor> logger;

    public FileCleanupProcessor(string mask, int fileCount, ILogger<FileCleanupProcessor> logger) {
        this.mask = mask;
        this.fileCount = fileCount;
        this.logger = logger;
    }

    public int Priority { get; set; }

    public Task<string> ProcessBackupFile(string backupFilePath, CancellationToken cancellationToken) {
        var backupFolder = new DirectoryInfo(Path.GetDirectoryName(backupFilePath) ?? string.Empty);
        var files = backupFolder.GetFiles(this.mask, SearchOption.TopDirectoryOnly).OrderByDescending(x => x.Name);
        var filesToDelete = files.Skip(this.fileCount);
        foreach (var file in filesToDelete) {
            file.Delete();
            this.logger.LogInformation("Deleted file {fileName}.", file.FullName);
        }
        return Task.FromResult(backupFilePath);
    }
}
