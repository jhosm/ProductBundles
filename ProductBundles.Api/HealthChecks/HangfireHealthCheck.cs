using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Hangfire;
using Hangfire.Storage;

namespace ProductBundles.Api.HealthChecks
{
    /// <summary>
    /// Health check for Hangfire background job system
    /// </summary>
    public class HangfireHealthCheck : IHealthCheck
    {
        private readonly ILogger<HangfireHealthCheck> _logger;

        public HangfireHealthCheck(ILogger<HangfireHealthCheck> logger)
        {
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if Hangfire storage is accessible
                var storage = JobStorage.Current;
                if (storage == null)
                {
                    return HealthCheckResult.Unhealthy("Hangfire storage not configured");
                }

                // Get monitoring API to check system status
                var monitoringApi = storage.GetMonitoringApi();
                
                // Check server statistics
                var statistics = monitoringApi.GetStatistics();
                
                var serverCount = statistics.Servers;
                var enqueuedJobs = statistics.Enqueued;
                var failedJobs = statistics.Failed;
                var processingJobs = statistics.Processing;
                var succeededJobs = statistics.Succeeded;

                // Build health status message
                var statusMessage = $"Hangfire operational - Servers: {serverCount}, " +
                                  $"Enqueued: {enqueuedJobs}, Processing: {processingJobs}, " +
                                  $"Failed: {failedJobs}, Succeeded: {succeededJobs}";

                // Consider it degraded if there are no servers running
                if (serverCount == 0)
                {
                    return HealthCheckResult.Degraded($"No Hangfire servers running - {statusMessage}");
                }

                // Consider it degraded if there are many failed jobs
                if (failedJobs > 10)
                {
                    return HealthCheckResult.Degraded($"High number of failed jobs - {statusMessage}");
                }

                return HealthCheckResult.Healthy(statusMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hangfire health check failed");
                return HealthCheckResult.Unhealthy($"Hangfire health check failed: {ex.Message}", ex);
            }
        }
    }
}
