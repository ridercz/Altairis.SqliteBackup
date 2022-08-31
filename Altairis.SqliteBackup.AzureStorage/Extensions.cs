using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup.AzureStorage;

public static class Extensions {
    public static BackupServiceBuilder WithAzureStorageUpload(this BackupServiceBuilder builder, string connectionString, Action<AzureStorageUploadProcessorOptions>? configureOptions = null) {
        var options = new AzureStorageUploadProcessorOptions(connectionString);
        configureOptions?.Invoke(options);
        builder.Services.AddSingleton<IBackupProcessor>(sp => new AzureStorageUploadProcessor(options, sp.GetRequiredService<ILogger<AzureStorageUploadProcessor>>()) { Priority = builder.NextProcessorPriority });
        return builder.Next();
    }
}
