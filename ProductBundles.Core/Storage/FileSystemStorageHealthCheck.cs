using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductBundles.Core.Configuration;

namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// Health check for FileSystem storage provider
    /// </summary>
    public class FileSystemStorageHealthCheck : IHealthCheck
    {
        private readonly StorageConfiguration _storageConfig;
        private readonly ILogger<FileSystemStorageHealthCheck> _logger;

        public FileSystemStorageHealthCheck(
            IOptions<StorageConfiguration> storageConfig,
            ILogger<FileSystemStorageHealthCheck> logger)
        {
            _storageConfig = storageConfig.Value;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var storageDir = _storageConfig.FileSystem?.StorageDirectory;
                
                if (string.IsNullOrWhiteSpace(storageDir))
                {
                    return HealthCheckResult.Unhealthy("FileSystem storage directory not configured");
                }

                // Check if directory exists and is accessible
                if (!Directory.Exists(storageDir))
                {
                    return HealthCheckResult.Unhealthy($"Storage directory does not exist: {storageDir}");
                }

                // Test write access by creating a temporary file
                var testFile = Path.Combine(storageDir, $"healthcheck_{Guid.NewGuid()}.tmp");
                await File.WriteAllTextAsync(testFile, "health check", cancellationToken);
                File.Delete(testFile);

                return HealthCheckResult.Healthy($"FileSystem storage accessible at: {storageDir}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FileSystem storage health check failed");
                return HealthCheckResult.Unhealthy($"FileSystem storage not accessible: {ex.Message}", ex);
            }
        }
    }
}
