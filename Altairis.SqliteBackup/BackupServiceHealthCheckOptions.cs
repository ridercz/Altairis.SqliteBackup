namespace Altairis.SqliteBackup;

public class BackupServiceHealthCheckOptions {

    public TimeSpan HealthyThreshold { get; set; } = TimeSpan.Zero;

    public TimeSpan DegradedThreshold { get; set; } = TimeSpan.Zero;

}
