using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core;
using ProductBundles.Core.BackgroundJobs;
using ProductBundles.Core.Storage;
using ProductBundles.Sdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProductBundles.UnitTests
{
    /// <summary>
    /// Tests for ProductBundleBackgroundService class
    /// </summary>
    [TestClass]
    public class ProductBundleBackgroundServiceTests
    {
        private MockProductBundlesLoader _mockPluginLoader;
        private MockProductBundleInstanceStorage _mockStorage;
        private ILogger<ProductBundleBackgroundService> _logger;
        private ProductBundleBackgroundService _backgroundService;

        [TestInitialize]
        public void Setup()
        {
            _mockPluginLoader = new MockProductBundlesLoader();
            _mockStorage = new MockProductBundleInstanceStorage();
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ProductBundleBackgroundService>.Instance;
            var resilienceManager = new ProductBundles.Core.Resilience.ResilienceManager(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ProductBundles.Core.Resilience.ResilienceManager>.Instance);
            _backgroundService = new ProductBundleBackgroundService(_mockPluginLoader, _mockStorage, resilienceManager, _logger);
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_WithNullEvent_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _backgroundService.ProcessEntityEventAsync(null!));
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_WithNoLoadedPlugins_LogsWarningAndReturns()
        {
            // Arrange
            var entityEvent = new EntityChangeEventArgs("customer", "123", "updated");
            // No plugins loaded in mock

            // Act
            await _backgroundService.ProcessEntityEventAsync(entityEvent);

            // Assert - should complete without error
            Assert.AreEqual(0, _mockStorage.GetByProductBundleIdAsyncCallCount);
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_WithLoadedPlugin_ProcessesInstances()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            _mockPluginLoader.AddPlugin(plugin);

            var instances = new List<ProductBundleInstance>
            {
                new ProductBundleInstance("instance1", "test-plugin", "1.0.0"),
                new ProductBundleInstance("instance2", "test-plugin", "1.0.0")
            };
            _mockStorage.AddInstancesForPlugin("test-plugin", instances);

            var entityEvent = new EntityChangeEventArgs("customer", "123", "updated",
                new Dictionary<string, object?> { { "name", "John Doe" } },
                new Dictionary<string, object?> { { "source", "test" } });

            // Act
            await _backgroundService.ProcessEntityEventAsync(entityEvent);

            // Assert
            Assert.AreEqual(2, _mockStorage.GetByProductBundleIdAsyncCallCount); // 1 call for instances + 1 call to check for more pages
            Assert.AreEqual(2, plugin.HandleEventCallCount); // Called once per instance (2 instances)
            Assert.AreEqual("entity.updated", plugin.LastEventName);
            
            // Verify enriched instance properties
            var lastEnrichedInstance = plugin.LastHandledInstance;
            Assert.IsNotNull(lastEnrichedInstance);
            Assert.AreEqual("customer", lastEnrichedInstance.Properties["_entityType"]);
            Assert.AreEqual("123", lastEnrichedInstance.Properties["_entityId"]);
            Assert.AreEqual("updated", lastEnrichedInstance.Properties["_eventType"]);
            Assert.AreEqual("John Doe", lastEnrichedInstance.Properties["_entity_name"]);
            Assert.AreEqual("test", lastEnrichedInstance.Properties["_meta_source"]);
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_WithMultiplePlugins_ProcessesAllPlugins()
        {
            // Arrange
            var plugin1 = new MockProductBundle("plugin1", "1.0.0");
            var plugin2 = new MockProductBundle("plugin2", "1.0.0");
            _mockPluginLoader.AddPlugin(plugin1);
            _mockPluginLoader.AddPlugin(plugin2);

            _mockStorage.AddInstancesForPlugin("plugin1", new List<ProductBundleInstance>
            {
                new ProductBundleInstance("instance1", "plugin1", "1.0.0")
            });
            _mockStorage.AddInstancesForPlugin("plugin2", new List<ProductBundleInstance>
            {
                new ProductBundleInstance("instance2", "plugin2", "1.0.0")
            });

            var entityEvent = new EntityChangeEventArgs("order", "456", "created");

            // Act
            await _backgroundService.ProcessEntityEventAsync(entityEvent);

            // Assert
            Assert.AreEqual(4, _mockStorage.GetByProductBundleIdAsyncCallCount); // 2 calls per plugin (instances + check for more pages)
            Assert.AreEqual(1, plugin1.HandleEventCallCount); // Called once per instance (1 instance for plugin1)
            Assert.AreEqual(1, plugin2.HandleEventCallCount); // Called once per instance (1 instance for plugin2)
            Assert.AreEqual("entity.created", plugin1.LastEventName);
            Assert.AreEqual("entity.created", plugin2.LastEventName);
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_WithNoInstancesForPlugin_CompletesSuccessfully()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            _mockPluginLoader.AddPlugin(plugin);
            // No instances added to storage for this plugin

            var entityEvent = new EntityChangeEventArgs("customer", "123", "deleted");

            // Act
            await _backgroundService.ProcessEntityEventAsync(entityEvent);

            // Assert
            Assert.AreEqual(1, _mockStorage.GetByProductBundleIdAsyncCallCount);
            Assert.AreEqual(0, plugin.HandleEventCallCount); // No instances to process
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_WithPaginatedInstances_ProcessesAllPages()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            _mockPluginLoader.AddPlugin(plugin);

            // Create enough instances to trigger pagination (more than 1000)
            var instances = new List<ProductBundleInstance>();
            for (int i = 0; i < 1500; i++)
            {
                instances.Add(new ProductBundleInstance($"instance{i}", "test-plugin", "1.0.0"));
            }
            _mockStorage.AddInstancesForPlugin("test-plugin", instances);

            var entityEvent = new EntityChangeEventArgs("product", "789", "updated");

            // Act
            await _backgroundService.ProcessEntityEventAsync(entityEvent);

            // Assert
            Assert.AreEqual(3, _mockStorage.GetByProductBundleIdAsyncCallCount); // 3 calls: page 1 (1000), page 2 (500), page 3 (0 - triggers break)
            Assert.AreEqual(1500, plugin.HandleEventCallCount); // All instances processed
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_EnrichesInstanceWithCorrectMetadata()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            _mockPluginLoader.AddPlugin(plugin);

            var instance = new ProductBundleInstance("instance1", "test-plugin", "1.0.0");
            instance.Properties["existingProp"] = "existingValue";
            _mockStorage.AddInstancesForPlugin("test-plugin", new List<ProductBundleInstance> { instance });

            var entityData = new Dictionary<string, object?>
            {
                { "customerName", "Jane Smith" },
                { "customerId", 12345 },
                { "isActive", true }
            };
            var metadata = new Dictionary<string, object?>
            {
                { "source", "webhook" },
                { "version", "v2" }
            };
            var timestamp = DateTime.UtcNow;
            var entityEvent = new EntityChangeEventArgs("customer", "123", "updated", entityData, metadata)
            {
                Timestamp = timestamp
            };

            // Act
            await _backgroundService.ProcessEntityEventAsync(entityEvent);

            // Assert
            var enrichedInstance = plugin.LastHandledInstance;
            Assert.IsNotNull(enrichedInstance);

            // Verify original properties preserved
            Assert.AreEqual("existingValue", enrichedInstance.Properties["existingProp"]);

            // Verify entity event metadata
            Assert.AreEqual("customer", enrichedInstance.Properties["_entityType"]);
            Assert.AreEqual("123", enrichedInstance.Properties["_entityId"]);
            Assert.AreEqual("updated", enrichedInstance.Properties["_eventType"]);
            Assert.AreEqual(timestamp, enrichedInstance.Properties["_eventTimestamp"]);

            // Verify entity data with prefix
            Assert.AreEqual("Jane Smith", enrichedInstance.Properties["_entity_customerName"]);
            Assert.AreEqual(12345, enrichedInstance.Properties["_entity_customerId"]);
            Assert.AreEqual(true, enrichedInstance.Properties["_entity_isActive"]);

            // Verify metadata with prefix
            Assert.AreEqual("webhook", enrichedInstance.Properties["_meta_source"]);
            Assert.AreEqual("v2", enrichedInstance.Properties["_meta_version"]);
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_CallsUpdateInstanceInStorage()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            _mockPluginLoader.AddPlugin(plugin);

            var instance = new ProductBundleInstance("instance1", "test-plugin", "1.0.0");
            _mockStorage.AddInstancesForPlugin("test-plugin", new List<ProductBundleInstance> { instance });

            var entityEvent = new EntityChangeEventArgs("customer", "123", "updated");

            // Act
            await _backgroundService.ProcessEntityEventAsync(entityEvent);

            // Assert
            Assert.AreEqual(1, _mockStorage.UpdateAsyncCallCount);
        }

        #region ExecuteRecurringJobAsync Tests

        [TestMethod]
        public async Task ExecuteRecurringJobAsync_WithValidPluginAndJob_ExecutesSuccessfully()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            plugin.AddRecurringJob("health-check", "*/5 * * * *", "Health check job");
            _mockPluginLoader.AddPlugin(plugin);

            var instances = new List<ProductBundleInstance>
            {
                new ProductBundleInstance("instance1", "test-plugin", "1.0.0"),
                new ProductBundleInstance("instance2", "test-plugin", "1.0.0")
            };
            _mockStorage.AddInstancesForPlugin("test-plugin", instances);

            var parameters = new Dictionary<string, object?> { { "timeout", 30 } };

            // Act
            await _backgroundService.ExecuteRecurringJobAsync("test-plugin", "health-check", parameters);

            // Assert
            Assert.AreEqual(2, plugin.HandleEventCallCount); // Called once per instance
            Assert.AreEqual("recurring.health-check", plugin.LastEventName);
            Assert.AreEqual(2, _mockStorage.UpdateAsyncCallCount); // Updated both instances
        }

        [TestMethod]
        public async Task ExecuteRecurringJobAsync_WithPluginNotFound_LogsWarningAndReturns()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>();

            // Act
            await _backgroundService.ExecuteRecurringJobAsync("nonexistent-plugin", "TestJob", new Dictionary<string, object?> { ["eventName"] = "test.event" });

            // Assert
            Assert.AreEqual(0, _mockStorage.GetByProductBundleIdAsyncCallCount);
        }

        [TestMethod]
        public async Task ExecuteRecurringJobAsync_WithJobNotFound_LogsWarningAndReturns()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            plugin.AddRecurringJob("existing-job", "*/5 * * * *", "Existing job");
            _mockPluginLoader.AddPlugin(plugin);

            var parameters = new Dictionary<string, object?>();

            // Act
            await _backgroundService.ExecuteRecurringJobAsync("test-plugin", "non-existent-job", parameters);

            // Assert
            Assert.AreEqual(0, _mockStorage.GetByProductBundleIdAsyncCallCount);
            Assert.AreEqual(0, plugin.HandleEventCallCount);
        }

        [TestMethod]
        public async Task ExecuteRecurringJobAsync_WithCustomEventName_UsesCustomEventName()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            plugin.AddRecurringJob("maintenance", "0 2 * * *", "Daily maintenance");
            _mockPluginLoader.AddPlugin(plugin);

            var instance = new ProductBundleInstance("instance1", "test-plugin", "1.0.0");
            _mockStorage.AddInstancesForPlugin("test-plugin", new List<ProductBundleInstance> { instance });

            var parameters = new Dictionary<string, object?> { { "eventName", "custom.maintenance" } };

            // Act
            await _backgroundService.ExecuteRecurringJobAsync("test-plugin", "maintenance", parameters);

            // Assert
            Assert.AreEqual("custom.maintenance", plugin.LastEventName);
        }

        [TestMethod]
        public async Task ExecuteRecurringJobAsync_WithNoInstances_CompletesSuccessfully()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            plugin.AddRecurringJob("cleanup", "0 4 * * *", "Cleanup job");
            _mockPluginLoader.AddPlugin(plugin);

            // No instances added to storage

            var parameters = new Dictionary<string, object?>();

            // Act
            await _backgroundService.ExecuteRecurringJobAsync("test-plugin", "cleanup", parameters);

            // Assert
            Assert.AreEqual(1, _mockStorage.GetByProductBundleIdAsyncCallCount);
            Assert.AreEqual(0, plugin.HandleEventCallCount);
        }

        [TestMethod]
        public async Task ExecuteRecurringJobAsync_WithPaginatedInstances_ProcessesAllPages()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            plugin.AddRecurringJob("batch-process", "0 */6 * * *", "Batch processing");
            _mockPluginLoader.AddPlugin(plugin);

            // Create enough instances to trigger pagination (more than 1000)
            var instances = new List<ProductBundleInstance>();
            for (int i = 0; i < 2500; i++)
            {
                instances.Add(new ProductBundleInstance($"instance{i}", "test-plugin", "1.0.0"));
            }
            _mockStorage.AddInstancesForPlugin("test-plugin", instances);

            var parameters = new Dictionary<string, object?>();

            // Act
            await _backgroundService.ExecuteRecurringJobAsync("test-plugin", "batch-process", parameters);

            // Assert
            Assert.AreEqual(4, _mockStorage.GetByProductBundleIdAsyncCallCount); // 4 calls: 1000, 1000, 500, 0 (triggers break)
            Assert.AreEqual(2500, plugin.HandleEventCallCount); // All instances processed
            Assert.AreEqual(2500, _mockStorage.UpdateAsyncCallCount); // All instances updated
        }

        [TestMethod]
        public async Task ExecuteRecurringJobAsync_EnrichesInstanceWithJobMetadata()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            plugin.AddRecurringJob("enrichment-test", "0 1 * * *", "Test enrichment");
            _mockPluginLoader.AddPlugin(plugin);

            var instance = new ProductBundleInstance("instance1", "test-plugin", "1.0.0");
            instance.Properties["originalProp"] = "originalValue";
            _mockStorage.AddInstancesForPlugin("test-plugin", new List<ProductBundleInstance> { instance });

            var parameters = new Dictionary<string, object?>
            {
                { "batchSize", 100 },
                { "timeout", 30 }
            };

            // Act
            await _backgroundService.ExecuteRecurringJobAsync("test-plugin", "enrichment-test", parameters);

            // Assert
            var enrichedInstance = plugin.LastHandledInstance;
            Assert.IsNotNull(enrichedInstance);

            // Verify original properties preserved
            Assert.AreEqual("originalValue", enrichedInstance.Properties["originalProp"]);

            // Verify job metadata
            Assert.AreEqual("enrichment-test", enrichedInstance.Properties["_recurringJobName"]);
            Assert.AreEqual("Test enrichment", enrichedInstance.Properties["_recurringJobDescription"]);
            Assert.IsTrue(enrichedInstance.Properties.ContainsKey("_executionTimestamp"));

            // Verify job parameters
            Assert.AreEqual(100, enrichedInstance.Properties["_job_batchSize"]);
            Assert.AreEqual(30, enrichedInstance.Properties["_job_timeout"]);
        }

        #endregion

        #region ExecuteProductBundleAsync Tests

        [TestMethod]
        public async Task ExecuteProductBundleAsync_WithValidPlugin_ExecutesSuccessfully()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            _mockPluginLoader.AddPlugin(plugin);

            var instances = new List<ProductBundleInstance>
            {
                new ProductBundleInstance("instance-1", "test-plugin", "1.0.0"),
                new ProductBundleInstance("instance-2", "test-plugin", "1.0.0")
            };
            _mockStorage.AddInstancesForPlugin("test-plugin", instances);

            var parameters = new Dictionary<string, object?>
            {
                { "eventName", "manual.execution" },
                { "batchSize", 10 }
            };

            // Act
            await _backgroundService.ExecuteProductBundleAsync("instance-1", "manual.execution");

            // Assert
            Assert.AreEqual(1, plugin.HandleEventCallCount, "Plugin should handle event for the specified instance");
            Assert.AreEqual("manual.execution", plugin.LastEventName, "Should use provided event name");
            Assert.AreEqual(1, _mockStorage.UpdateAsyncCallCount, "Should update the specified instance in storage");
        }

        [TestMethod]
        public async Task ExecuteProductBundleAsync_WithPluginNotFound_LogsErrorAndReturns()
        {
            // Arrange
            var parameters = new Dictionary<string, object?>
            {
                { "eventName", "manual.execution" }
            };

            // Act
            await _backgroundService.ExecuteProductBundleAsync("non-existent-instance", "manual.execution");

            // Assert
            // Should not throw and should handle gracefully
            Assert.AreEqual(0, _mockStorage.UpdateAsyncCallCount, "Should not call storage when plugin not found");
        }

        [TestMethod]
        public async Task ExecuteProductBundleAsync_WithParameters_EnrichesInstancesWithParameters()
        {
            // Arrange
            var plugin = new MockProductBundle("test-plugin", "1.0.0");
            _mockPluginLoader.AddPlugin(plugin);
            
            var instance1 = new ProductBundleInstance("instance1", "test-plugin", "1.0.0")
            {
                Properties = new Dictionary<string, object?> { ["existing"] = "value" }
            };
            var instance2 = new ProductBundleInstance("instance2", "test-plugin", "1.0.0");
            
            _mockStorage.AddInstancesForPlugin("test-plugin", new List<ProductBundleInstance> { instance1, instance2 });
            
            var parameters = new Dictionary<string, object?>
            {
                ["eventName"] = "custom.event",
                ["batchSize"] = 10,
                ["timeout"] = 30
            };
            
            // Act
            await _backgroundService.ExecuteProductBundleAsync("instance1", "custom.event");
            
            // Assert
            Assert.AreEqual(1, plugin.HandleEventCallCount, "Should handle event for the specified instance");
            Assert.AreEqual(1, _mockStorage.UpdateAsyncCallCount, "Should update the specified instance");
            
            // Verify the last handled instance
            var lastHandledInstance = plugin.LastHandledInstance;
            Assert.IsNotNull(lastHandledInstance, "Should have processed an instance");
            Assert.AreEqual("custom.event", plugin.LastEventName, "Should have correct event name");
            Assert.AreEqual("instance1", lastHandledInstance.Id, "Should have processed the correct instance");
        }

        #endregion

    }

    /// <summary>
    /// Mock ProductBundlesLoader for testing
    /// </summary>
    public class MockProductBundlesLoader : ProductBundlesLoader
    {
        private readonly List<IAmAProductBundle> _mockPlugins = new List<IAmAProductBundle>();

        public MockProductBundlesLoader() : base("test-plugins")
        {
        }

        public void AddPlugin(IAmAProductBundle plugin)
        {
            _mockPlugins.Add(plugin);
            
            // Use reflection to populate the base class's _loadedPlugins list
            var field = typeof(ProductBundlesLoader).GetField("_loadedPlugins", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                var baseList = (List<IAmAProductBundle>)field.GetValue(this)!;
                if (!baseList.Contains(plugin))
                {
                    baseList.Add(plugin);
                }
            }
        }

        public void ClearPlugins()
        {
            _mockPlugins.Clear();
            
            // Also clear the base class's _loadedPlugins list
            var field = typeof(ProductBundlesLoader).GetField("_loadedPlugins", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                var baseList = (List<IAmAProductBundle>)field.GetValue(this)!;
                baseList.Clear();
            }
        }
    }

    /// <summary>
    /// Mock ProductBundle for testing
    /// </summary>
    public class MockProductBundle : IAmAProductBundle
    {
        private readonly List<RecurringBackgroundJob> _recurringJobs = new List<RecurringBackgroundJob>();

        public string Id { get; }
        public string FriendlyName { get; }
        public string Description { get; }
        public string Version { get; }
        public IReadOnlyList<Property> Properties { get; } = new List<Property>();
        public IReadOnlyList<RecurringBackgroundJob> RecurringBackgroundJobs => _recurringJobs.AsReadOnly();
        public string? Schedule { get; } = null;

        public int HandleEventCallCount { get; private set; }
        public string? LastEventName { get; private set; }
        public ProductBundleInstance? LastHandledInstance { get; private set; }
        
        public int UpgradeCallCount { get; private set; }
        public ProductBundleInstance? LastUpgradedInstance { get; private set; }
        public bool ShouldFailUpgrade { get; set; } = false;

        public MockProductBundle(string id, string version)
        {
            Id = id;
            FriendlyName = $"Mock {id}";
            Description = $"Mock plugin {id}";
            Version = version;
        }

        public void AddRecurringJob(string name, string cronSchedule, string description)
        {
            var parameters = new Dictionary<string, object?>
            {
                { "eventName", $"recurring.{name}" }
            };
            _recurringJobs.Add(new RecurringBackgroundJob(name, cronSchedule, description, parameters));
        }

        public void ClearRecurringJobs()
        {
            _recurringJobs.Clear();
        }

        public void Initialize()
        {
        }

        public ProductBundleInstance HandleEvent(string eventName, ProductBundleInstance bundleInstance)
        {
            HandleEventCallCount++;
            LastEventName = eventName;
            LastHandledInstance = bundleInstance;

            // Return a simple result instance
            var result = new ProductBundleInstance(Guid.NewGuid().ToString(), Id, Version);
            result.Properties["status"] = "processed";
            result.Properties["originalInstanceId"] = bundleInstance.Id;
            result.Properties["eventName"] = eventName;
            return result;
        }

        public ProductBundleInstance UpgradeProductBundleInstance(ProductBundleInstance bundleInstance)
        {
            UpgradeCallCount++;
            
            if (ShouldFailUpgrade)
            {
                throw new InvalidOperationException("Simulated upgrade failure");
            }

            // Create upgraded instance with current plugin version
            var upgradedInstance = new ProductBundleInstance(
                bundleInstance.Id, // Preserve original ID
                Id, // Use current plugin ID
                Version // Use current plugin version
            );

            // Copy all original properties
            foreach (var prop in bundleInstance.Properties)
            {
                upgradedInstance.Properties[prop.Key] = prop.Value;
            }

            // Add upgrade metadata
            upgradedInstance.Properties["_upgraded"] = true;
            upgradedInstance.Properties["_originalVersion"] = bundleInstance.ProductBundleVersion;
            upgradedInstance.Properties["_upgradeTimestamp"] = DateTime.UtcNow;

            LastUpgradedInstance = upgradedInstance;
            return upgradedInstance;
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Mock ProductBundleInstanceStorage for testing
    /// </summary>
    public class MockProductBundleInstanceStorage : IProductBundleInstanceStorage
    {
        private readonly Dictionary<string, List<ProductBundleInstance>> _instancesByPlugin = new Dictionary<string, List<ProductBundleInstance>>();

        public int GetByProductBundleIdAsyncCallCount { get; private set; }
        public int UpdateAsyncCallCount { get; private set; }

        public void AddInstancesForPlugin(string productBundleId, List<ProductBundleInstance> instances)
        {
            _instancesByPlugin[productBundleId] = instances;
        }

        public Task<bool> CreateAsync(ProductBundleInstance instance)
        {
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(string instanceId)
        {
            return Task.FromResult(true);
        }

        public Task<bool> ExistsAsync(string instanceId)
        {
            return Task.FromResult(true);
        }

        public Task<PaginatedResult<ProductBundleInstance>> GetAllAsync(PaginationRequest paginationRequest)
        {
            var allInstances = _instancesByPlugin.Values.SelectMany(x => x).ToList();
            var skip = (paginationRequest.PageNumber - 1) * paginationRequest.PageSize;
            var pageItems = allInstances.Skip(skip).Take(paginationRequest.PageSize).ToList();
            
            return Task.FromResult(new PaginatedResult<ProductBundleInstance>(pageItems, paginationRequest.PageNumber, paginationRequest.PageSize));
        }

        public Task<ProductBundleInstance?> GetAsync(string instanceId)
        {
            var instance = _instancesByPlugin.Values.SelectMany(x => x).FirstOrDefault(x => x.Id == instanceId);
            return Task.FromResult(instance);
        }

        public Task<PaginatedResult<ProductBundleInstance>> GetByProductBundleIdAsync(string productBundleId, PaginationRequest paginationRequest)
        {
            GetByProductBundleIdAsyncCallCount++;
            
            if (_instancesByPlugin.TryGetValue(productBundleId, out var instances))
            {
                var skip = (paginationRequest.PageNumber - 1) * paginationRequest.PageSize;
                var pageItems = instances.Skip(skip).Take(paginationRequest.PageSize).ToList();
                
                return Task.FromResult(new PaginatedResult<ProductBundleInstance>(pageItems, paginationRequest.PageNumber, paginationRequest.PageSize));
            }
            
            return Task.FromResult(new PaginatedResult<ProductBundleInstance>(new List<ProductBundleInstance>(), paginationRequest.PageNumber, paginationRequest.PageSize));
        }

        public Task<int> GetCountAsync()
        {
            return Task.FromResult(_instancesByPlugin.Values.SelectMany(x => x).Count());
        }

        public Task<int> GetCountByProductBundleIdAsync(string productBundleId)
        {
            if (_instancesByPlugin.TryGetValue(productBundleId, out var instances))
            {
                return Task.FromResult(instances.Count);
            }
            return Task.FromResult(0);
        }

        public Task<bool> UpdateAsync(ProductBundleInstance instance)
        {
            UpdateAsyncCallCount++;
            return Task.FromResult(true);
        }
    }
}
