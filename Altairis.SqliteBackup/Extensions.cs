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

    public static BackupServiceBuilder WithProcessor<TProcessor>(this BackupServiceBuilder builder, Func<IServiceProvider, IBackupProcessor> implementationFactory) where TProcessor: IBackupProcessor {
        builder.Services.AddSingleton<IBackupProcessor>(sp => {
            var p = implementationFactory(sp);
            p.Priority = builder.NextProcessorPriority;
            return p;
        });
        return builder.Next();
    }

    // Specific processor implementation

    public static BackupServiceBuilder WithFileCleanup(this BackupServiceBuilder builder, string mask, int fileCount) => builder.WithProcessor<FileCleanupProcessor>(sp => new FileCleanupProcessor(mask, fileCount, sp.GetRequiredService<ILogger<FileCleanupProcessor>>()));

    public static BackupServiceBuilder WithGZip(this BackupServiceBuilder builder, Action<GZipProcessorOptions>? configureOptions = null) {
        var options = new GZipProcessorOptions();
        configureOptions?.Invoke(options);
        return builder.WithProcessor<GZipProcessor>(sp => new GZipProcessor(options, sp.GetRequiredService<ILogger<GZipProcessor>>()));
    }

    public static BackupServiceBuilder WithHttpUpload(this BackupServiceBuilder builder, string targetUri, Action<HttpUploadProcessorOptions>? configureOptions = null) => builder.WithHttpUpload(new Uri(targetUri), configureOptions);

    public static BackupServiceBuilder WithHttpUpload(this BackupServiceBuilder builder, Uri targetUri, Action<HttpUploadProcessorOptions>? configureOptions = null) {
        var options = new HttpUploadProcessorOptions(targetUri);
        configureOptions?.Invoke(options);
        return builder.WithProcessor<HttpUploadProcessor>(sp => new HttpUploadProcessor(options, sp.GetRequiredService<ILogger<HttpUploadProcessor>>()));
    }

}
