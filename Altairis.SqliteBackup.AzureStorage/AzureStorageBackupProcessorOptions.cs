using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Altairis.SqliteBackup.AzureStorage;

public class AzureStorageBackupProcessorOptions {
    private const string DefaultContainerName = "sqlitebackup";

    public AzureStorageBackupProcessorOptions(string connectionString) {
        this.ConnectionString = connectionString;
    }

    public string ConnectionString { get; set; }

    public string ContainerName { get; set; } = DefaultContainerName;

}
