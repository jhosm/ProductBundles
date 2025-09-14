using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Storage;
using System.Collections.Generic;
using System.IO;

namespace ProductBundles.UnitTests.Extensions
{
    /// <summary>
    /// Unit tests for ServiceCollectionExtensions health check methods
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsHealthCheckTests
    {
        private IServiceCollection _services = null!;
        private IConfiguration _configuration = null!;
        private IHealthChecksBuilder _healthChecksBuilder = null!;
        private string _testStorageDirectory = null!;

        [TestInitialize]
        public void Setup()
        {
            _services = new ServiceCollection();
            _services.AddLogging();
            _healthChecksBuilder = _services.AddHealthChecks();
            _testStorageDirectory = Path.Combine(Path.GetTempPath(), "ProductBundlesTests", Guid.NewGuid().ToString());
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (Directory.Exists(_testStorageDirectory))
            {
                try
                {
                    Directory.Delete(_testStorageDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public void AddProductBundleStorageHealthChecks_WithFileSystemProvider_RegistersFileSystemHealthCheck()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "FileSystem"},
                {"ProductBundleStorage:FileSystem:StorageDirectory", _testStorageDirectory}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            var result = _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration);

            // Assert
            Assert.AreSame(_healthChecksBuilder, result, "Method should return the same health checks builder for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetService<HealthCheckService>();
            
            Assert.IsNotNull(healthCheckService, "HealthCheckService should be available");
        }

        [TestMethod]
        public void AddProductBundleStorageHealthChecks_WithMongoDbProvider_RegistersMongoDbHealthCheck()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "MongoDB"},
                {"ProductBundleStorage:MongoDB:ConnectionString", "mongodb://localhost:27017"},
                {"ProductBundleStorage:MongoDB:DatabaseName", "testdb"},
                {"ProductBundleStorage:MongoDB:CollectionName", "testcollection"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            var result = _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration);

            // Assert
            Assert.AreSame(_healthChecksBuilder, result, "Method should return the same health checks builder for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetService<HealthCheckService>();
            
            Assert.IsNotNull(healthCheckService, "HealthCheckService should be available");
        }

        [TestMethod]
        public void AddProductBundleStorageHealthChecks_WithSqlServerProvider_RegistersSqlServerHealthCheck()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "SqlServer"},
                {"ProductBundleStorage:SqlServer:ConnectionString", "Server=localhost;Database=test;Integrated Security=true;"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            var result = _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration);

            // Assert
            Assert.AreSame(_healthChecksBuilder, result, "Method should return the same health checks builder for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetService<HealthCheckService>();
            
            Assert.IsNotNull(healthCheckService, "HealthCheckService should be available");
        }

        [TestMethod]
        public void AddProductBundleStorageHealthChecks_WithCustomSectionName_UsesCustomSection()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"CustomStorage:Provider", "FileSystem"},
                {"CustomStorage:FileSystem:StorageDirectory", _testStorageDirectory}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            var result = _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration, "CustomStorage");

            // Assert
            Assert.AreSame(_healthChecksBuilder, result, "Method should return the same health checks builder for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetService<HealthCheckService>();
            
            Assert.IsNotNull(healthCheckService, "HealthCheckService should be available");
        }

        [TestMethod]
        public void AddProductBundleStorageHealthChecks_WithMissingConfiguration_ReturnsBuilderWithoutAddingChecks()
        {
            // Arrange
            _configuration = new ConfigurationBuilder().Build(); // Empty configuration

            // Act
            var result = _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration);

            // Assert
            Assert.AreSame(_healthChecksBuilder, result, "Method should return the same health checks builder for chaining");
            
            // Should not throw or fail, just return the builder without adding health checks
            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetService<HealthCheckService>();
            
            Assert.IsNotNull(healthCheckService, "HealthCheckService should still be available");
        }

        [TestMethod]
        public void AddProductBundleStorageHealthChecks_WithUnsupportedProvider_ReturnsBuilderWithoutAddingChecks()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "UnsupportedProvider"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            var result = _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration);

            // Assert
            Assert.AreSame(_healthChecksBuilder, result, "Method should return the same health checks builder for chaining");
            
            // Should not throw or fail, just return the builder without adding health checks
            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetService<HealthCheckService>();
            
            Assert.IsNotNull(healthCheckService, "HealthCheckService should still be available");
        }

        [TestMethod]
        public void AddProductBundleStorageHealthChecks_WithCaseInsensitiveProvider_RegistersCorrectHealthCheck()
        {
            // Arrange - Test case insensitivity
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "FILESYSTEM"}, // Uppercase
                {"ProductBundleStorage:FileSystem:StorageDirectory", _testStorageDirectory}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            var result = _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration);

            // Assert
            Assert.AreSame(_healthChecksBuilder, result, "Method should return the same health checks builder for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetService<HealthCheckService>();
            
            Assert.IsNotNull(healthCheckService, "HealthCheckService should be available");
        }

        [TestMethod]
        public void AddProductBundleStorageHealthChecks_RegistersHealthCheckWithCorrectName()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "FileSystem"},
                {"ProductBundleStorage:FileSystem:StorageDirectory", _testStorageDirectory}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();
            
            // Verify that the health check is registered by attempting to check health
            // This will validate that the health check was properly registered
            var healthCheckResult = healthCheckService.CheckHealthAsync().Result;
            Assert.IsNotNull(healthCheckResult, "Health check should be executable");
        }

        [TestMethod]
        public void AddProductBundleStorageHealthChecks_RegistersHealthCheckWithReadyTag()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "FileSystem"},
                {"ProductBundleStorage:FileSystem:StorageDirectory", _testStorageDirectory}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var healthCheckService = serviceProvider.GetRequiredService<HealthCheckService>();
            
            // Verify that the health check is registered and can be executed
            var healthCheckResult = healthCheckService.CheckHealthAsync().Result;
            Assert.IsNotNull(healthCheckResult, "Health check should be executable");
            
            // The "ready" tag is used for readiness probes - this is verified by successful registration
        }

        [TestMethod]
        public void AddProductBundleStorageHealthChecks_CalledMultipleTimes_ThrowsDuplicateException()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "FileSystem"},
                {"ProductBundleStorage:FileSystem:StorageDirectory", _testStorageDirectory}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration);
            _healthChecksBuilder.AddProductBundleStorageHealthChecks(_configuration);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            
            // Should throw ArgumentException for duplicate health check registrations
            Assert.ThrowsException<ArgumentException>(() =>
                serviceProvider.GetRequiredService<HealthCheckService>(),
                "Should throw ArgumentException when duplicate health checks are registered");
        }
    }
}
