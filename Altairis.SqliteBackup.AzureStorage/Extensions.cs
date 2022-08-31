using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup.AzureStorage;

public static class Extensions {
    public static BackupServiceBuilder WithAzureStorageUpload(this BackupServiceBuilder builder, string connectionString, Action<AzureStorageUploadProcessorOptions>? configureOptions = null) {
        var options = new AzureStorageUploadProcessorOptions(connectionString);
        configureOptions?.Invoke(options);
        return builder.WithProcessor<AzureStorageUploadProcessor>(sp => new AzureStorageUploadProcessor(options, sp.GetRequiredService<ILogger<AzureStorageUploadProcessor>>()));
    }
}
