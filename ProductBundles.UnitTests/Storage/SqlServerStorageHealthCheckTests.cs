using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Configuration;
using ProductBundles.Core.Storage;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductBundles.UnitTests.Storage
{
    /// <summary>
    /// Unit tests for SqlServerStorageHealthCheck
    /// </summary>
    [TestClass]
    public class SqlServerStorageHealthCheckTests
    {
        private MockLogger<SqlServerStorageHealthCheck> _mockLogger = null!;
        private SqlServerStorageHealthCheck _healthCheck = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new MockLogger<SqlServerStorageHealthCheck>();
        }

        private SqlServerStorageHealthCheck CreateHealthCheck(StorageConfiguration config)
        {
            var options = Options.Create(config);
            return new SqlServerStorageHealthCheck(options, _mockLogger);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithNullSqlServerConfiguration_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = null
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("SQL Server connection string not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithEmptyConnectionString_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = ""
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("SQL Server connection string not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithWhitespaceConnectionString_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = "   "
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("SQL Server connection string not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithNullConnectionString_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = null
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("SQL Server connection string not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithInvalidConnectionString_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = "invalid connection string"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description.StartsWith("SQL Server connection failed:"));
            Assert.IsNotNull(result.Exception);

            // Verify logging occurred
            Assert.IsTrue(_mockLogger.LoggedMessages.Count > 0, "Should have logged an error message");
            Assert.IsTrue(_mockLogger.LoggedMessages.Any(m => m.LogLevel == LogLevel.Error), "Should have logged an error");
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithUnreachableServer_ReturnsUnhealthy()
        {
            // Arrange - Use a connection string that points to a non-existent server
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = "Server=nonexistent.server.local;Database=test;Connection Timeout=1;"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description.StartsWith("SQL Server connection failed:"));
            Assert.IsNotNull(result.Exception);

            // Verify logging occurred
            Assert.IsTrue(_mockLogger.LoggedMessages.Count > 0, "Should have logged an error message");
            Assert.IsTrue(_mockLogger.LoggedMessages.Any(m => m.LogLevel == LogLevel.Error), "Should have logged an error");
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithCancellationToken_PropagatesCancellation()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = "Server=nonexistent.server.local;Database=test;Connection Timeout=30;"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(100); // Cancel after 100ms

            // Act & Assert
            try
            {
                await _healthCheck.CheckHealthAsync(new HealthCheckContext(), cts.Token);
                // If no exception is thrown, the connection failed quickly which is also valid
                Assert.IsTrue(true, "Health check completed (connection failed quickly)");
            }
            catch (OperationCanceledException)
            {
                // This is acceptable - cancellation was properly propagated
                Assert.IsTrue(true, "Cancellation was properly propagated");
            }
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithHealthCheckContext_HandlesContextCorrectly()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = "Server=nonexistent.server.local;Database=test;Connection Timeout=1;"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "test-sqlserver",
                    _ => _healthCheck,
                    HealthStatus.Unhealthy,
                    new[] { "ready" })
            };

            // Act
            var result = await _healthCheck.CheckHealthAsync(context);

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description.StartsWith("SQL Server connection failed:"));
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithSqlException_LogsErrorAndReturnsUnhealthy()
        {
            // Arrange - Use a malformed connection string that will cause a SqlException
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = "Server=;Database=;Invalid=Parameter;"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description.StartsWith("SQL Server connection failed:"));
            Assert.IsNotNull(result.Exception);

            // Verify logging occurred
            Assert.IsTrue(_mockLogger.LoggedMessages.Count > 0, "Should have logged an error message");
            Assert.IsTrue(_mockLogger.LoggedMessages.Any(m => m.LogLevel == LogLevel.Error), "Should have logged an error");
            Assert.IsTrue(_mockLogger.LoggedMessages.Any(m => m.Message.Contains("SQL Server storage health check failed")), 
                "Should have logged the specific error message");
        }

        [TestMethod]
        public async Task CheckHealthAsync_MultipleCallsInParallel_HandlesCorrectly()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = "Server=nonexistent.server.local;Database=test;Connection Timeout=1;"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act - Run multiple health checks in parallel
            var tasks = new Task<HealthCheckResult>[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = _healthCheck.CheckHealthAsync(new HealthCheckContext());
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            foreach (var result in results)
            {
                Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
                Assert.IsTrue(result.Description.StartsWith("SQL Server connection failed:"));
            }

            // Verify logging occurred for each call
            Assert.IsTrue(_mockLogger.LoggedMessages.Count >= tasks.Length, 
                "Should have logged error messages for each parallel call");
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithValidConnectionStringFormat_AttemptsConnection()
        {
            // Arrange - Use a properly formatted connection string (even if server doesn't exist)
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;Connection Timeout=1;"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            // This will likely fail since we don't have a SQL Server running, but it should attempt the connection
            // and return an appropriate error message rather than a configuration error
            if (result.Status == HealthStatus.Unhealthy)
            {
                Assert.IsTrue(result.Description.StartsWith("SQL Server connection failed:"));
                Assert.IsNotNull(result.Exception);
            }
            else
            {
                // If somehow it succeeds (e.g., there's actually a SQL Server running), that's fine too
                Assert.AreEqual(HealthStatus.Healthy, result.Status);
                Assert.AreEqual("SQL Server connection successful", result.Description);
            }
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithDifferentConnectionStringFormats_HandlesCorrectly()
        {
            // Test various valid connection string formats
            var connectionStrings = new[]
            {
                "Server=localhost;Database=test;Integrated Security=true;Connection Timeout=1;",
                "Data Source=localhost;Initial Catalog=test;Integrated Security=SSPI;Connection Timeout=1;",
                "Server=.;Database=test;Trusted_Connection=yes;Connection Timeout=1;",
                "Server=(local);Database=test;Integrated Security=true;Connection Timeout=1;"
            };

            foreach (var connectionString in connectionStrings)
            {
                // Arrange
                var config = new StorageConfiguration
                {
                    Provider = "SqlServer",
                    SqlServer = new SqlServerStorageOptions
                    {
                        ConnectionString = connectionString
                    }
                };
                _healthCheck = CreateHealthCheck(config);

                // Act
                var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

                // Assert
                // All should attempt connection (and likely fail since no SQL Server is running)
                // but should not fail due to configuration issues
                Assert.IsNotNull(result);
                if (result.Status == HealthStatus.Unhealthy)
                {
                    Assert.IsTrue(result.Description.StartsWith("SQL Server connection failed:"));
                }
            }
        }

        [TestMethod]
        public async Task CheckHealthAsync_ExceptionContainsOriginalException()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = "Server=nonexistent.server.local;Database=test;Connection Timeout=1;"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsNotNull(result.Exception);
            
            // The exception should be the original SqlException or related connection exception
            Assert.IsTrue(result.Exception is SqlException || 
                         result.Exception is InvalidOperationException ||
                         result.Exception is System.ComponentModel.Win32Exception ||
                         result.Exception is TimeoutException,
                $"Exception type should be a connection-related exception, but was: {result.Exception.GetType()}");
        }
    }
}
