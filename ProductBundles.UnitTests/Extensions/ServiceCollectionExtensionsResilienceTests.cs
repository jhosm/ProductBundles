using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Extensions;
using ProductBundles.Core.Resilience;

namespace ProductBundles.UnitTests.Extensions
{
    /// <summary>
    /// Unit tests for ServiceCollectionExtensions resilience methods
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsResilienceTests
    {
        private IServiceCollection _services = null!;

        [TestInitialize]
        public void Setup()
        {
            _services = new ServiceCollection();
            _services.AddLogging();
        }

        [TestMethod]
        public void AddPluginResilience_WithDefaultTimeout_RegistersResilienceManager()
        {
            // Act
            var result = _services.AddPluginResilience();

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var resilienceManager = serviceProvider.GetService<ResilienceManager>();
            
            Assert.IsNotNull(resilienceManager, "ResilienceManager should be registered");
        }

        [TestMethod]
        public void AddPluginResilience_WithCustomTimeout_RegistersResilienceManagerWithCustomTimeout()
        {
            // Arrange
            var customTimeout = TimeSpan.FromSeconds(60);

            // Act
            var result = _services.AddPluginResilience(customTimeout);

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var resilienceManager = serviceProvider.GetService<ResilienceManager>();
            
            Assert.IsNotNull(resilienceManager, "ResilienceManager should be registered");
        }

        [TestMethod]
        public void AddPluginResilience_WithNullTimeout_RegistersResilienceManagerWithDefaultTimeout()
        {
            // Act
            var result = _services.AddPluginResilience(null);

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var resilienceManager = serviceProvider.GetService<ResilienceManager>();
            
            Assert.IsNotNull(resilienceManager, "ResilienceManager should be registered");
        }

        [TestMethod]
        public void AddPluginResilience_RegisteredAsSingleton_ReturnsSameInstance()
        {
            // Arrange
            _services.AddPluginResilience();
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var manager1 = serviceProvider.GetService<ResilienceManager>();
            var manager2 = serviceProvider.GetService<ResilienceManager>();

            // Assert
            Assert.AreSame(manager1, manager2, "ResilienceManager should be registered as singleton");
        }

        [TestMethod]
        public void AddPluginResilience_CalledMultipleTimes_RegistersOnlyOnce()
        {
            // Act
            _services.AddPluginResilience(TimeSpan.FromSeconds(30));
            _services.AddPluginResilience(TimeSpan.FromSeconds(60));

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var managers = serviceProvider.GetServices<ResilienceManager>().ToList();
            
            Assert.AreEqual(1, managers.Count, "Should register ResilienceManager only once due to TryAddSingleton");
        }

        [TestMethod]
        public void AddPluginResilience_RequiresLogger_InjectsLoggerCorrectly()
        {
            // Arrange
            _services.AddPluginResilience();

            // Act
            var serviceProvider = _services.BuildServiceProvider();
            var resilienceManager = serviceProvider.GetService<ResilienceManager>();

            // Assert
            Assert.IsNotNull(resilienceManager, "ResilienceManager should be created successfully with logger injection");
        }

        [TestMethod]
        public void AddPluginResilience_WithoutLogging_ThrowsException()
        {
            // Arrange
            var servicesWithoutLogging = new ServiceCollection();
            servicesWithoutLogging.AddPluginResilience();

            // Act & Assert
            var serviceProvider = servicesWithoutLogging.BuildServiceProvider();
            
            Assert.ThrowsException<InvalidOperationException>(() =>
                serviceProvider.GetRequiredService<ResilienceManager>(),
                "Should throw when logger is not available");
        }

        [TestMethod]
        public void AddPluginResilience_WithMinimumValidTimeout_RegistersResilienceManager()
        {
            // Arrange
            var minimumTimeout = TimeSpan.FromMilliseconds(10); // Minimum valid timeout

            // Act
            var result = _services.AddPluginResilience(minimumTimeout);

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var resilienceManager = serviceProvider.GetService<ResilienceManager>();
            
            Assert.IsNotNull(resilienceManager, "ResilienceManager should be registered with minimum valid timeout");
        }

        [TestMethod]
        public void AddPluginResilience_WithMaximumValidTimeout_RegistersResilienceManager()
        {
            // Arrange
            var maximumTimeout = TimeSpan.FromHours(1); // Maximum valid timeout

            // Act
            var result = _services.AddPluginResilience(maximumTimeout);

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var resilienceManager = serviceProvider.GetService<ResilienceManager>();
            
            Assert.IsNotNull(resilienceManager, "ResilienceManager should be registered with maximum valid timeout");
        }

        [TestMethod]
        public void AddPluginResilience_WithInvalidTimeout_ThrowsValidationException()
        {
            // Arrange
            var invalidTimeout = TimeSpan.Zero; // Invalid timeout (too small)
            _services.AddPluginResilience(invalidTimeout);

            // Act & Assert
            var serviceProvider = _services.BuildServiceProvider();
            
            Assert.ThrowsException<System.ComponentModel.DataAnnotations.ValidationException>(() =>
                serviceProvider.GetRequiredService<ResilienceManager>(),
                "Should throw ValidationException for invalid timeout values");
        }
    }
}
