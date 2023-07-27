using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup.BackupProcessors;

public class HttpUploadProcessor : IBackupProcessor {
    private readonly HttpUploadProcessorOptions options;
    private readonly ILogger<HttpUploadProcessor> logger;

    public HttpUploadProcessor(HttpUploadProcessorOptions options, ILogger<HttpUploadProcessor> logger) {
        this.options = options;
        this.logger = logger;
    }

    public int Priority { get; set; }

    public async Task<string> ProcessBackupFile(string backupFilePath, CancellationToken cancellationToken) {
        this.logger.LogInformation("Uploading file {backupFilePath} to {targetUrl}.", backupFilePath, this.options.TargetUri);

        // Prepare new StreamContent
        using var file = File.OpenRead(backupFilePath);
        using var sc = new StreamContent(file);
        sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(this.options.ContentType);

        // Prepare HTTP request
        using var data = new MultipartFormDataContent {
            { sc, this.options.FieldName, Path.GetFileName(backupFilePath) }
        };

        // POST data
        using var client = this.options.GetHttpClient();
        var response = await client.PostAsync(this.options.TargetUri, data, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        this.logger.LogInformation("File {backupFilePath} was successfully uploaded to {targetUrl}.", backupFilePath, this.options.TargetUri);
        return backupFilePath;
    }

}

public class HttpUploadProcessorOptions {
    private const string DefaultContentType = "application/vnd.sqlite3";
    private const string DefaultFieldName = "backupFile";

    public HttpUploadProcessorOptions(Uri targetUri) {
        this.TargetUri = targetUri;
    }

    public Uri TargetUri { get; set; }

    public string ContentType { get; set; } = DefaultContentType;

    public string FieldName { get; set; } = DefaultFieldName;

    public Func<HttpClient> GetHttpClient { get; set; } = () => new HttpClient();

}
