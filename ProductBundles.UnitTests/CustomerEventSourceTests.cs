using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.SamplePlugin;
using ProductBundles.Sdk;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class CustomerEventSourceTests
    {
        private CustomerEventSource _eventSource;
        private ILogger<CustomerEventSource> _logger;
        private List<EntityChangeEventArgs> _capturedEvents;

        [TestInitialize]
        public void Setup()
        {
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<CustomerEventSource>.Instance;
            _eventSource = new CustomerEventSource(_logger);
            _capturedEvents = new List<EntityChangeEventArgs>();
            
            // Subscribe to events
            _eventSource.EntityChanged += OnEntityChanged;
        }

        [TestCleanup]
        public void Cleanup()
        {
            _eventSource?.Dispose();
        }

        private void OnEntityChanged(object? sender, EntityChangeEventArgs e)
        {
            _capturedEvents.Add(e);
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new CustomerEventSource(null!));
        }

        [TestMethod]
        public void Constructor_WithValidLogger_InitializesCorrectly()
        {
            // Arrange & Act
            var source = new CustomerEventSource(_logger);

            // Assert
            Assert.AreEqual("customer-source-default", source.Id);
            Assert.AreEqual("Customer Event Source", source.FriendlyName);
            Assert.AreEqual("customer", source.EntityType);
            Assert.IsFalse(source.IsActive);
            
            // Cleanup
            source.Dispose();
        }

        [TestMethod]
        public async Task InitializeAsync_SetsActiveToTrue()
        {
            // Act
            await _eventSource.InitializeAsync();

            // Assert
            Assert.IsTrue(_eventSource.IsActive);
        }

        [TestMethod]
        public async Task ShutdownAsync_SetsActiveToFalse()
        {
            // Arrange
            await _eventSource.InitializeAsync();
            Assert.IsTrue(_eventSource.IsActive);

            // Act
            await _eventSource.ShutdownAsync();

            // Assert
            Assert.IsFalse(_eventSource.IsActive);
        }

        [TestMethod]
        public async Task RaiseCustomerCreatedEvent_WithValidData_RaisesEvent()
        {
            // Arrange
            await _eventSource.InitializeAsync();
            var customerId = "cust-123";

            // Act
            _eventSource.SimulateCustomerCreated(customerId);

            // Assert
            Assert.AreEqual(1, _capturedEvents.Count);
            var eventArgs = _capturedEvents[0];
            Assert.AreEqual("customer", eventArgs.EntityType);
            Assert.AreEqual(customerId, eventArgs.EntityId);
            Assert.AreEqual("created", eventArgs.EventType);
            Assert.IsNotNull(eventArgs.Timestamp);
            Assert.IsNotNull(eventArgs.EntityData);
        }

        [TestMethod]
        public async Task RaiseCustomerCreatedEvent_WithNullCustomerId_ThrowsArgumentException()
        {
            // Arrange
            await _eventSource.InitializeAsync();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(
                () => _eventSource.SimulateCustomerCreated(null!));
        }

        [TestMethod]
        public async Task RaiseCustomerCreatedEvent_WithEmptyCustomerId_ThrowsArgumentException()
        {
            // Arrange
            await _eventSource.InitializeAsync();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(
                () => _eventSource.SimulateCustomerCreated(""));
        }

        [TestMethod]
        public void RaiseCustomerCreatedEvent_WhenNotActive_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(
                () => _eventSource.SimulateCustomerCreated("cust-123"));
        }

        [TestMethod]
        public async Task RaiseCustomerUpdatedEvent_WithValidData_RaisesEvent()
        {
            // Arrange
            await _eventSource.InitializeAsync();
            var customerId = "cust-456";
            var properties = new Dictionary<string, object?> 
            { 
                { "name", "John Doe" }, 
                { "email", "john@example.com" } 
            };

            // Act
            _eventSource.SimulateCustomerUpdated(customerId, properties);

            // Assert
            Assert.AreEqual(1, _capturedEvents.Count);
            var eventArgs = _capturedEvents[0];
            Assert.AreEqual("customer", eventArgs.EntityType);
            Assert.AreEqual(customerId, eventArgs.EntityId);
            Assert.AreEqual("updated", eventArgs.EventType);
            Assert.IsNotNull(eventArgs.Timestamp);
            Assert.IsNotNull(eventArgs.Metadata);
            Assert.IsTrue(eventArgs.EntityData.ContainsKey("name"));
            Assert.IsTrue(eventArgs.EntityData.ContainsKey("email"));
        }

        [TestMethod]
        public async Task RaiseCustomerUpdatedEvent_WithNullProperties_RaisesEventWithEmptyEntityData()
        {
            // Arrange
            await _eventSource.InitializeAsync();
            var customerId = "cust-789";

            // Act
            _eventSource.SimulateCustomerUpdated(customerId, null);

            // Assert
            Assert.AreEqual(1, _capturedEvents.Count);
            var eventArgs = _capturedEvents[0];
            Assert.IsNotNull(eventArgs.Metadata);
            Assert.AreEqual(0, eventArgs.EntityData.Count);
        }

        [TestMethod]
        public async Task RaiseCustomerUpdatedEvent_WithNullCustomerId_ThrowsArgumentException()
        {
            // Arrange
            await _eventSource.InitializeAsync();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(
                () => _eventSource.SimulateCustomerUpdated(null!, null));
        }

        [TestMethod]
        public void RaiseCustomerUpdatedEvent_WhenNotActive_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(
                () => _eventSource.SimulateCustomerUpdated("cust-123", null));
        }

        [TestMethod]
        public async Task RaiseCustomerDeletedEvent_WithValidData_RaisesEvent()
        {
            // Arrange
            await _eventSource.InitializeAsync();
            var customerId = "cust-delete";

            // Act
            _eventSource.SimulateCustomerDeleted(customerId);

            // Assert
            Assert.AreEqual(1, _capturedEvents.Count);
            var eventArgs = _capturedEvents[0];
            Assert.AreEqual("customer", eventArgs.EntityType);
            Assert.AreEqual(customerId, eventArgs.EntityId);
            Assert.AreEqual("deleted", eventArgs.EventType);
            Assert.IsNotNull(eventArgs.Timestamp);
            Assert.IsNotNull(eventArgs.Metadata);
        }

        [TestMethod]
        public async Task RaiseCustomerDeletedEvent_WithNullCustomerId_ThrowsArgumentException()
        {
            // Arrange
            await _eventSource.InitializeAsync();

            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(
                () => _eventSource.SimulateCustomerDeleted(null!));
        }

        [TestMethod]
        public void RaiseCustomerDeletedEvent_WhenNotActive_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(
                () => _eventSource.SimulateCustomerDeleted("cust-123"));
        }

        [TestMethod]
        public async Task MultipleEvents_RaisesInCorrectOrder()
        {
            // Arrange
            await _eventSource.InitializeAsync();
            var customerId = "cust-multi";

            // Act
            _eventSource.SimulateCustomerCreated(customerId);
            _eventSource.SimulateCustomerUpdated(customerId, 
                new Dictionary<string, object?> { { "status", "active" } });
            _eventSource.SimulateCustomerDeleted(customerId);

            // Assert
            Assert.AreEqual(3, _capturedEvents.Count);
            
            Assert.AreEqual("created", _capturedEvents[0].EventType);
            Assert.AreEqual(customerId, _capturedEvents[0].EntityId);
            
            Assert.AreEqual("updated", _capturedEvents[1].EventType);
            Assert.AreEqual(customerId, _capturedEvents[1].EntityId);
            Assert.IsTrue(_capturedEvents[1].EntityData.ContainsKey("status"));
            
            Assert.AreEqual("deleted", _capturedEvents[2].EventType);
            Assert.AreEqual(customerId, _capturedEvents[2].EntityId);
        }

        [TestMethod]
        public async Task EventTimestamps_AreInChronologicalOrder()
        {
            // Arrange
            await _eventSource.InitializeAsync();
            var customerId = "cust-timing";

            // Act
            _eventSource.SimulateCustomerCreated(customerId);
            await Task.Delay(10); // Small delay to ensure different timestamps
            _eventSource.SimulateCustomerUpdated(customerId, null);
            await Task.Delay(10);
            _eventSource.SimulateCustomerDeleted(customerId);

            // Assert
            Assert.AreEqual(3, _capturedEvents.Count);
            Assert.IsTrue(_capturedEvents[0].Timestamp <= _capturedEvents[1].Timestamp);
            Assert.IsTrue(_capturedEvents[1].Timestamp <= _capturedEvents[2].Timestamp);
        }

        [TestMethod]
        public async Task Dispose_ShutsDownAndStopsEvents()
        {
            // Arrange
            await _eventSource.InitializeAsync();
            Assert.IsTrue(_eventSource.IsActive);

            // Act
            _eventSource.Dispose();

            // Assert
            Assert.IsFalse(_eventSource.IsActive);
            
            // Should not raise events after disposal
            Assert.ThrowsException<InvalidOperationException>(
                () => _eventSource.SimulateCustomerCreated("cust-disposed"));
        }

        [TestMethod]
        public void Dispose_MultipleCallsShouldNotThrow()
        {
            // Act & Assert - Multiple dispose calls should be safe
            _eventSource.Dispose();
            _eventSource.Dispose();
            _eventSource.Dispose();
            
            // No exception should be thrown
            Assert.IsFalse(_eventSource.IsActive);
        }

        [TestMethod]
        public async Task ConcurrentEventRaising_HandlesCorrectly()
        {
            // Arrange
            await _eventSource.InitializeAsync();
            var tasks = new List<Task>();
            
            // Act - Use Task.Run to simulate concurrent calls since the method is synchronous
            for (int i = 0; i < 10; i++)
            {
                var customerId = $"cust-concurrent-{i}";
                tasks.Add(Task.Run(() => _eventSource.SimulateCustomerCreated(customerId)));
            }
            
            await Task.WhenAll(tasks);

            // Assert
            Assert.AreEqual(10, _capturedEvents.Count);
            
            // Verify all events have unique customer IDs
            var uniqueIds = _capturedEvents.Select(e => e.EntityId).Distinct().Count();
            Assert.AreEqual(10, uniqueIds);
            
            // Verify all are creation events
            Assert.IsTrue(_capturedEvents.All(e => e.EventType == "created"));
        }
    }
}
