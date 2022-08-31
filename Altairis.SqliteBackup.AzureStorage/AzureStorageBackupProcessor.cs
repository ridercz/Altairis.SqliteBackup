using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup.AzureStorage;
public class AzureStorageBackupProcessor : IBackupProcessor {

    private readonly AzureStorageBackupProcessorOptions options;
    private readonly ILogger<AzureStorageBackupProcessor> logger;

    public AzureStorageBackupProcessor(AzureStorageBackupProcessorOptions options, ILogger<AzureStorageBackupProcessor> logger) {
        this.options = options;
        this.logger = logger;
    }

    public int Priority { get; set; }

    public async Task<string> ProcessBackupFile(string backupFilePath, CancellationToken cancellationToken) {
        try {
            // Get or create container
            var container = new BlobContainerClient(this.options.ConnectionString, this.options.ContainerName);
            _ = await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            // Create a blob
            var blob = container.GetBlobClient(Path.GetFileName(backupFilePath));
            this.logger.LogInformation("Uploading {backupFilePath} to blob {blobUri}.", backupFilePath, blob.Uri.AbsoluteUri);
            _ = await blob.UploadAsync(backupFilePath, cancellationToken);
        } catch (Exception ex) {
            this.logger.LogError(ex, "Exception while uploading backup file to Azure Storage.");
        }
        return backupFilePath;
    }
}
