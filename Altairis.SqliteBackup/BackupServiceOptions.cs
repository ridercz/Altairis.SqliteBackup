namespace Altairis.SqliteBackup;

public class BackupServiceOptions {

    public BackupServiceOptions(string connectionString) {
        this.ConnectionString = connectionString;
    }

    public string ConnectionString { get; set; }

    public string? BackupFolder { get; set; }

    public string BackupFileExtension { get; set; } = ".bak";

    public TimeSpan BackupInterval { get; set; } = TimeSpan.FromDays(1);

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(15);

    public int NumberOfBackupFiles { get; set; } = 7;

    public Action<string?>? AfterBackupAction { get; set; }

}
