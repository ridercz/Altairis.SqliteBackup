using Microsoft.Extensions.DependencyInjection;

namespace Altairis.SqliteBackup;

public class BackupServiceBuilder {

    public BackupServiceBuilder(IServiceCollection services, BackupServiceOptions serviceOptions, int nextProcessorPriority = 0) {
        this.Services = services;
        this.ServiceOptions = serviceOptions;
        this.NextProcessorPriority = nextProcessorPriority;
    }

    public IServiceCollection Services { get; }

    public BackupServiceOptions ServiceOptions { get; }

    public int NextProcessorPriority { get; } = 0;

    public BackupServiceBuilder Next() {
        return new BackupServiceBuilder(this.Services, this.ServiceOptions, this.NextProcessorPriority + 1);
    }

}
