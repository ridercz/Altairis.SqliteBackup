using Altairis.SqliteBackup.BackupProcessors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup;

public static class Extensions {

    public static BackupServiceBuilder AddSqliteBackup(this IServiceCollection services, string connectionString, Action<BackupServiceOptions>? configureOptions = null) {
        var options = new BackupServiceOptions(connectionString);
        configureOptions?.Invoke(options);
        services.AddSingleton(options);
        services.AddHostedService<BackupService>();
        return new BackupServiceBuilder(services, options);
    }

    public static BackupServiceBuilder WithFileCleanup(this BackupServiceBuilder builder, string mask, int fileCount) {
        builder.Services.AddSingleton<IBackupProcessor>(sp => new FileCleanupProcessor(mask, fileCount, sp.GetRequiredService<ILogger<FileCleanupProcessor>>()) { Priority = builder.NextProcessorPriority });
        return builder.Next();
    }

    public static BackupServiceBuilder WithGZip(this BackupServiceBuilder builder, Action<GZipProcessorOptions>? configureOptions = null) {
        var options = new GZipProcessorOptions();
        configureOptions?.Invoke(options);
        builder.Services.AddSingleton<IBackupProcessor>(sp => new GZipProcessor(options, sp.GetRequiredService<ILogger<GZipProcessor>>()) { Priority = builder.NextProcessorPriority });
        return builder.Next();
    }

    public static BackupServiceBuilder WithHttpUpload(this BackupServiceBuilder builder, string targetUri, Action<HttpUploadProcessorOptions>? configureOptions = null) => builder.WithHttpUpload(new Uri(targetUri), configureOptions);

    public static BackupServiceBuilder WithHttpUpload(this BackupServiceBuilder builder, Uri targetUri, Action<HttpUploadProcessorOptions>? configureOptions = null) {
        var options = new HttpUploadProcessorOptions(targetUri);
        configureOptions?.Invoke(options);
        builder.Services.AddSingleton<IBackupProcessor>(sp => new HttpUploadProcessor(options, sp.GetRequiredService<ILogger<HttpUploadProcessor>>()) { Priority = builder.NextProcessorPriority });
        return builder.Next();
    }

}
