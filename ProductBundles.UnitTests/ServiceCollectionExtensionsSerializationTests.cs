using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Serialization;
using System.Text.Json;

namespace ProductBundles.UnitTests
{
    /// <summary>
    /// Unit tests for ServiceCollectionExtensions serialization methods
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsSerializationTests
    {
        private IServiceCollection _services = null!;
        private IServiceProvider _serviceProvider = null!;

        [TestInitialize]
        public void Setup()
        {
            _services = new ServiceCollection();
        }

        [TestMethod]
        public void AddProductBundleInstanceSerialization_WithDefaultOptions_RegistersServices()
        {
            // Act
            var result = _services.AddProductBundleInstanceSerialization();

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            _serviceProvider = _services.BuildServiceProvider();
            
            // Verify JsonSerializerOptions is registered
            var jsonOptions = _serviceProvider.GetService<JsonSerializerOptions>();
            Assert.IsNotNull(jsonOptions, "JsonSerializerOptions should be registered");
            Assert.AreEqual(JsonNamingPolicy.CamelCase, jsonOptions.PropertyNamingPolicy, "Should use camelCase naming policy");
            Assert.IsTrue(jsonOptions.WriteIndented, "Should write indented JSON by default");
            
            // Verify serializer is registered
            var serializer = _serviceProvider.GetService<IProductBundleInstanceSerializer>();
            Assert.IsNotNull(serializer, "IProductBundleInstanceSerializer should be registered");
            Assert.IsInstanceOfType(serializer, typeof(JsonProductBundleInstanceSerializer), "Should register JsonProductBundleInstanceSerializer implementation");
        }

        [TestMethod]
        public void AddProductBundleInstanceSerialization_WithCustomOptions_AppliesConfiguration()
        {
            // Arrange
            bool configureOptionsCalled = false;

            // Act
            _services.AddProductBundleInstanceSerialization(options =>
            {
                configureOptionsCalled = true;
                options.WriteIndented = false;
                options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            });

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            var jsonOptions = _serviceProvider.GetService<JsonSerializerOptions>();
            
            Assert.IsTrue(configureOptionsCalled, "Configure options action should be called");
            Assert.IsNotNull(jsonOptions, "JsonSerializerOptions should be registered");
            Assert.AreEqual(JsonNamingPolicy.SnakeCaseLower, jsonOptions.PropertyNamingPolicy, "Should apply custom naming policy");
            Assert.IsFalse(jsonOptions.WriteIndented, "Should apply custom indentation setting");
        }

        [TestMethod]
        public void AddProductBundleInstanceSerialization_WithNullConfigureOptions_DoesNotThrow()
        {
            // Act & Assert - Should not throw
            var result = _services.AddProductBundleInstanceSerialization(null);
            
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            _serviceProvider = _services.BuildServiceProvider();
            var jsonOptions = _serviceProvider.GetService<JsonSerializerOptions>();
            Assert.IsNotNull(jsonOptions, "JsonSerializerOptions should still be registered with default configuration");
        }

        [TestMethod]
        public void AddProductBundleInstanceSerialization_CalledMultipleTimes_RegistersOnlyOnce()
        {
            // Act
            _services.AddProductBundleInstanceSerialization();
            _services.AddProductBundleInstanceSerialization(options => options.WriteIndented = false);

            // Assert
            _serviceProvider = _services.BuildServiceProvider();
            var serializers = _serviceProvider.GetServices<IProductBundleInstanceSerializer>().ToList();
            
            Assert.AreEqual(1, serializers.Count, "Should register serializer only once even when called multiple times");
        }

        [TestMethod]
        public void AddProductBundleInstanceSerialization_JsonSerializerOptions_IsSingleton()
        {
            // Arrange
            _services.AddProductBundleInstanceSerialization();
            _serviceProvider = _services.BuildServiceProvider();

            // Act
            var options1 = _serviceProvider.GetService<JsonSerializerOptions>();
            var options2 = _serviceProvider.GetService<JsonSerializerOptions>();

            // Assert
            Assert.AreSame(options1, options2, "JsonSerializerOptions should be registered as singleton");
        }

        [TestMethod]
        public void AddProductBundleInstanceSerialization_Serializer_IsSingleton()
        {
            // Arrange
            _services.AddProductBundleInstanceSerialization();
            _serviceProvider = _services.BuildServiceProvider();

            // Act
            var serializer1 = _serviceProvider.GetService<IProductBundleInstanceSerializer>();
            var serializer2 = _serviceProvider.GetService<IProductBundleInstanceSerializer>();

            // Assert
            Assert.AreSame(serializer1, serializer2, "IProductBundleInstanceSerializer should be registered as singleton");
        }
    }
}
