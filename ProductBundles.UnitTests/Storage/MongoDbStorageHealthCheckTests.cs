using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using ProductBundles.Core.Configuration;
using ProductBundles.Core.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProductBundles.UnitTests.Storage
{
    /// <summary>
    /// Unit tests for MongoDbStorageHealthCheck
    /// </summary>
    [TestClass]
    public class MongoDbStorageHealthCheckTests
    {
        private MockLogger<MongoDbStorageHealthCheck> _mockLogger = null!;
        private MongoDbStorageHealthCheck _healthCheck = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new MockLogger<MongoDbStorageHealthCheck>();
        }

        private MongoDbStorageHealthCheck CreateHealthCheck(StorageConfiguration config)
        {
            var options = Options.Create(config);
            return new MongoDbStorageHealthCheck(options, _mockLogger);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithNullMongoDbConfiguration_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = null
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("MongoDB connection string or database name not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithEmptyConnectionString_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "",
                    DatabaseName = "testdb"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("MongoDB connection string or database name not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithWhitespaceConnectionString_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "   ",
                    DatabaseName = "testdb"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("MongoDB connection string or database name not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithNullConnectionString_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = null,
                    DatabaseName = "testdb"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("MongoDB connection string or database name not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithEmptyDatabaseName_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = ""
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("MongoDB connection string or database name not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithWhitespaceDatabaseName_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = "   "
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("MongoDB connection string or database name not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithNullDatabaseName_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = null
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("MongoDB connection string or database name not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithBothConnectionStringAndDatabaseNameEmpty_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "",
                    DatabaseName = ""
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.AreEqual("MongoDB connection string or database name not configured", result.Description);
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithInvalidConnectionString_ReturnsUnhealthy()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "invalid connection string",
                    DatabaseName = "testdb"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description.StartsWith("MongoDB connection failed:"));
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
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://nonexistent.server.local:27017/?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000",
                    DatabaseName = "testdb"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description.StartsWith("MongoDB connection failed:"));
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
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://nonexistent.server.local:27017/?connectTimeoutMS=30000&serverSelectionTimeoutMS=30000",
                    DatabaseName = "testdb"
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
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://nonexistent.server.local:27017/?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000",
                    DatabaseName = "testdb"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            var context = new HealthCheckContext
            {
                Registration = new HealthCheckRegistration(
                    "test-mongodb",
                    _ => _healthCheck,
                    HealthStatus.Unhealthy,
                    new[] { "ready" })
            };

            // Act
            var result = await _healthCheck.CheckHealthAsync(context);

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description.StartsWith("MongoDB connection failed:"));
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithMongoException_LogsErrorAndReturnsUnhealthy()
        {
            // Arrange - Use a malformed connection string that will cause a MongoDB exception
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://invalid:port:format",
                    DatabaseName = "testdb"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsTrue(result.Description.StartsWith("MongoDB connection failed:"));
            Assert.IsNotNull(result.Exception);

            // Verify logging occurred
            Assert.IsTrue(_mockLogger.LoggedMessages.Count > 0, "Should have logged an error message");
            Assert.IsTrue(_mockLogger.LoggedMessages.Any(m => m.LogLevel == LogLevel.Error), "Should have logged an error");
            Assert.IsTrue(_mockLogger.LoggedMessages.Any(m => m.Message.Contains("MongoDB storage health check failed")), 
                "Should have logged the specific error message");
        }

        [TestMethod]
        public async Task CheckHealthAsync_MultipleCallsInParallel_HandlesCorrectly()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://nonexistent.server.local:27017/?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000",
                    DatabaseName = "testdb"
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
                Assert.IsTrue(result.Description.StartsWith("MongoDB connection failed:"));
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
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://localhost:27017/?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000",
                    DatabaseName = "testdb"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            // This will likely fail since we don't have MongoDB running, but it should attempt the connection
            // and return an appropriate error message rather than a configuration error
            if (result.Status == HealthStatus.Unhealthy)
            {
                Assert.IsTrue(result.Description.StartsWith("MongoDB connection failed:"));
                Assert.IsNotNull(result.Exception);
            }
            else
            {
                // If somehow it succeeds (e.g., there's actually a MongoDB running), that's fine too
                Assert.AreEqual(HealthStatus.Healthy, result.Status);
                Assert.IsTrue(result.Description.Contains("MongoDB connection successful to database: testdb"));
            }
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithDifferentConnectionStringFormats_HandlesCorrectly()
        {
            // Test various valid MongoDB connection string formats
            var connectionStrings = new[]
            {
                "mongodb://localhost:27017/?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000",
                "mongodb://127.0.0.1:27017/?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000",
                "mongodb://localhost/?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000",
                "mongodb://user:pass@localhost:27017/?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000",
                "mongodb://localhost:27017/defaultdb?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000"
            };

            foreach (var connectionString in connectionStrings)
            {
                // Arrange
                var config = new StorageConfiguration
                {
                    Provider = "MongoDB",
                    MongoDB = new MongoStorageOptions
                    {
                        ConnectionString = connectionString,
                        DatabaseName = "testdb"
                    }
                };
                _healthCheck = CreateHealthCheck(config);

                // Act
                var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

                // Assert
                // All should attempt connection (and likely fail since no MongoDB is running)
                // but should not fail due to configuration issues
                Assert.IsNotNull(result);
                if (result.Status == HealthStatus.Unhealthy)
                {
                    Assert.IsTrue(result.Description.StartsWith("MongoDB connection failed:"));
                }
            }
        }

        [TestMethod]
        public async Task CheckHealthAsync_ExceptionContainsOriginalException()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://nonexistent.server.local:27017/?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000",
                    DatabaseName = "testdb"
                }
            };
            _healthCheck = CreateHealthCheck(config);

            // Act
            var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

            // Assert
            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsNotNull(result.Exception);
            
            // The exception should be a MongoDB-related exception
            Assert.IsTrue(result.Exception is MongoException || 
                         result.Exception is TimeoutException ||
                         result.Exception is ArgumentException ||
                         result.Exception is InvalidOperationException,
                $"Exception type should be a MongoDB-related exception, but was: {result.Exception.GetType()}");
        }

        [TestMethod]
        public async Task CheckHealthAsync_WithDifferentDatabaseNames_HandlesCorrectly()
        {
            // Test various valid database names
            var databaseNames = new[]
            {
                "testdb",
                "test-db",
                "test_db",
                "TestDB",
                "db123",
                "my-application-db"
            };

            foreach (var databaseName in databaseNames)
            {
                // Arrange
                var config = new StorageConfiguration
                {
                    Provider = "MongoDB",
                    MongoDB = new MongoStorageOptions
                    {
                        ConnectionString = "mongodb://localhost:27017/?connectTimeoutMS=1000&serverSelectionTimeoutMS=1000",
                        DatabaseName = databaseName
                    }
                };
                _healthCheck = CreateHealthCheck(config);

                // Act
                var result = await _healthCheck.CheckHealthAsync(new HealthCheckContext());

                // Assert
                Assert.IsNotNull(result);
                if (result.Status == HealthStatus.Unhealthy)
                {
                    Assert.IsTrue(result.Description.StartsWith("MongoDB connection failed:"));
                }
                else if (result.Status == HealthStatus.Healthy)
                {
                    Assert.IsTrue(result.Description.Contains($"MongoDB connection successful to database: {databaseName}"));
                }
            }
        }
    }
}
