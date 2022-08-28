namespace Altairis.SqliteBackup;

public class HttpUploadBackupProcessorOptions {

    public HttpUploadBackupProcessorOptions(Uri targetUri) {
        this.TargetUri = targetUri;
    }

    public Uri TargetUri { get; set; }

    public string ContentType { get; set; } = "application/vnd.sqlite3";

    public string FieldName { get; set; } = "backupFile";

    public Func<HttpClient> GetHttpClient { get; set; } = () => new HttpClient();

}
