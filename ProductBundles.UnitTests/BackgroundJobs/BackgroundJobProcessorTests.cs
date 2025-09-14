using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.BackgroundJobs;
using ProductBundles.Sdk;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductBundles.UnitTests
{
    /// <summary>
    /// Tests for IBackgroundJobProcessor interface contract and behavior
    /// </summary>
    [TestClass]
    public class BackgroundJobProcessorTests
    {
        private MockBackgroundJobProcessor _processor;

        [TestInitialize]
        public void Setup()
        {
            _processor = new MockBackgroundJobProcessor();
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_WithValidEntityChangeEvent_CallsProcessMethodCorrectly()
        {
            // Arrange
            var entityEvent = new EntityChangeEventArgs(
                entityType: "customer",
                entityId: "123",
                eventType: "updated",
                entityData: new Dictionary<string, object?> { { "name", "John Doe" } },
                metadata: new Dictionary<string, object?> { { "source", "test" } }
            );

            // Act
            await _processor.ProcessEntityEventAsync(entityEvent);

            // Assert
            Assert.IsTrue(_processor.ProcessEntityEventWasCalled);
            Assert.AreEqual("customer", _processor.LastProcessedEvent?.EntityType);
            Assert.AreEqual("123", _processor.LastProcessedEvent?.EntityId);
            Assert.AreEqual("updated", _processor.LastProcessedEvent?.EventType);
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_WithNullEvent_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _processor.ProcessEntityEventAsync(null!));
        }

        [TestMethod]
        public async Task ProcessEntityEventAsync_WithEmptyEntityType_StillProcessesEvent()
        {
            // Arrange
            var entityEvent = new EntityChangeEventArgs(
                entityType: string.Empty,
                entityId: "123",
                eventType: "updated"
            );

            // Act
            await _processor.ProcessEntityEventAsync(entityEvent);

            // Assert
            Assert.IsTrue(_processor.ProcessEntityEventWasCalled);
            Assert.AreEqual(string.Empty, _processor.LastProcessedEvent?.EntityType);
        }

        [TestMethod]
        public async Task ExecuteProductBundleAsync_CallsCorrectly()
        {
            // Arrange
            const string instanceId = "instance-123";
            const string eventName = "test.event";

            // Act
            await _processor.ExecuteProductBundleAsync(instanceId, eventName);

            // Assert
            Assert.IsTrue(_processor.ExecuteProductBundleWasCalled);
            Assert.AreEqual(instanceId, _processor.LastExecutedInstanceId);
            Assert.AreEqual(eventName, _processor.LastExecutedEventName);
        }

        [TestMethod]
        public async Task ExecuteRecurringJobAsync_CallsCorrectly()
        {
            // Arrange
            const string productBundleId = "bundle-123";
            const string jobName = "test-job";
            var parameters = new Dictionary<string, object?> { { "key", "value" } };

            // Act
            await _processor.ExecuteRecurringJobAsync(productBundleId, jobName, parameters);

            // Assert
            Assert.IsTrue(_processor.ExecuteRecurringJobWasCalled);
            Assert.AreEqual(productBundleId, _processor.LastRecurringJobProductBundleId);
            Assert.AreEqual(jobName, _processor.LastRecurringJobName);
            Assert.AreEqual(parameters, _processor.LastRecurringJobParameters);
        }

        [TestMethod]
        public async Task UpgradeProductBundleInstancesAsync_CallsCorrectly()
        {
            // Arrange
            const string productBundleId = "bundle-123";

            // Act
            await _processor.UpgradeProductBundleInstancesAsync(productBundleId);

            // Assert
            Assert.IsTrue(_processor.UpgradeProductBundleInstancesWasCalled);
            Assert.AreEqual(productBundleId, _processor.LastUpgradedProductBundleId);
        }
    }

    /// <summary>
    /// Mock implementation of IBackgroundJobProcessor for testing
    /// </summary>
    public class MockBackgroundJobProcessor : IBackgroundJobProcessor
    {
        public bool ProcessEntityEventWasCalled { get; private set; }
        public bool ExecuteProductBundleWasCalled { get; private set; }
        public bool ExecuteRecurringJobWasCalled { get; private set; }
        public bool UpgradeProductBundleInstancesWasCalled { get; private set; }

        public EntityChangeEventArgs? LastProcessedEvent { get; private set; }
        
        // Additional properties for EntitySourceManagerTests compatibility
        public int ProcessEntityEventAsyncCallCount { get; private set; }
        public EntityChangeEventArgs? LastEntityChangeEvent => LastProcessedEvent;
        public bool ShouldFailProcessing { get; set; }
        public string? LastExecutedInstanceId { get; private set; }
        public string? LastExecutedEventName { get; private set; }
        public string? LastRecurringJobProductBundleId { get; private set; }
        public string? LastRecurringJobName { get; private set; }
        public Dictionary<string, object?>? LastRecurringJobParameters { get; private set; }
        public string? LastUpgradedProductBundleId { get; private set; }

        public Task ExecuteProductBundleAsync(string instanceId, string eventName = "background.execute")
        {
            ExecuteProductBundleWasCalled = true;
            LastExecutedInstanceId = instanceId;
            LastExecutedEventName = eventName;
            return Task.CompletedTask;
        }

        public Task ExecuteRecurringJobAsync(string productBundleId, string recurringJobName, Dictionary<string, object?> parameters)
        {
            ExecuteRecurringJobWasCalled = true;
            LastRecurringJobProductBundleId = productBundleId;
            LastRecurringJobName = recurringJobName;
            LastRecurringJobParameters = parameters;
            return Task.CompletedTask;
        }

        public Task ProcessEntityEventAsync(EntityChangeEventArgs entityChangeEvent)
        {
            if (entityChangeEvent == null)
                throw new ArgumentNullException(nameof(entityChangeEvent));

            ProcessEntityEventWasCalled = true;
            ProcessEntityEventAsyncCallCount++;
            LastProcessedEvent = entityChangeEvent;
            
            if (ShouldFailProcessing)
            {
                throw new InvalidOperationException("Mock processing failure");
            }
            
            return Task.CompletedTask;
        }

        public Task UpgradeProductBundleInstancesAsync(string productBundleId)
        {
            UpgradeProductBundleInstancesWasCalled = true;
            LastUpgradedProductBundleId = productBundleId;
            return Task.CompletedTask;
        }
    }
}
