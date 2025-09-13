using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Serialization;
using ProductBundles.Core.Storage;
using MongoDB.Driver;

namespace ProductBundles.UnitTests
{
    /// <summary>
    /// Unit tests for ServiceCollectionExtensions storage methods
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsStorageTests
    {
        private IServiceCollection _services = null!;
        private IServiceProvider _serviceProvider = null!;
        private string _testStorageDirectory = null!;

        [TestInitialize]
        public void Setup()
        {
            _services = new ServiceCollection();
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
        public void AddProductBundleFileSystemStorage_WithValidDirectory_RegistersServices()
        {
            // Arrange
            _services.AddProductBundleJsonSerialization(); // Required dependency

            // Act
            var result = _services.AddProductBundleFileSystemStorage((string)_testStorageDirectory);

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            _serviceProvider = _services.BuildServiceProvider();
            
            // Verify storage options are registered
            var options = _serviceProvider.GetService<ProductBundleInstanceStorageOptions>();
            Assert.IsNotNull(options, "ProductBundleInstanceStorageOptions should be registered");
            Assert.AreEqual(_testStorageDirectory, options.StorageDirectory, "Storage directory should be set correctly");
            
            // Verify storage implementation is registered
            var storage = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
            Assert.IsInstanceOfType(storage, typeof(FileSystemProductBundleInstanceStorage), "Should register FileSystemProductBundleInstanceStorage implementation");
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_WithEmptyDirectory_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddProductBundleFileSystemStorage(string.Empty));

            Assert.AreEqual("storageDirectory", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Storage directory cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_WithWhitespaceDirectory_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddProductBundleFileSystemStorage("   "));

            Assert.AreEqual("storageDirectory", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Storage directory cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_WithConfigurationAction_CallsConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleJsonSerialization();
            var configurationCalled = false;
            var expectedDirectory = Path.GetTempPath();

            // Act
            services.AddProductBundleFileSystemStorage(options =>
            {
                configurationCalled = true;
                options.StorageDirectory = expectedDirectory;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<ProductBundleInstanceStorageOptions>();
            var storage = serviceProvider.GetService<IProductBundleInstanceStorage>();

            Assert.IsTrue(configurationCalled);
            Assert.IsNotNull(options);
            Assert.AreEqual(expectedDirectory, options.StorageDirectory);
            Assert.IsNotNull(storage);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProductBundleFileSystemStorage_EmptyDirectory_ThrowsException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleJsonSerialization();

            // Act & Assert
            services.AddProductBundleFileSystemStorage(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProductBundleFileSystemStorage_WhitespaceDirectory_ThrowsException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleJsonSerialization();

            // Act & Assert
            services.AddProductBundleFileSystemStorage("   ");
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_WithConfiguration_RegistersServices()
        {
            // Arrange
            _services.AddProductBundleJsonSerialization(); // Required dependency
            bool configureActionCalled = false;

            // Act
            var result = _services.AddProductBundleFileSystemStorage(options =>
            {
                configureActionCalled = true;
                options.StorageDirectory = _testStorageDirectory;
                options.MaxConcurrentOperations = 10;
                options.CreateDirectoryIfNotExists = true;
            });

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            Assert.IsTrue(configureActionCalled, "Configure action should be called");
            
            _serviceProvider = _services.BuildServiceProvider();
            
            // Verify storage is registered
            var storage = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_WithConfigurationNullAction_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentNullException>(() =>
                _services.AddProductBundleFileSystemStorage((Action<ProductBundleInstanceStorageOptions>)null!));

            Assert.AreEqual("configure", exception.ParamName, "Should specify correct parameter name");
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_WithConfigurationEmptyDirectory_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddProductBundleFileSystemStorage(options =>
                {
                    options.StorageDirectory = string.Empty;
                }));

            Assert.AreEqual("configure", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Storage directory must be specified in options", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_WithConfigurationNullDirectory_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddProductBundleFileSystemStorage(options =>
                {
                    options.StorageDirectory = null!;
                }));

            Assert.AreEqual("configure", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Storage directory must be specified in options", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_InjectsLoggerCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleJsonSerialization();
            var validDirectory = Path.GetTempPath();

            // Act
            services.AddProductBundleFileSystemStorage(validDirectory);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var storage = serviceProvider.GetService<IProductBundleInstanceStorage>();

            Assert.IsNotNull(storage);
            // Logger injection is verified by successful service creation
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_ValidDirectory_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleJsonSerialization();
            var validDirectory = Path.GetTempPath();

            // Act
            services.AddProductBundleFileSystemStorage(validDirectory);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<ProductBundleInstanceStorageOptions>();
            var storage = serviceProvider.GetService<IProductBundleInstanceStorage>();

            Assert.IsNotNull(options);
            Assert.AreEqual(validDirectory, options.StorageDirectory);
            Assert.IsNotNull(storage);
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_Storage_IsSingleton()
        {
            // Arrange
            _services.AddProductBundleJsonSerialization();
            _services.AddProductBundleFileSystemStorage(_testStorageDirectory);
            _serviceProvider = _services.BuildServiceProvider();

            // Act
            var storage1 = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            var storage2 = _serviceProvider.GetService<IProductBundleInstanceStorage>();

            // Assert
            Assert.AreSame(storage1, storage2, "IProductBundleInstanceStorage should be registered as singleton");
        }

        [TestMethod]
        public void AddProductBundleFileSystemStorage_MultipleCallsDoNotDuplicate()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleJsonSerialization();
            var validDirectory = Path.GetTempPath();

            // Act
            services.AddProductBundleFileSystemStorage(validDirectory);
            services.AddProductBundleFileSystemStorage(validDirectory);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var storageServices = services.Where(s => s.ServiceType == typeof(IProductBundleInstanceStorage));

            // Should have exactly one registration due to TryAddSingleton
            Assert.AreEqual(1, storageServices.Count());
        }

        [TestMethod]
        public void AddProductBundleMongoStorage_WithConnectionString_RegistersServices()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            const string databaseName = "testdb";
            const string collectionName = "testcollection";

            // Act
            _services.AddProductBundleMongoStorage(connectionString, databaseName, collectionName);
            _serviceProvider = _services.BuildServiceProvider();

            // Assert
            var mongoClient = _serviceProvider.GetService<IMongoClient>();
            Assert.IsNotNull(mongoClient, "IMongoClient should be registered");

            var mongoDatabase = _serviceProvider.GetService<IMongoDatabase>();
            Assert.IsNotNull(mongoDatabase, "IMongoDatabase should be registered");
            Assert.AreEqual(databaseName, mongoDatabase.DatabaseNamespace.DatabaseName, "Database name should match");

            var mongoCollection = _serviceProvider.GetService<IMongoCollection<ProductBundles.Sdk.ProductBundleInstance>>();
            Assert.IsNotNull(mongoCollection, "IMongoCollection should be registered");
            Assert.AreEqual(collectionName, mongoCollection.CollectionNamespace.CollectionName, "Collection name should match");

            var storage = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
            Assert.IsInstanceOfType(storage, typeof(MongoProductBundleInstanceStorage), "Storage should be MongoProductBundleInstanceStorage");
        }

        [TestMethod]
        public void AddProductBundleMongoStorage_WithDefaultCollectionName_RegistersServicesWithDefaultName()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            const string databaseName = "testdb";

            // Act
            _services.AddProductBundleMongoStorage(connectionString, databaseName);
            _serviceProvider = _services.BuildServiceProvider();

            // Assert
            var mongoCollection = _serviceProvider.GetService<IMongoCollection<ProductBundles.Sdk.ProductBundleInstance>>();
            Assert.IsNotNull(mongoCollection, "IMongoCollection should be registered");
            Assert.AreEqual("ProductBundleInstances", mongoCollection.CollectionNamespace.CollectionName, "Collection name should default to 'ProductBundleInstances'");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProductBundleMongoStorage_WithNullConnectionString_ThrowsArgumentException()
        {
            // Act & Assert
            _services.AddProductBundleMongoStorage((string)null!, "testdb");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProductBundleMongoStorage_WithEmptyConnectionString_ThrowsArgumentException()
        {
            // Act & Assert
            _services.AddProductBundleMongoStorage(string.Empty, "testdb");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProductBundleMongoStorage_WithWhitespaceConnectionString_ThrowsArgumentException()
        {
            // Act & Assert
            _services.AddProductBundleMongoStorage("   ", "testdb");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProductBundleMongoStorage_WithNullDatabaseName_ThrowsArgumentException()
        {
            // Act & Assert
            _services.AddProductBundleMongoStorage("mongodb://localhost:27017", (string)null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProductBundleMongoStorage_WithEmptyDatabaseName_ThrowsArgumentException()
        {
            // Act & Assert
            _services.AddProductBundleMongoStorage("mongodb://localhost:27017", string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProductBundleMongoStorage_WithWhitespaceDatabaseName_ThrowsArgumentException()
        {
            // Act & Assert
            _services.AddProductBundleMongoStorage("mongodb://localhost:27017", "   ");
        }

        [TestMethod]
        public void AddProductBundleMongoStorage_RegisteredAsSingleton_ReturnsSameInstance()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            const string databaseName = "testdb";

            _services.AddProductBundleMongoStorage(connectionString, databaseName);
            _serviceProvider = _services.BuildServiceProvider();

            // Act
            var storage1 = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            var storage2 = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            var mongoClient1 = _serviceProvider.GetService<IMongoClient>();
            var mongoClient2 = _serviceProvider.GetService<IMongoClient>();

            // Assert
            Assert.AreSame(storage1, storage2, "Storage instances should be the same (singleton)");
            Assert.AreSame(mongoClient1, mongoClient2, "MongoClient instances should be the same (singleton)");
        }
    }
}
