using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.BackgroundJobs;
using ProductBundles.Core.EntitySources;
using ProductBundles.Sdk;
using System.Collections.Concurrent;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class EntitySourceManagerTests
    {
        private EntitySourceManager _manager;
        private ILogger<EntitySourceManager> _logger;

        [TestInitialize]
        public void Setup()
        {
            _logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<EntitySourceManager>.Instance;
            _manager = new EntitySourceManager(_logger);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _manager?.Dispose();
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => new EntitySourceManager(null!));
        }

        [TestMethod]
        public void Constructor_WithValidLogger_InitializesSuccessfully()
        {
            // Arrange & Act
            var manager = new EntitySourceManager(_logger);

            // Assert
            Assert.IsNotNull(manager);
            Assert.AreEqual(0, manager.EntitySources.Count);
            Assert.AreEqual(0, manager.Processors.Count);
            
            // Cleanup
            manager.Dispose();
        }

        [TestMethod]
        public async Task RegisterEntitySourceAsync_WithValidSource_RegistersSuccessfully()
        {
            // Arrange
            var mockSource = new MockEntitySource("test-source", "customer");

            // Act
            await _manager.RegisterEntitySourceAsync(mockSource);

            // Assert
            Assert.AreEqual(1, _manager.EntitySources.Count);
            Assert.IsTrue(_manager.EntitySources.ContainsKey("test-source"));
            Assert.AreEqual(mockSource, _manager.EntitySources["test-source"]);
            Assert.IsTrue(mockSource.IsInitializeCalled);
        }

        [TestMethod]
        public async Task RegisterEntitySourceAsync_WithNullSource_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _manager.RegisterEntitySourceAsync(null!));
        }

        [TestMethod]
        public async Task RegisterEntitySourceAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Arrange
            var mockSource = new MockEntitySource("", "customer");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _manager.RegisterEntitySourceAsync(mockSource));
        }

        [TestMethod]
        public async Task RegisterEntitySourceAsync_WithDuplicateId_LogsWarningAndReturns()
        {
            // Arrange
            var source1 = new MockEntitySource("duplicate", "customer");
            var source2 = new MockEntitySource("duplicate", "order");

            // Act
            await _manager.RegisterEntitySourceAsync(source1);
            await _manager.RegisterEntitySourceAsync(source2); // Should not throw, just log warning

            // Assert
            Assert.AreEqual(1, _manager.EntitySources.Count);
            Assert.AreEqual(source1, _manager.EntitySources["duplicate"]); // First one wins
        }

        [TestMethod]
        public async Task RegisterEntitySourceAsync_WhenInitializeFails_RemovesSourceAndRethrows()
        {
            // Arrange
            var mockSource = new MockEntitySource("failing-source", "customer");
            mockSource.ShouldFailInitialize = true;

            // Act & Assert
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => _manager.RegisterEntitySourceAsync(mockSource));

            // Assert source was removed from registry
            Assert.AreEqual(0, _manager.EntitySources.Count);
            Assert.IsFalse(mockSource.IsEventSubscribed);
        }

        [TestMethod]
        public async Task UnregisterEntitySourceAsync_WithValidId_UnregistersSuccessfully()
        {
            // Arrange
            var mockSource = new MockEntitySource("test-source", "customer");
            await _manager.RegisterEntitySourceAsync(mockSource);

            // Act
            await _manager.UnregisterEntitySourceAsync("test-source");

            // Assert
            Assert.AreEqual(0, _manager.EntitySources.Count);
            Assert.IsTrue(mockSource.IsShutdownCalled);
            Assert.IsFalse(mockSource.IsEventSubscribed);
        }

        [TestMethod]
        public async Task UnregisterEntitySourceAsync_WithNonExistentId_LogsWarningAndReturns()
        {
            // Act & Assert - Should not throw
            await _manager.UnregisterEntitySourceAsync("non-existent");
            
            // Arrange
            Assert.AreEqual(0, _manager.EntitySources.Count);
        }

        [TestMethod]
        public async Task UnregisterEntitySourceAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _manager.UnregisterEntitySourceAsync(""));
        }

        [TestMethod]
        public void RegisterProcessor_WithValidProcessor_RegistersSuccessfully()
        {
            // Arrange
            var mockProcessor = new MockBackgroundJobProcessor();

            // Act
            _manager.RegisterProcessor("test-processor", mockProcessor);

            // Assert
            Assert.AreEqual(1, _manager.Processors.Count);
            Assert.IsTrue(_manager.Processors.ContainsKey("test-processor"));
            Assert.AreEqual(mockProcessor, _manager.Processors["test-processor"]);
        }

        [TestMethod]
        public void RegisterProcessor_WithNullProcessor_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => 
                _manager.RegisterProcessor("test", null!));
        }

        [TestMethod]
        public void RegisterProcessor_WithEmptyId_ThrowsArgumentException()
        {
            // Arrange
            var mockProcessor = new MockBackgroundJobProcessor();

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => 
                _manager.RegisterProcessor("", mockProcessor));
        }

        [TestMethod]
        public void RegisterProcessor_WithDuplicateId_LogsWarningAndKeepsFirst()
        {
            // Arrange
            var processor1 = new MockBackgroundJobProcessor();
            var processor2 = new MockBackgroundJobProcessor();

            // Act
            _manager.RegisterProcessor("duplicate", processor1);
            _manager.RegisterProcessor("duplicate", processor2); // Should log warning

            // Assert
            Assert.AreEqual(1, _manager.Processors.Count);
            Assert.AreEqual(processor1, _manager.Processors["duplicate"]); // First one wins
        }

        [TestMethod]
        public void UnregisterProcessor_WithValidId_UnregistersSuccessfully()
        {
            // Arrange
            var mockProcessor = new MockBackgroundJobProcessor();
            _manager.RegisterProcessor("test-processor", mockProcessor);

            // Act
            _manager.UnregisterProcessor("test-processor");

            // Assert
            Assert.AreEqual(0, _manager.Processors.Count);
        }

        [TestMethod]
        public void UnregisterProcessor_WithNonExistentId_LogsWarningAndReturns()
        {
            // Act & Assert - Should not throw
            _manager.UnregisterProcessor("non-existent");
            
            Assert.AreEqual(0, _manager.Processors.Count);
        }

        [TestMethod]
        public async Task EntityEventDispatch_WithRegisteredProcessor_DispatchesEventSuccessfully()
        {
            // Arrange
            var mockSource = new MockEntitySource("test-source", "customer");
            var mockProcessor = new MockBackgroundJobProcessor();
            
            await _manager.RegisterEntitySourceAsync(mockSource);
            _manager.RegisterProcessor("test-processor", mockProcessor);

            var eventArgs = new EntityChangeEventArgs("customer", "123", "created");

            // Act
            mockSource.TriggerEntityChanged(eventArgs);

            // Give async dispatch some time to complete
            await Task.Delay(100);

            // Assert
            Assert.AreEqual(1, mockProcessor.ProcessEntityEventAsyncCallCount);
            Assert.AreEqual(eventArgs, mockProcessor.LastEntityChangeEvent);
        }

        [TestMethod]
        public async Task EntityEventDispatch_WithMultipleProcessors_DispatchesToAllProcessors()
        {
            // Arrange
            var mockSource = new MockEntitySource("test-source", "customer");
            var processor1 = new MockBackgroundJobProcessor();
            var processor2 = new MockBackgroundJobProcessor();
            
            await _manager.RegisterEntitySourceAsync(mockSource);
            _manager.RegisterProcessor("processor1", processor1);
            _manager.RegisterProcessor("processor2", processor2);

            var eventArgs = new EntityChangeEventArgs("customer", "123", "updated");

            // Act
            mockSource.TriggerEntityChanged(eventArgs);

            // Give async dispatch some time to complete
            await Task.Delay(100);

            // Assert
            Assert.AreEqual(1, processor1.ProcessEntityEventAsyncCallCount);
            Assert.AreEqual(1, processor2.ProcessEntityEventAsyncCallCount);
            Assert.AreEqual(eventArgs, processor1.LastEntityChangeEvent);
            Assert.AreEqual(eventArgs, processor2.LastEntityChangeEvent);
        }

        [TestMethod]
        public async Task EntityEventDispatch_WhenProcessorFails_ContinuesWithOtherProcessors()
        {
            // Arrange
            var mockSource = new MockEntitySource("test-source", "customer");
            var failingProcessor = new MockBackgroundJobProcessor { ShouldFailProcessing = true };
            var successProcessor = new MockBackgroundJobProcessor();
            
            await _manager.RegisterEntitySourceAsync(mockSource);
            _manager.RegisterProcessor("failing-processor", failingProcessor);
            _manager.RegisterProcessor("success-processor", successProcessor);

            var eventArgs = new EntityChangeEventArgs("customer", "123", "deleted");

            // Act
            mockSource.TriggerEntityChanged(eventArgs);

            // Give async dispatch some time to complete
            await Task.Delay(100);

            // Assert
            Assert.AreEqual(1, failingProcessor.ProcessEntityEventAsyncCallCount);
            Assert.AreEqual(1, successProcessor.ProcessEntityEventAsyncCallCount);
            Assert.AreEqual(eventArgs, successProcessor.LastEntityChangeEvent);
        }

        [TestMethod]
        public async Task Dispose_WithRegisteredSources_ShutsDownAllSources()
        {
            // Arrange
            var source1 = new MockEntitySource("source1", "customer");
            var source2 = new MockEntitySource("source2", "order");
            
            await _manager.RegisterEntitySourceAsync(source1);
            await _manager.RegisterEntitySourceAsync(source2);

            // Act
            _manager.Dispose();

            // Assert
            Assert.IsTrue(source1.IsShutdownCalled);
            Assert.IsTrue(source2.IsShutdownCalled);
            Assert.IsFalse(source1.IsEventSubscribed);
            Assert.IsFalse(source2.IsEventSubscribed);
        }

        [TestMethod]
        public async Task DisposedManager_ThrowsObjectDisposedExceptionOnOperations()
        {
            // Arrange
            var mockSource = new MockEntitySource("test-source", "customer");
            var mockProcessor = new MockBackgroundJobProcessor();
            
            _manager.Dispose();

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(
                () => _manager.RegisterEntitySourceAsync(mockSource));
            
            Assert.ThrowsException<ObjectDisposedException>(
                () => _manager.RegisterProcessor("test", mockProcessor));

            Assert.ThrowsException<ObjectDisposedException>(
                () => _manager.UnregisterProcessor("test"));

            await Assert.ThrowsExceptionAsync<ObjectDisposedException>(
                () => _manager.UnregisterEntitySourceAsync("test"));
        }
    }

    // Mock entity source for testing
    public class MockEntitySource : IEntitySource
    {
        public string Id { get; }
        public string FriendlyName { get; }
        public string EntityType { get; }
        public bool IsActive { get; private set; }
        
        public bool IsInitializeCalled { get; private set; }
        public bool IsShutdownCalled { get; private set; }
        public bool ShouldFailInitialize { get; set; }
        public bool IsEventSubscribed { get; private set; }

        public event EventHandler<EntityChangeEventArgs>? EntityChanged
        {
            add 
            { 
                _entityChanged += value;
                IsEventSubscribed = true;
            }
            remove 
            { 
                _entityChanged -= value;
                IsEventSubscribed = false;
            }
        }
        
        private event EventHandler<EntityChangeEventArgs>? _entityChanged;

        public MockEntitySource(string id, string entityType)
        {
            Id = id;
            EntityType = entityType;
            FriendlyName = $"Mock {entityType} Source";
        }

        public Task InitializeAsync()
        {
            IsInitializeCalled = true;
            if (ShouldFailInitialize)
            {
                throw new InvalidOperationException("Mock initialization failure");
            }
            IsActive = true;
            return Task.CompletedTask;
        }

        public Task ShutdownAsync()
        {
            IsShutdownCalled = true;
            IsActive = false;
            return Task.CompletedTask;
        }

        public void TriggerEntityChanged(EntityChangeEventArgs args)
        {
            _entityChanged?.Invoke(this, args);
        }
    }
}
