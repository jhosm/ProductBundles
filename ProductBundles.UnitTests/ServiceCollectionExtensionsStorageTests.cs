using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Serialization;
using ProductBundles.Core.Storage;

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
        public void AddProductBundleInstanceFileSystemStorage_WithValidDirectory_RegistersServices()
        {
            // Arrange
            _services.AddProductBundleInstanceSerialization(); // Required dependency

            // Act
            var result = _services.AddProductBundleInstanceFileSystemStorage((string)_testStorageDirectory);

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
        public void AddProductBundleInstanceFileSystemStorage_WithEmptyDirectory_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddProductBundleInstanceFileSystemStorage(string.Empty));

            Assert.AreEqual("storageDirectory", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Storage directory cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddProductBundleInstanceFileSystemStorage_WithWhitespaceDirectory_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddProductBundleInstanceFileSystemStorage("   "));

            Assert.AreEqual("storageDirectory", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Storage directory cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddProductBundleInstanceFileSystemStorage_WithConfigurationAction_CallsConfiguration()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleInstanceSerialization();
            var configurationCalled = false;
            var expectedDirectory = Path.GetTempPath();

            // Act
            services.AddProductBundleInstanceFileSystemStorage(options =>
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
        public void AddProductBundleInstanceFileSystemStorage_EmptyDirectory_ThrowsException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleInstanceSerialization();

            // Act & Assert
            services.AddProductBundleInstanceFileSystemStorage(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddProductBundleInstanceFileSystemStorage_WhitespaceDirectory_ThrowsException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleInstanceSerialization();

            // Act & Assert
            services.AddProductBundleInstanceFileSystemStorage("   ");
        }

        [TestMethod]
        public void AddProductBundleInstanceFileSystemStorage_WithConfiguration_RegistersServices()
        {
            // Arrange
            _services.AddProductBundleInstanceSerialization(); // Required dependency
            bool configureActionCalled = false;

            // Act
            var result = _services.AddProductBundleInstanceFileSystemStorage(options =>
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
        public void AddProductBundleInstanceFileSystemStorage_WithConfigurationNullAction_ThrowsArgumentNullException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentNullException>(() =>
                _services.AddProductBundleInstanceFileSystemStorage((Action<ProductBundleInstanceStorageOptions>)null!));

            Assert.AreEqual("configure", exception.ParamName, "Should specify correct parameter name");
        }

        [TestMethod]
        public void AddProductBundleInstanceFileSystemStorage_WithConfigurationEmptyDirectory_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddProductBundleInstanceFileSystemStorage(options =>
                {
                    options.StorageDirectory = string.Empty;
                }));

            Assert.AreEqual("configure", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Storage directory must be specified in options", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddProductBundleInstanceFileSystemStorage_WithConfigurationNullDirectory_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddProductBundleInstanceFileSystemStorage(options =>
                {
                    options.StorageDirectory = null!;
                }));

            Assert.AreEqual("configure", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Storage directory must be specified in options", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddProductBundleInstanceFileSystemStorage_InjectsLoggerCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleInstanceSerialization();
            var validDirectory = Path.GetTempPath();

            // Act
            services.AddProductBundleInstanceFileSystemStorage(validDirectory);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var storage = serviceProvider.GetService<IProductBundleInstanceStorage>();

            Assert.IsNotNull(storage);
            // Logger injection is verified by successful service creation
        }

        [TestMethod]
        public void AddProductBundleInstanceFileSystemStorage_ValidDirectory_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleInstanceSerialization();
            var validDirectory = Path.GetTempPath();

            // Act
            services.AddProductBundleInstanceFileSystemStorage(validDirectory);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var options = serviceProvider.GetService<ProductBundleInstanceStorageOptions>();
            var storage = serviceProvider.GetService<IProductBundleInstanceStorage>();

            Assert.IsNotNull(options);
            Assert.AreEqual(validDirectory, options.StorageDirectory);
            Assert.IsNotNull(storage);
        }

        [TestMethod]
        public void AddProductBundleInstanceFileSystemStorage_Storage_IsSingleton()
        {
            // Arrange
            _services.AddProductBundleInstanceSerialization();
            _services.AddProductBundleInstanceFileSystemStorage(_testStorageDirectory);
            _serviceProvider = _services.BuildServiceProvider();

            // Act
            var storage1 = _serviceProvider.GetService<IProductBundleInstanceStorage>();
            var storage2 = _serviceProvider.GetService<IProductBundleInstanceStorage>();

            // Assert
            Assert.AreSame(storage1, storage2, "IProductBundleInstanceStorage should be registered as singleton");
        }

        [TestMethod]
        public void AddProductBundleInstanceFileSystemStorage_MultipleCallsDoNotDuplicate()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleInstanceSerialization();
            var validDirectory = Path.GetTempPath();

            // Act
            services.AddProductBundleInstanceFileSystemStorage(validDirectory);
            services.AddProductBundleInstanceFileSystemStorage(validDirectory);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var storageServices = services.Where(s => s.ServiceType == typeof(IProductBundleInstanceStorage));

            // Should have exactly one registration due to TryAddSingleton
            Assert.AreEqual(1, storageServices.Count());
        }
    }
}
