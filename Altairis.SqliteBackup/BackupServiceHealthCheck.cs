using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Altairis.SqliteBackup;

public class BackupServiceHealthCheck : IHealthCheck {
    private const float DefaultHealthyThreshold = 1.25f;
    private const float DefaultDegradedThreshold = 2.25f;

    private readonly ILogger<BackupServiceHealthCheck> logger;
    private readonly TimeSpan healthyThreshold, degradedThreshold, checkInterval;
    private readonly DateTime startTime = DateTime.Now;
    private DateTime lastSuccessTime = DateTime.MinValue;
    private bool updateReceived = false;
    private string? lastMessage;
    private Exception? lastException;

    public BackupServiceHealthCheck(BackupServiceOptions serviceOptions, ILogger<BackupServiceHealthCheck> logger, IOptions<BackupServiceHealthCheckOptions>? optionsAccessor = null) {
        var options = optionsAccessor?.Value ?? new();

        if (options.HealthyThreshold > options.DegradedThreshold) throw new ArgumentException("Healthy threshold must be less than or equal to degraded threshold.");

        this.healthyThreshold = options.HealthyThreshold > TimeSpan.Zero ? options.HealthyThreshold : serviceOptions.BackupInterval * DefaultHealthyThreshold;
        this.degradedThreshold = options.DegradedThreshold > TimeSpan.Zero ? options.DegradedThreshold : serviceOptions.BackupInterval * DefaultDegradedThreshold;
        this.checkInterval = serviceOptions.CheckInterval;
        this.logger = logger;
    }

    public void Update(bool success, string? message = null, Exception? exception = null) {
        this.updateReceived = true;
        this.lastMessage = message;
        this.lastException = exception;
        if (success) this.lastSuccessTime = DateTime.Now;
        this.logger.LogDebug("Health check updated: {State}, {Message}", success ? "OK" : "ERROR", message);
    }

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) {
        // Handle special case - too fast check
        if (!this.updateReceived && DateTime.Now - this.startTime < this.checkInterval) {
            this.logger.LogDebug("Too fast check, returning degraded status");
            return Task.FromResult(new HealthCheckResult(HealthStatus.Degraded, $"Backup check was not performed yet, try again in {this.checkInterval}."));
        }

        // Derive status from last success time
        var timeSinceLastSuccess = DateTime.Now - this.lastSuccessTime;
        HealthStatus status;
        if (timeSinceLastSuccess <= this.healthyThreshold) {
            status = HealthStatus.Healthy;
        } else if (timeSinceLastSuccess <= this.degradedThreshold) {
            status = HealthStatus.Degraded;
        } else {
            status = HealthStatus.Unhealthy;
        }

        // Create result with information
        var result = new HealthCheckResult(status, this.lastMessage, this.lastException, data: new Dictionary<string, object> {
                       { "LastSuccessTime", this.lastSuccessTime },
                       { "TimeSinceLastSuccess", timeSinceLastSuccess },
                       { "HealthyThreshold", this.healthyThreshold },
                       { "DegradedThreshold", this.degradedThreshold }
                   });
        return Task.FromResult(result);
    }
}
