namespace Altairis.SqliteBackup;

public interface IBackupProcessor {

    public int Priority { get; set; }

    public Task<string> ProcessBackupFile(string backupFilePath, CancellationToken cancellationToken);

}
