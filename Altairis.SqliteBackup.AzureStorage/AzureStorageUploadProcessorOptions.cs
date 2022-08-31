namespace Altairis.SqliteBackup.AzureStorage;

public class AzureStorageUploadProcessorOptions {
    private const string DefaultContainerName = "sqlitebackup";
    private const string DefaultContentType = "application/vnd.sqlite3";

    public AzureStorageUploadProcessorOptions(string connectionString) {
        this.ConnectionString = connectionString;
    }

    public string ConnectionString { get; set; }

    public string ContainerName { get; set; } = DefaultContainerName;

    public string ContentType { get; set; } = DefaultContentType;

}
