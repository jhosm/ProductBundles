using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core;
using ProductBundles.Core.BackgroundJobs;
using ProductBundles.Core.EntitySources;
using ProductBundles.Core.Storage;
using ProductBundles.SamplePlugin;
using ProductBundles.Sdk;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class EntitySourceIntegrationTests
    {
        private ProductBundlesLoader _pluginLoader;
        private EntitySourceManager _entitySourceManager;
        private CustomerEventSource _customerEventSource;
        private ProductBundleBackgroundService _backgroundService;
        private IntegrationTestStorage _instanceStorage;
        private ILogger<EntitySourceManager> _managerLogger;
        private ILogger<CustomerEventSource> _sourceLogger;
        private ILogger<ProductBundleBackgroundService> _serviceLogger;

        [TestInitialize]
        public void Setup()
        {
            _managerLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EntitySourceManager>.Instance;
            _sourceLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomerEventSource>.Instance;
            _serviceLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ProductBundleBackgroundService>.Instance;
            
            // Setup plugin loader (using empty plugins directory for this test)
            _pluginLoader = new ProductBundlesLoader("test-plugins", Microsoft.Extensions.Logging.Abstractions.NullLogger<ProductBundlesLoader>.Instance);
            
            // Setup mock storage with sample instances
            _instanceStorage = new IntegrationTestStorage();
            
            // Setup entity source manager
            _entitySourceManager = new EntitySourceManager(_managerLogger);
            
            // Setup customer event source
            _customerEventSource = new CustomerEventSource(_sourceLogger, "integration-test-source");
            
            // Setup background service
            var resilienceManager = new ProductBundles.Core.Resilience.ResilienceManager(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<ProductBundles.Core.Resilience.ResilienceManager>.Instance);
            _backgroundService = new ProductBundleBackgroundService(_pluginLoader, _instanceStorage, resilienceManager, _serviceLogger);
            
            // Register background service as processor in entity source manager
            _entitySourceManager.RegisterProcessor("background-service", _backgroundService);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _entitySourceManager?.Dispose();
            _customerEventSource?.ShutdownAsync().Wait();
        }

        [TestMethod]
        public async Task FullWorkflow_CustomerCreated_ProcessesSuccessfully()
        {
            // Arrange - Register entity source and add sample instances
            await _entitySourceManager.RegisterEntitySourceAsync(_customerEventSource);
            
            // Add sample ProductBundle instances to storage
            var instance1 = new ProductBundleInstance("instance-1", "sample-bundle", "1.0.0");
            instance1.Properties["customerId"] = "customer-123";
            
            var instance2 = new ProductBundleInstance("instance-2", "another-bundle", "2.0.0");
            instance2.Properties["customerId"] = "customer-123";
            
            await _instanceStorage.CreateAsync(instance1);
            await _instanceStorage.CreateAsync(instance2);

            // Track calls to ProcessEntityEventAsync
            var mockProcessor = new MockBackgroundJobProcessor();
            _entitySourceManager.RegisterProcessor("mock-processor", mockProcessor);

            // Act - Simulate customer creation event
            _customerEventSource.SimulateCustomerCreated("customer-123", new Dictionary<string, object?>
            {
                ["name"] = "John Doe",
                ["email"] = "john@example.com"
            });

            // Give async dispatch time to complete
            await Task.Delay(200);

            // Assert
            Assert.AreEqual(1, mockProcessor.ProcessEntityEventAsyncCallCount, "ProcessEntityEventAsync should be called once");
            
            var lastEvent = mockProcessor.LastEntityChangeEvent;
            Assert.IsNotNull(lastEvent, "Entity change event should be captured");
            Assert.AreEqual("customer", lastEvent.EntityType);
            Assert.AreEqual("customer-123", lastEvent.EntityId);
            Assert.AreEqual("created", lastEvent.EventType);
            Assert.IsNotNull(lastEvent.EntityData);
            Assert.AreEqual("John Doe", lastEvent.EntityData["name"]);
            Assert.AreEqual("john@example.com", lastEvent.EntityData["email"]);
        }

        [TestMethod]
        public async Task FullWorkflow_CustomerUpdated_ProcessesSuccessfully()
        {
            // Arrange
            await _entitySourceManager.RegisterEntitySourceAsync(_customerEventSource);
            
            var mockProcessor = new MockBackgroundJobProcessor();
            _entitySourceManager.RegisterProcessor("mock-processor", mockProcessor);

            // Act - Simulate customer update event
            _customerEventSource.SimulateCustomerUpdated("customer-456", new Dictionary<string, object?>
            {
                ["name"] = "Jane Smith",
                ["status"] = "premium"
            });

            await Task.Delay(200);

            // Assert
            Assert.AreEqual(1, mockProcessor.ProcessEntityEventAsyncCallCount);
            
            var lastEvent = mockProcessor.LastEntityChangeEvent;
            Assert.AreEqual("customer", lastEvent.EntityType);
            Assert.AreEqual("customer-456", lastEvent.EntityId);
            Assert.AreEqual("updated", lastEvent.EventType);
            Assert.AreEqual("Jane Smith", lastEvent.EntityData["name"]);
            Assert.AreEqual("premium", lastEvent.EntityData["status"]);
        }

        [TestMethod]
        public async Task FullWorkflow_CustomerDeleted_ProcessesSuccessfully()
        {
            // Arrange
            await _entitySourceManager.RegisterEntitySourceAsync(_customerEventSource);
            
            var mockProcessor = new MockBackgroundJobProcessor();
            _entitySourceManager.RegisterProcessor("mock-processor", mockProcessor);

            // Act - Simulate customer deletion event
            _customerEventSource.SimulateCustomerDeleted("customer-789", null);

            await Task.Delay(200);

            // Assert
            Assert.AreEqual(1, mockProcessor.ProcessEntityEventAsyncCallCount);
            
            var lastEvent = mockProcessor.LastEntityChangeEvent;
            Assert.AreEqual("customer", lastEvent.EntityType);
            Assert.AreEqual("customer-789", lastEvent.EntityId);
            Assert.AreEqual("deleted", lastEvent.EventType);
            Assert.IsTrue(lastEvent.EntityData.Count == 0 || lastEvent.EntityData == null);
        }

        [TestMethod]
        public async Task FullWorkflow_MultipleProcessors_AllReceiveEvents()
        {
            // Arrange
            await _entitySourceManager.RegisterEntitySourceAsync(_customerEventSource);
            
            var processor1 = new MockBackgroundJobProcessor();
            var processor2 = new MockBackgroundJobProcessor();
            
            _entitySourceManager.RegisterProcessor("processor-1", processor1);
            _entitySourceManager.RegisterProcessor("processor-2", processor2);

            // Act
            _customerEventSource.SimulateCustomerCreated("multi-customer", new Dictionary<string, object?>
            {
                ["test"] = "multiple processors"
            });

            await Task.Delay(200);

            // Assert
            Assert.AreEqual(1, processor1.ProcessEntityEventAsyncCallCount);
            Assert.AreEqual(1, processor2.ProcessEntityEventAsyncCallCount);
            
            Assert.AreEqual("multi-customer", processor1.LastEntityChangeEvent.EntityId);
            Assert.AreEqual("multi-customer", processor2.LastEntityChangeEvent.EntityId);
        }

        [TestMethod]
        public async Task FullWorkflow_InactiveSource_ThrowsException()
        {
            // Arrange - Don't register the source (it remains inactive)
            
            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                _customerEventSource.SimulateCustomerCreated("customer-inactive", new Dictionary<string, object?>());
            });
        }

        [TestMethod]
        public async Task FullWorkflow_NoProcessorsRegistered_CompletesWithoutError()
        {
            // Arrange - Register source but no processors
            await _entitySourceManager.RegisterEntitySourceAsync(_customerEventSource);

            // Act - Should not throw
            _customerEventSource.SimulateCustomerCreated("customer-no-processors", new Dictionary<string, object?>());

            await Task.Delay(100);

            // Assert - Just verify no exception was thrown
            Assert.IsTrue(true, "Should complete without error even with no processors");
        }
    }

    // Mock storage for integration testing
    public class IntegrationTestStorage : IProductBundleInstanceStorage
    {
        private readonly Dictionary<string, ProductBundleInstance> _instances = new();

        public Task<bool> CreateAsync(ProductBundleInstance instance)
        {
            if (_instances.ContainsKey(instance.Id))
                return Task.FromResult(false);
            
            _instances[instance.Id] = instance;
            return Task.FromResult(true);
        }

        public Task<ProductBundleInstance?> GetAsync(string instanceId)
        {
            _instances.TryGetValue(instanceId, out var instance);
            return Task.FromResult(instance);
        }

        public Task<bool> UpdateAsync(ProductBundleInstance instance)
        {
            if (!_instances.ContainsKey(instance.Id))
                return Task.FromResult(false);
                
            _instances[instance.Id] = instance;
            return Task.FromResult(true);
        }

        public Task<bool> DeleteAsync(string instanceId)
        {
            return Task.FromResult(_instances.Remove(instanceId));
        }

        public Task<bool> ExistsAsync(string instanceId)
        {
            return Task.FromResult(_instances.ContainsKey(instanceId));
        }

        public Task<IEnumerable<ProductBundleInstance>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<ProductBundleInstance>>(_instances.Values);
        }

        public Task<PaginatedResult<ProductBundleInstance>> GetByProductBundleIdAsync(string productBundleId, PaginationRequest paginationRequest)
        {
            var filtered = _instances.Values.Where(i => i.ProductBundleId == productBundleId);
            var items = filtered.Skip(paginationRequest.Skip).Take(paginationRequest.PageSize);
            var result = new PaginatedResult<ProductBundleInstance>(items, paginationRequest.PageNumber, paginationRequest.PageSize);
            return Task.FromResult(result);
        }

        public Task<int> GetCountAsync()
        {
            return Task.FromResult(_instances.Count);
        }

        public Task<int> GetCountByProductBundleIdAsync(string productBundleId)
        {
            var count = _instances.Values.Count(i => i.ProductBundleId == productBundleId);
            return Task.FromResult(count);
        }
    }
}
