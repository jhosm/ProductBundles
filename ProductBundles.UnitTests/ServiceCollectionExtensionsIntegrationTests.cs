using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Serialization;
using ProductBundles.Core.Storage;

namespace ProductBundles.UnitTests
{
    /// <summary>
    /// Unit tests for ServiceCollectionExtensions integration methods that register multiple services
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsIntegrationTests
    {
        private IServiceCollection _services = null!;
        private IServiceProvider _serviceProvider = null!;
        private string _testStorageDirectory = null!;

        [TestInitialize]
        public void Setup()
        {
            _services = new ServiceCollection();
            _services.AddLogging();
            _testStorageDirectory = Path.Combine(Path.GetTempPath(), "ProductBundlesTests", Guid.NewGuid().ToString());
        }

        [TestCleanup]
        public void Cleanup()
        {            
            // Clean up test directory
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
        public void AddProductBundleInstanceServices_WithDirectory_RegistersAllServices()
        {
            // Act
            var result = _services.AddProductBundleInstanceServices(_testStorageDirectory);

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            _serviceProvider = _services.BuildServiceProvider();
            
            // Verify serialization services
            var jsonOptions = _serviceProvider.GetService<JsonSerializerOptions>();
            Assert.IsNotNull(jsonOptions, "JsonSerializerOptions should be registered");
            
            var serializer = _serviceProvider.GetService<IProductBundleInstanceSerializer>();
            Assert.IsNotNull(serializer, "IProductBundleInstanceSerializer should be registered");
            Assert.IsInstanceOfType(serializer, typeof(JsonProductBundleInstanceSerializer), "Should register JsonProductBundleInstanceSerializer");
            
            // Verify storage services
            var storageOptions = _serviceProvider.GetService<ProductBundleInstanceStorageOptions>();
            Assert.IsNotNull(storageOptions, "ProductBundleInstanceStorageOptions should be registered");
            Assert.AreEqual(_testStorageDirectory, storageOptions.StorageDirectory, "Storage directory should be set correctly");
            
            var storage = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
            Assert.IsInstanceOfType(storage, typeof(FileSystemProductBundleInstanceStorage), "Should register FileSystemProductBundleInstanceStorage");
        }

        [TestMethod]
        public void AddProductBundleInstanceServices_WithDirectoryAndJsonOptions_AppliesConfiguration()
        {
            // Arrange
            bool configureJsonCalled = false;

            // Act
            _services.AddProductBundleInstanceServices(_testStorageDirectory, options =>
            {
                configureJsonCalled = true;
                options.WriteIndented = false;
                options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            });

            // Assert
            Assert.IsTrue(configureJsonCalled, "JSON configuration action should be called");
            
            _serviceProvider = _services.BuildServiceProvider();
            
            var jsonOptions = _serviceProvider.GetService<JsonSerializerOptions>();
            Assert.IsNotNull(jsonOptions, "JsonSerializerOptions should be registered");
            Assert.AreEqual(JsonNamingPolicy.SnakeCaseLower, jsonOptions.PropertyNamingPolicy, "Should apply custom naming policy");
            Assert.IsFalse(jsonOptions.WriteIndented, "Should apply custom indentation setting");
        }

        [TestMethod]
        public void AddProductBundleInstanceServices_WithDirectoryAndNullJsonOptions_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            var result = _services.AddProductBundleInstanceServices(_testStorageDirectory, null);
            
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            _serviceProvider = _services.BuildServiceProvider();
            var jsonOptions = _serviceProvider.GetService<JsonSerializerOptions>();
            Assert.IsNotNull(jsonOptions, "JsonSerializerOptions should still be registered with default configuration");
        }

        [TestMethod]
        public void AddProductBundleInstanceServices_WithStorageConfiguration_RegistersAllServices()
        {
            // Arrange
            bool configureStorageCalled = false;

            // Act
            var result = _services.AddProductBundleInstanceServices(options =>
            {
                configureStorageCalled = true;
                options.StorageDirectory = _testStorageDirectory;
                options.MaxConcurrentOperations = 5;
                options.CreateDirectoryIfNotExists = true;
            });

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            Assert.IsTrue(configureStorageCalled, "Storage configuration action should be called");
            
            _serviceProvider = _services.BuildServiceProvider();
            
            // Verify all services are registered
            var serializer = _serviceProvider.GetService<IProductBundleInstanceSerializer>();
            Assert.IsNotNull(serializer, "IProductBundleInstanceSerializer should be registered");
            
            var storage = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
        }

        [TestMethod]
        public void AddProductBundleInstanceServices_WithStorageAndJsonConfiguration_AppliesBothConfigurations()
        {
            // Arrange
            bool configureStorageCalled = false;
            bool configureJsonCalled = false;

            // Act
            _services.AddProductBundleInstanceServices(
                storageOptions =>
                {
                    configureStorageCalled = true;
                    storageOptions.StorageDirectory = _testStorageDirectory;
                },
                jsonOptions =>
                {
                    configureJsonCalled = true;
                    jsonOptions.WriteIndented = false;
                });

            // Assert
            Assert.IsTrue(configureStorageCalled, "Storage configuration action should be called");
            Assert.IsTrue(configureJsonCalled, "JSON configuration action should be called");
            
            _serviceProvider = _services.BuildServiceProvider();
            
            // Verify JSON configuration applied
            var jsonOptions = _serviceProvider.GetService<JsonSerializerOptions>();
            Assert.IsNotNull(jsonOptions, "JsonSerializerOptions should be registered");
            Assert.IsFalse(jsonOptions.WriteIndented, "Should apply custom JSON configuration");
            
            // Verify both services are registered
            var serializer = _serviceProvider.GetService<IProductBundleInstanceSerializer>();
            Assert.IsNotNull(serializer, "IProductBundleInstanceSerializer should be registered");
            
            var storage = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProductBundleInstanceServices_WithEmptyStorageDirectory_ThrowsException()
        {
            // Act & Assert
            _services.AddProductBundleInstanceServices((string)string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddProductBundleInstanceServices_WithNullStorageConfiguration_ThrowsException()
        {
            // Act & Assert
            _services.AddProductBundleInstanceServices((Action<ProductBundleInstanceStorageOptions>)null!);
        }

        [TestMethod]
        public void AddProductBundleInstanceServices_ServicesAreSingletons()
        {
            // Arrange
            _services.AddProductBundleInstanceServices(_testStorageDirectory);
            _serviceProvider = _services.BuildServiceProvider();

            // Act & Assert - Test serializer singleton
            var serializer1 = _serviceProvider.GetService<IProductBundleInstanceSerializer>();
            var serializer2 = _serviceProvider.GetService<IProductBundleInstanceSerializer>();
            Assert.AreSame(serializer1, serializer2, "IProductBundleInstanceSerializer should be singleton");

            // Act & Assert - Test storage singleton
            var storage1 = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            var storage2 = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            Assert.AreSame(storage1, storage2, "IProductBundleInstanceStorage should be singleton");

            // Act & Assert - Test JSON options singleton
            var options1 = _serviceProvider.GetService<JsonSerializerOptions>();
            var options2 = _serviceProvider.GetService<JsonSerializerOptions>();
            Assert.AreSame(options1, options2, "JsonSerializerOptions should be singleton");
        }

        [TestMethod]
        public void AddProductBundleInstanceServices_CalledMultipleTimes_RegistersOnlyOnce()
        {
            // Act
            _services.AddProductBundleInstanceServices(_testStorageDirectory);
            _services.AddProductBundleInstanceServices(_testStorageDirectory + "2");

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            
            var serializers = _serviceProvider.GetServices<IProductBundleInstanceSerializer>().ToList();
            Assert.AreEqual(1, serializers.Count, "Should register serializer only once");
            
            var storages = _serviceProvider.GetServices<IProductBundleInstanceStorage>().ToList();
            Assert.AreEqual(1, storages.Count, "Should register storage only once");
        }

        [TestMethod]
        public void AddProductBundleInstanceServices_IntegrationTest_ServicesWorkTogether()
        {
            // Arrange
            _services.AddProductBundleInstanceServices(_testStorageDirectory);
            _serviceProvider = _services.BuildServiceProvider();

            // Act
            var storage = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            var serializer = _serviceProvider.GetService<IProductBundleInstanceSerializer>();

            // Assert
            Assert.IsNotNull(storage, "Storage should be available");
            Assert.IsNotNull(serializer, "Serializer should be available");
            
            // The storage should be able to use the serializer (integration test)
            // This verifies that the dependency injection is working correctly
            Assert.IsInstanceOfType(storage, typeof(FileSystemProductBundleInstanceStorage), "Storage should be FileSystemProductBundleInstanceStorage");
        }
    }
}
