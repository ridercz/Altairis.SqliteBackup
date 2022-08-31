using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup.AzureStorage;
public class AzureStorageUploadProcessor : IBackupProcessor {

    private readonly AzureStorageUploadProcessorOptions options;
    private readonly ILogger<AzureStorageUploadProcessor> logger;

    public AzureStorageUploadProcessor(AzureStorageUploadProcessorOptions options, ILogger<AzureStorageUploadProcessor> logger) {
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
            var options = new BlobUploadOptions {
                HttpHeaders = new BlobHttpHeaders {
                    ContentType = this.options.ContentType
                }
            };
            _ = await blob.UploadAsync(backupFilePath, options, cancellationToken);
        } catch (Exception ex) {
            this.logger.LogError(ex, "Exception while uploading backup file to Azure Storage.");
        }
        return backupFilePath;
    }
}
