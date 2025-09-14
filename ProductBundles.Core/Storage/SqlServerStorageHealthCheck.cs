using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;
using ProductBundles.Core.Configuration;

namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// Health check for SQL Server storage provider
    /// </summary>
    public class SqlServerStorageHealthCheck : IHealthCheck
    {
        private readonly StorageConfiguration _storageConfig;
        private readonly ILogger<SqlServerStorageHealthCheck> _logger;

        public SqlServerStorageHealthCheck(
            IOptions<StorageConfiguration> storageConfig,
            ILogger<SqlServerStorageHealthCheck> logger)
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
                var connectionString = _storageConfig.SqlServer?.ConnectionString;

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    return HealthCheckResult.Unhealthy("SQL Server connection string not configured");
                }

                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                
                // Test basic query
                using var command = new SqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(cancellationToken);
                
                return HealthCheckResult.Healthy("SQL Server connection successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL Server storage health check failed");
                return HealthCheckResult.Unhealthy($"SQL Server connection failed: {ex.Message}", ex);
            }
        }
    }
}
