namespace Altairis.SqliteBackup;

public interface IBackupProcessor {

    Task ProcessBackupFile(string backupFilePath);

}
