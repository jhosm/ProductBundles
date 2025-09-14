using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using ProductBundles.Core.Configuration;

namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// Health check for MongoDB storage provider
    /// </summary>
    public class MongoDbStorageHealthCheck : IHealthCheck
    {
        private readonly StorageConfiguration _storageConfig;
        private readonly ILogger<MongoDbStorageHealthCheck> _logger;

        public MongoDbStorageHealthCheck(
            IOptions<StorageConfiguration> storageConfig,
            ILogger<MongoDbStorageHealthCheck> logger)
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
                var connectionString = _storageConfig.MongoDB?.ConnectionString;
                var databaseName = _storageConfig.MongoDB?.DatabaseName;

                if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(databaseName))
                {
                    return HealthCheckResult.Unhealthy("MongoDB connection string or database name not configured");
                }

                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                
                // Test connection by pinging the database
                await database.RunCommandAsync<object>("{ ping: 1 }", cancellationToken: cancellationToken);
                
                return HealthCheckResult.Healthy($"MongoDB connection successful to database: {databaseName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MongoDB storage health check failed");
                return HealthCheckResult.Unhealthy($"MongoDB connection failed: {ex.Message}", ex);
            }
        }
    }
}
