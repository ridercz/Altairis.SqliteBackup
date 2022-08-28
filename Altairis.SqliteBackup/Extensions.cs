using Microsoft.Extensions.DependencyInjection;

namespace Altairis.SqliteBackup;

public static class Extensions {

    public static SqliteBackupBuilder AddSqliteBackupService(this IServiceCollection services, string connectionString, Action<BackupServiceOptions>? configureOptions = null) {
        var options = new BackupServiceOptions(connectionString);
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        services.AddHostedService<BackupService>();
        return new SqliteBackupBuilder { Services = services };
    }

    public static SqliteBackupBuilder WithHttpUpload(this SqliteBackupBuilder builder, Uri targetUri, Action<HttpUploadBackupProcessorOptions>? configureOptions = null) {
        var options = new HttpUploadBackupProcessorOptions(targetUri);
        configureOptions?.Invoke(options);
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton<IBackupProcessor, HttpUploadBackupProcessor>();
        return builder;
    }
}

public class SqliteBackupBuilder {

    public IServiceCollection Services { get; set; } = null!;

}