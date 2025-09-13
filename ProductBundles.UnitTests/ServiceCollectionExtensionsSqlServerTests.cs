using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Serialization;
using ProductBundles.Core.Storage;
using System.Text.Json;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class ServiceCollectionExtensionsSqlServerTests
    {
        private const string TestConnectionString = "Server=localhost;Database=ProductBundlesTest;Integrated Security=false;User Id=test;Password=test;";

        [TestMethod]
        public void AddProductBundleSqlServerStorage_WithValidConnectionString_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddProductBundleSqlServerStorage(TestConnectionString);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            // Check that the service is registered (don't instantiate to avoid DB connection)
            var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IProductBundleInstanceStorage));
            Assert.IsNotNull(serviceDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        }

        [TestMethod]
        public void AddProductBundleSqlServerStorage_WithEmptyConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                services.AddProductBundleSqlServerStorage(""));
        }

        [TestMethod]
        public void AddProductBundleSqlServerStorage_WithNullConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                services.AddProductBundleSqlServerStorage(null!));
        }

        [TestMethod]
        public void AddProductBundleSqlServerStorage_WithWhitespaceConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                services.AddProductBundleSqlServerStorage("   "));
        }

        [TestMethod]
        public void AddProductBundleSqlServerServices_WithValidConnectionString_RegistersAllServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services
                .AddProductBundleJsonSerialization()
                .AddProductBundleSqlServerStorage(TestConnectionString);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            // Check service registrations without instantiating (to avoid DB connection)
            var storageDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IProductBundleInstanceStorage));
            var serializerDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IProductBundleInstanceSerializer));
            var jsonOptionsDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(JsonSerializerOptions));
            
            Assert.IsNotNull(storageDescriptor);
            Assert.IsNotNull(serializerDescriptor);
            Assert.IsNotNull(jsonOptionsDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, storageDescriptor.Lifetime);
            Assert.AreEqual(ServiceLifetime.Singleton, serializerDescriptor.Lifetime);
        }

        [TestMethod]
        public void AddProductBundleSqlServerServices_WithCustomJsonOptions_ConfiguresJsonCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services
                .AddProductBundleJsonSerialization(options =>
                {
                    options.WriteIndented = false;
                    options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                })
                .AddProductBundleSqlServerStorage(TestConnectionString);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var jsonOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
            
            Assert.IsFalse(jsonOptions.WriteIndented);
            Assert.AreEqual(JsonNamingPolicy.SnakeCaseLower, jsonOptions.PropertyNamingPolicy);
        }

        [TestMethod]
        public void AddProductBundleSqlServerServices_WithEmptyConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                services
                    .AddProductBundleJsonSerialization()
                    .AddProductBundleSqlServerStorage(""));
        }

        [TestMethod]
        public void AddProductBundleSqlServerServices_WithNullConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                services
                    .AddProductBundleJsonSerialization()
                    .AddProductBundleSqlServerStorage(null!));
        }

        [TestMethod]
        public void AddProductBundleSqlServerServices_RegisteredAsSingleton_HasCorrectLifetime()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services
                .AddProductBundleJsonSerialization()
                .AddProductBundleSqlServerStorage(TestConnectionString);

            // Act & Assert
            var storageDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IProductBundleInstanceStorage));
            Assert.IsNotNull(storageDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, storageDescriptor.Lifetime);
        }

        [TestMethod]
        public void AddProductBundleSqlServerServices_WithoutLogger_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            // Note: Not adding logging services

            // Act & Assert - Should not throw
            services
                .AddProductBundleJsonSerialization()
                .AddProductBundleSqlServerStorage(TestConnectionString);
            
            var storageDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IProductBundleInstanceStorage));
            Assert.IsNotNull(storageDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, storageDescriptor.Lifetime);
        }
    }
}
