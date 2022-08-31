using System.IO.Compression;
using Microsoft.Extensions.Logging;

namespace Altairis.SqliteBackup.BackupProcessors;

public class GZipProcessor : IBackupProcessor {
    private readonly GZipProcessorOptions options;
    private readonly ILogger<GZipProcessor> logger;

    public GZipProcessor(GZipProcessorOptions options, ILogger<GZipProcessor> logger) {
        this.options = options;
        this.logger = logger;
    }

    public int Priority { get; set; }

    public async Task<string> ProcessBackupFile(string backupFilePath, CancellationToken cancellationToken) {
        // Open source file
        var inputFile = new FileInfo(backupFilePath);
        var inputStream = inputFile.OpenRead();

        // Create temp file
        var outputFile = new FileInfo(backupFilePath + this.options.AddExtension);
        var outputStream = outputFile.Create();

        // Create GZip compression and compress data
        var gzipStream = new GZipStream(outputStream, this.options.CompressionLevel);
        await inputStream.CopyToAsync(gzipStream, cancellationToken);

        // Close all streams
        gzipStream.Close();
        outputStream.Close();
        inputStream.Close();

        // Log result
        this.logger.LogInformation("Compressed {inputFileName} ({inputSize} bytes) to {outputFileName} ({outputSize} bytes).",
            inputFile.FullName,
            inputFile.Length,
            outputFile.FullName,
            outputFile.Length);

        return outputFile.FullName;
    }
}

public class GZipProcessorOptions {

    private const string DefaultAddExtension = ".gz";

    public CompressionLevel CompressionLevel { get; set; } = CompressionLevel.Optimal;

    public string AddExtension { get; set; } = DefaultAddExtension;

}