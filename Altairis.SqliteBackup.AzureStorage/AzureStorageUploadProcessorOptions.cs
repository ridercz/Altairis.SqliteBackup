namespace Altairis.SqliteBackup.AzureStorage;

public class AzureStorageUploadProcessorOptions {
    private const string DefaultContainerName = "sqlitebackup";

    public AzureStorageUploadProcessorOptions(string connectionString) {
        this.ConnectionString = connectionString;
    }

    public string ConnectionString { get; set; }

    public string ContainerName { get; set; } = DefaultContainerName;

}
