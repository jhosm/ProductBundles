using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Serialization;
using ProductBundles.Core.Storage;
using ProductBundles.Sdk;
using System.Text.Json;

namespace ProductBundles.UnitTests.Extensions
{
    /// <summary>
    /// Unit tests for ServiceCollectionExtensions custom implementation methods
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsCustomTests
    {
        private IServiceCollection _services = null!;

        [TestInitialize]
        public void Setup()
        {
            _services = new ServiceCollection();
            _services.AddLogging();
        }

        #region Custom Serialization Tests

        [TestMethod]
        public void AddProductBundleCustomSerialization_WithValidSerializer_RegistersCustomSerializer()
        {
            // Act
            var result = _services.AddProductBundleCustomSerialization<TestCustomSerializer>();

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var serializer = serviceProvider.GetService<IProductBundleInstanceSerializer>();
            
            Assert.IsNotNull(serializer, "IProductBundleInstanceSerializer should be registered");
            Assert.IsInstanceOfType(serializer, typeof(TestCustomSerializer), "Should register custom serializer implementation");
        }

        [TestMethod]
        public void AddProductBundleCustomSerialization_CalledMultipleTimes_RegistersOnlyOnce()
        {
            // Act
            _services.AddProductBundleCustomSerialization<TestCustomSerializer>();
            _services.AddProductBundleCustomSerialization<AnotherTestCustomSerializer>();

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var serializers = serviceProvider.GetServices<IProductBundleInstanceSerializer>().ToList();
            
            Assert.AreEqual(1, serializers.Count, "Should register serializer only once due to TryAddSingleton");
            Assert.IsInstanceOfType(serializers[0], typeof(TestCustomSerializer), "Should keep first registered serializer");
        }

        [TestMethod]
        public void AddProductBundleCustomSerialization_RegisteredAsSingleton_ReturnsSameInstance()
        {
            // Arrange
            _services.AddProductBundleCustomSerialization<TestCustomSerializer>();
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var serializer1 = serviceProvider.GetService<IProductBundleInstanceSerializer>();
            var serializer2 = serviceProvider.GetService<IProductBundleInstanceSerializer>();

            // Assert
            Assert.AreSame(serializer1, serializer2, "Custom serializer should be registered as singleton");
        }

        #endregion

        #region Custom Storage Tests

        [TestMethod]
        public void AddProductBundleCustomStorage_WithValidStorage_RegistersCustomStorage()
        {
            // Act
            var result = _services.AddProductBundleCustomStorage<TestCustomStorage>();

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var storage = serviceProvider.GetService<IProductBundleInstanceStorage>();
            
            Assert.IsNotNull(storage, "IProductBundleInstanceStorage should be registered");
            Assert.IsInstanceOfType(storage, typeof(TestCustomStorage), "Should register custom storage implementation");
        }

        [TestMethod]
        public void AddProductBundleCustomStorage_CalledMultipleTimes_RegistersOnlyOnce()
        {
            // Act
            _services.AddProductBundleCustomStorage<TestCustomStorage>();
            _services.AddProductBundleCustomStorage<AnotherTestCustomStorage>();

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var storages = serviceProvider.GetServices<IProductBundleInstanceStorage>().ToList();
            
            Assert.AreEqual(1, storages.Count, "Should register storage only once due to TryAddSingleton");
            Assert.IsInstanceOfType(storages[0], typeof(TestCustomStorage), "Should keep first registered storage");
        }

        [TestMethod]
        public void AddProductBundleCustomStorage_RegisteredAsSingleton_ReturnsSameInstance()
        {
            // Arrange
            _services.AddProductBundleCustomStorage<TestCustomStorage>();
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var storage1 = serviceProvider.GetService<IProductBundleInstanceStorage>();
            var storage2 = serviceProvider.GetService<IProductBundleInstanceStorage>();

            // Assert
            Assert.AreSame(storage1, storage2, "Custom storage should be registered as singleton");
        }

        #endregion

        #region Test Helper Classes

        private class TestCustomSerializer : IProductBundleInstanceSerializer
        {
            public string FormatName => "TestCustom";
            public string FileExtension => ".test";

            public string Serialize(ProductBundleInstance instance)
            {
                return "test-serialized";
            }

            public ProductBundleInstance Deserialize(string serializedData)
            {
                return new ProductBundleInstance
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductBundleId = "test-bundle",
                    ProductBundleVersion = "1.0",
                    Properties = new Dictionary<string, object?>()
                };
            }

            public bool TryDeserialize(string serializedData, out ProductBundleInstance? instance)
            {
                try
                {
                    instance = Deserialize(serializedData);
                    return true;
                }
                catch
                {
                    instance = null;
                    return false;
                }
            }
        }

