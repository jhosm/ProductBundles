using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Configuration;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Storage;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace ProductBundles.UnitTests.Extensions
{
    /// <summary>
    /// Unit tests for ServiceCollectionExtensions configuration-based methods
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsConfigurationTests
    {
        private IServiceCollection _services = null!;
        private IConfiguration _configuration = null!;
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
        public void AddProductBundleStorageFromConfiguration_WithFileSystemProvider_RegistersFileSystemStorage()
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
            _services.AddProductBundleJsonSerialization(); // Add required serializer
            var result = _services.AddProductBundleStorageFromConfiguration(_configuration);

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var storage = serviceProvider.GetService<IProductBundleInstanceStorage>();
            var storageConfig = serviceProvider.GetService<IOptions<StorageConfiguration>>();
            
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
            Assert.IsInstanceOfType(storage, typeof(FileSystemProductBundleInstanceStorage), "Should register FileSystemProductBundleInstanceStorage");
            Assert.IsNotNull(storageConfig, "StorageConfiguration should be registered");
            Assert.AreEqual("FileSystem", storageConfig.Value.Provider, "Provider should be FileSystem");
        }

        [TestMethod]
        public void AddProductBundleStorageFromConfiguration_WithMongoDbProvider_RegistersMongoStorage()
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
            _services.AddProductBundleStorageFromConfiguration(_configuration);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var storage = serviceProvider.GetService<IProductBundleInstanceStorage>();
            var storageConfig = serviceProvider.GetService<IOptions<StorageConfiguration>>();
            
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
            Assert.IsInstanceOfType(storage, typeof(MongoProductBundleInstanceStorage), "Should register MongoProductBundleInstanceStorage");
            Assert.IsNotNull(storageConfig, "StorageConfiguration should be registered");
            Assert.AreEqual("MongoDB", storageConfig.Value.Provider, "Provider should be MongoDB");
        }

        [TestMethod]
        public void AddProductBundleStorageFromConfiguration_WithSqlServerProvider_RegistersSqlServerStorage()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "SqlServer"},
                {"ProductBundleStorage:SqlServer:ConnectionString", "Server=localhost;Database=TestDB;Integrated Security=true;"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act
            _services.AddProductBundleStorageFromConfiguration(_configuration);

            // Assert - Check service registration without instantiating (to avoid DB connection)
            var serviceProvider = _services.BuildServiceProvider();
            var serviceDescriptor = _services.FirstOrDefault(s => s.ServiceType == typeof(IProductBundleInstanceStorage));
            
            Assert.IsNotNull(serviceDescriptor, "IProductBundleInstanceStorage should be registered");
            Assert.AreEqual(ServiceLifetime.Singleton, serviceDescriptor.Lifetime, "Should be registered as singleton");
            
            // Verify configuration is registered
            var storageConfig = serviceProvider.GetService<IOptions<StorageConfiguration>>();
            Assert.IsNotNull(storageConfig, "StorageConfiguration should be registered");
            Assert.AreEqual("SqlServer", storageConfig.Value.Provider, "Provider should be SqlServer");
        }

        [TestMethod]
        public void AddProductBundleStorageFromConfiguration_WithCustomSectionName_UsesCustomSection()
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
            _services.AddProductBundleJsonSerialization(); // Add required serializer
            _services.AddProductBundleStorageFromConfiguration(_configuration, "CustomStorage");

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var storage = serviceProvider.GetService<IProductBundleInstanceStorage>();
            
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
            Assert.IsInstanceOfType(storage, typeof(FileSystemProductBundleInstanceStorage), "Should register FileSystemProductBundleInstanceStorage");
        }

        [TestMethod]
        public void AddProductBundleStorageFromConfiguration_WithMissingConfiguration_ThrowsInvalidOperationException()
        {
            // Arrange
            _configuration = new ConfigurationBuilder().Build(); // Empty configuration

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() =>
                _services.AddProductBundleStorageFromConfiguration(_configuration));

            StringAssert.Contains(exception.Message, "Storage configuration section 'ProductBundleStorage' not found or is empty", 
                "Should have descriptive error message");
        }

        [TestMethod]
        public void AddProductBundleStorageFromConfiguration_WithInvalidConfiguration_ThrowsInvalidOperationException()
        {
            // Arrange - Missing required FileSystem configuration
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "FileSystem"}
                // Missing StorageDirectory
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() =>
                _services.AddProductBundleStorageFromConfiguration(_configuration));

            StringAssert.Contains(exception.Message, "FileSystem configuration is required", 
                "Should have validation error message");
        }

        [TestMethod]
        public void AddProductBundleStorageFromConfiguration_WithUnsupportedProvider_ThrowsInvalidOperationException()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "UnsupportedProvider"}
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() =>
                _services.AddProductBundleStorageFromConfiguration(_configuration));

            StringAssert.Contains(exception.Message, "Unknown storage provider 'UnsupportedProvider'", 
                "Should have descriptive error message");
        }

        [TestMethod]
        public void AddProductBundleStorageFromConfiguration_WithCaseInsensitiveProvider_RegistersCorrectStorage()
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
            _services.AddProductBundleJsonSerialization(); // Add required serializer
            _services.AddProductBundleStorageFromConfiguration(_configuration);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var storage = serviceProvider.GetService<IProductBundleInstanceStorage>();
            
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
            Assert.IsInstanceOfType(storage, typeof(FileSystemProductBundleInstanceStorage), "Should register FileSystemProductBundleInstanceStorage");
        }

        [TestMethod]
        public void AddProductBundleStorageFromConfiguration_RegistersStorageConfiguration_ForDependencyInjection()
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
            _services.AddProductBundleStorageFromConfiguration(_configuration);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var storageConfig = serviceProvider.GetService<IOptions<StorageConfiguration>>();
            
            Assert.IsNotNull(storageConfig, "StorageConfiguration should be registered for dependency injection");
            Assert.AreEqual("FileSystem", storageConfig.Value.Provider, "Provider should be correctly configured");
            Assert.AreEqual(_testStorageDirectory, storageConfig.Value.FileSystem!.StorageDirectory, "Storage directory should be correctly configured");
        }

        [TestMethod]
        public void AddProductBundleStorageFromConfiguration_WithMongoDbMissingConnectionString_ThrowsInvalidOperationException()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "MongoDB"},
                {"ProductBundleStorage:MongoDB:DatabaseName", "testdb"}
                // Missing ConnectionString
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() =>
                _services.AddProductBundleStorageFromConfiguration(_configuration));

            StringAssert.Contains(exception.Message, "MongoDB.ConnectionString is required", 
                "Should have validation error message");
        }

        [TestMethod]
        public void AddProductBundleStorageFromConfiguration_WithSqlServerMissingConnectionString_ThrowsInvalidOperationException()
        {
            // Arrange
            var configData = new Dictionary<string, string?>
            {
                {"ProductBundleStorage:Provider", "SqlServer"}
                // Missing ConnectionString
            };
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(configData)
                .Build();

            // Act & Assert
            var exception = Assert.ThrowsException<InvalidOperationException>(() =>
                _services.AddProductBundleStorageFromConfiguration(_configuration));

            StringAssert.Contains(exception.Message, "SqlServer configuration is required", 
                "Should have validation error message");
        }
    }
}
