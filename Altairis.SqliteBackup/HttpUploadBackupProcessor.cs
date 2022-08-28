using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup;
public class HttpUploadBackupProcessor : IBackupProcessor {
    private readonly HttpUploadBackupProcessorOptions options;
    private readonly ILogger<HttpUploadBackupProcessor> logger;

    public HttpUploadBackupProcessor(HttpUploadBackupProcessorOptions options, ILogger<HttpUploadBackupProcessor> logger) {
        this.options = options;
        this.logger = logger;
    }

    public async Task ProcessBackupFile(string backupFilePath) {
        this.logger.LogInformation("Uploading file {backupFilePath} to {targetUrl}.", backupFilePath, this.options.TargetUri);

        // Prepare new StreamContent
        using var file = File.OpenRead(backupFilePath);
        using var sc = new StreamContent(file);
        sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(this.options.ContentType);

        // Prepare HTTP request
        using var data = new MultipartFormDataContent();
        data.Add(sc, this.options.FieldName, Path.GetFileName(backupFilePath));

        // POST data
        using var client = this.options.GetHttpClient();
        try {
            var response = await client.PostAsync(this.options.TargetUri, data);
            response.EnsureSuccessStatusCode();
            this.logger.LogInformation("File {backupFilePath} was successfully uploaded to {targetUrl}.", backupFilePath, this.options.TargetUri);
        } catch (Exception ex) {
            this.logger.LogError(ex, "Error while uploading {backupFilePath} to {targetUrl}.", backupFilePath, this.options.TargetUri);
        }
    }

}

