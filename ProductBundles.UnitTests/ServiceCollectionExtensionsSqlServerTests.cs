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
        public void AddProductBundleInstanceSqlServerStorage_WithValidConnectionString_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddProductBundleInstanceSqlServerStorage(TestConnectionString);

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            
            // Check that the service is registered (don't instantiate to avoid DB connection)
            var serviceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IProductBundleInstanceStorage));
            Assert.IsNotNull(serviceDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, serviceDescriptor.Lifetime);
        }

        [TestMethod]
        public void AddProductBundleInstanceSqlServerStorage_WithEmptyConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                services.AddProductBundleInstanceSqlServerStorage(""));
        }

        [TestMethod]
        public void AddProductBundleInstanceSqlServerStorage_WithNullConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                services.AddProductBundleInstanceSqlServerStorage(null!));
        }

        [TestMethod]
        public void AddProductBundleInstanceSqlServerStorage_WithWhitespaceConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                services.AddProductBundleInstanceSqlServerStorage("   "));
        }

        [TestMethod]
        public void AddProductBundleInstanceSqlServerServices_WithValidConnectionString_RegistersAllServices()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddProductBundleInstanceSqlServerServices(TestConnectionString);

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
        public void AddProductBundleInstanceSqlServerServices_WithCustomJsonOptions_ConfiguresJsonCorrectly()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();

            // Act
            services.AddProductBundleInstanceSqlServerServices(TestConnectionString, options =>
            {
                options.WriteIndented = false;
                options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            });

            // Assert
            var serviceProvider = services.BuildServiceProvider();
            var jsonOptions = serviceProvider.GetRequiredService<JsonSerializerOptions>();
            
            Assert.IsFalse(jsonOptions.WriteIndented);
            Assert.AreEqual(JsonNamingPolicy.SnakeCaseLower, jsonOptions.PropertyNamingPolicy);
        }

        [TestMethod]
        public void AddProductBundleInstanceSqlServerServices_WithEmptyConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                services.AddProductBundleInstanceSqlServerServices(""));
        }

        [TestMethod]
        public void AddProductBundleInstanceSqlServerServices_WithNullConnectionString_ThrowsArgumentException()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() =>
                services.AddProductBundleInstanceSqlServerServices(null!));
        }

        [TestMethod]
        public void AddProductBundleInstanceSqlServerServices_RegisteredAsSingleton_HasCorrectLifetime()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddProductBundleInstanceSqlServerServices(TestConnectionString);

            // Act & Assert
            var storageDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IProductBundleInstanceStorage));
            Assert.IsNotNull(storageDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, storageDescriptor.Lifetime);
        }

        [TestMethod]
        public void AddProductBundleInstanceSqlServerServices_WithoutLogger_RegistersServices()
        {
            // Arrange
            var services = new ServiceCollection();
            // Note: Not adding logging services

            // Act & Assert - Should not throw
            services.AddProductBundleInstanceSqlServerServices(TestConnectionString);
            
            var storageDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IProductBundleInstanceStorage));
            Assert.IsNotNull(storageDescriptor);
            Assert.AreEqual(ServiceLifetime.Singleton, storageDescriptor.Lifetime);
        }
    }
}
