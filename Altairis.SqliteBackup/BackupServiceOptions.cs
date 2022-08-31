namespace Altairis.SqliteBackup;

public class BackupServiceOptions {
    private const string DefaultFileExtension = ".bak";

    public BackupServiceOptions(string connectionString) {
        this.ConnectionString = connectionString;
    }

    public string ConnectionString { get; set; }

    public string? FolderName { get; set; }

    public string FileExtension { get; set; } = DefaultFileExtension;

    public TimeSpan BackupInterval { get; set; } = TimeSpan.FromDays(1);

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(15);

    public bool UseLocalTime { get; set; } = false;

    public bool UseChecksum { get; set; } = true;

}