        private class AnotherTestCustomSerializer : IProductBundleInstanceSerializer
        {
            public string FormatName => "AnotherTestCustom";
            public string FileExtension => ".another";

            public string Serialize(ProductBundleInstance instance)
            {
                return "another-test-serialized";
            }

            public ProductBundleInstance Deserialize(string serializedData)
            {
                return new ProductBundleInstance
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductBundleId = "another-test-bundle",
                    ProductBundleVersion = "1.0",
                    Properties = new Dictionary<string, object?>()
                };
            }

            public bool TryDeserialize(string serializedData, out ProductBundleInstance? instance)
            {
                try
                {
                    instance = Deserialize(serializedData);
                    return true;
                }
                catch
                {
                    instance = null;
                    return false;
                }
            }
        }

        private class TestCustomStorage : IProductBundleInstanceStorage
        {
            public Task<bool> CreateAsync(ProductBundleInstance instance)
            {
                return Task.FromResult(true);
            }

            public Task<ProductBundleInstance?> GetAsync(string id)
            {
                return Task.FromResult<ProductBundleInstance?>(null);
            }

            public Task<PaginatedResult<ProductBundleInstance>> GetByProductBundleIdAsync(string productBundleId, PaginationRequest paginationRequest)
            {
                return Task.FromResult(new PaginatedResult<ProductBundleInstance>(
                    new List<ProductBundleInstance>(),
                    paginationRequest.PageNumber,
                    paginationRequest.PageSize));
            }

            public Task<bool> UpdateAsync(ProductBundleInstance instance)
            {
                return Task.FromResult(true);
            }

            public Task<bool> DeleteAsync(string id)
            {
                return Task.FromResult(true);
            }

            public Task<bool> ExistsAsync(string id)
            {
                return Task.FromResult(false);
            }

            public Task<int> GetCountAsync()
            {
                return Task.FromResult(0);
            }

            public Task<int> GetCountByProductBundleIdAsync(string productBundleId)
            {
                return Task.FromResult(0);
            }
        }

        private class AnotherTestCustomStorage : IProductBundleInstanceStorage
        {
            public Task<bool> CreateAsync(ProductBundleInstance instance)
            {
                return Task.FromResult(true);
            }

            public Task<ProductBundleInstance?> GetAsync(string id)
            {
                return Task.FromResult<ProductBundleInstance?>(null);
            }

            public Task<PaginatedResult<ProductBundleInstance>> GetByProductBundleIdAsync(string productBundleId, PaginationRequest paginationRequest)
            {
                return Task.FromResult(new PaginatedResult<ProductBundleInstance>(
                    new List<ProductBundleInstance>(),
                    paginationRequest.PageNumber,
                    paginationRequest.PageSize));
            }

            public Task<bool> UpdateAsync(ProductBundleInstance instance)
            {
                return Task.FromResult(true);
            }

            public Task<bool> DeleteAsync(string id)
            {
                return Task.FromResult(true);
            }

            public Task<bool> ExistsAsync(string id)
            {
                return Task.FromResult(false);
            }

            public Task<int> GetCountAsync()
            {
                return Task.FromResult(0);
            }

            public Task<int> GetCountByProductBundleIdAsync(string productBundleId)
            {
                return Task.FromResult(0);
            }
        }

        #endregion
    }
}
