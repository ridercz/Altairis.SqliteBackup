using Microsoft.Extensions.DependencyInjection;

namespace Altairis.SqliteBackup;

public static class Extensions {

    public static void AddSqliteBackup(this IServiceCollection services, string connectionString, Action<BackupServiceOptions>? configureOptions = null) {
        var options = new BackupServiceOptions(connectionString);
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        services.AddHostedService<BackupService>();
    }

    public static void AddSqliteBackupHttpUpload(this IServiceCollection services, Uri targetUri, Action<HttpUploadBackupProcessorOptions>? configureOptions = null) {
        var options = new HttpUploadBackupProcessorOptions(targetUri);
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        services.AddSingleton<IBackupProcessor, HttpUploadBackupProcessor>();
    }
}
