using Microsoft.Extensions.Logging;
using ProductBundles.Sdk;

namespace ProductBundles.SamplePlugin
{
    /// <summary>
    /// Sample entity source that monitors customer-related events.
    /// In a real implementation, this would integrate with a customer management system,
    /// database change logs, message queues, or other external systems.
    /// </summary>
    public class CustomerEventSource : IEntitySource
    {
        private readonly ILogger<CustomerEventSource> _logger;
        private readonly Timer? _simulationTimer;
        private bool _isActive = false;
        private bool _disposed = false;

        /// <inheritdoc />
        public string Id { get; }

        /// <inheritdoc />
        public string FriendlyName { get; }

        /// <inheritdoc />
        public string EntityType => "customer";

        /// <inheritdoc />
        public bool IsActive => _isActive;

        /// <inheritdoc />
        public event EventHandler<EntityChangeEventArgs>? EntityChanged;

        /// <summary>
        /// Initializes a new instance of the CustomerEventSource
        /// </summary>
        /// <param name="logger">Logger for the customer event source</param>
        /// <param name="sourceId">Unique identifier for this source instance</param>
        public CustomerEventSource(ILogger<CustomerEventSource> logger, string? sourceId = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            Id = sourceId ?? "customer-source-default";
            FriendlyName = "Customer Event Source";
        }

        /// <inheritdoc />
        public Task InitializeAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(CustomerEventSource));

            if (_isActive)
            {
                _logger.LogWarning("CustomerEventSource '{SourceId}' is already active", Id);
                return Task.CompletedTask;
            }

            _isActive = true;
            _logger.LogInformation("Initialized CustomerEventSource '{SourceId}'", Id);
            
            // In a real implementation, this is where you would:
            // - Connect to database change streams
            // - Subscribe to message queues
            // - Start polling external APIs
            // - Set up webhooks or event listeners
            
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task ShutdownAsync()
        {
            if (!_isActive)
            {
                _logger.LogWarning("CustomerEventSource '{SourceId}' is already inactive", Id);
                return Task.CompletedTask;
            }

            _isActive = false;
            _logger.LogInformation("Shutdown CustomerEventSource '{SourceId}'", Id);
            
            // In a real implementation, this is where you would:
            // - Close database connections
            // - Unsubscribe from message queues
            // - Stop timers and background tasks
            // - Clean up resources
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Simulates a customer creation event
        /// In a real implementation, this would be called when an actual customer is created
        /// </summary>
        /// <param name="customerId">The ID of the created customer</param>
        /// <param name="customerData">Additional customer data</param>
        public void SimulateCustomerCreated(string customerId, Dictionary<string, object?>? customerData = null)
        {
            if (!_isActive)
            {
                throw new InvalidOperationException($"CustomerEventSource '{Id}' is not active");
            }

            if (customerId == null)
            {
                throw new ArgumentNullException(nameof(customerId), "Customer ID cannot be null");
            }

            if (string.IsNullOrWhiteSpace(customerId))
            {
                throw new ArgumentException("Customer ID cannot be empty or whitespace", nameof(customerId));
            }

            var eventArgs = new EntityChangeEventArgs(
                EntityType,
                customerId,
                "created",
                customerData ?? new Dictionary<string, object?>(),
                new Dictionary<string, object?>
                {
                    { "source", Id },
                    { "timestamp", DateTimeOffset.UtcNow },
                    { "eventVersion", "1.0" }
                }
            );

            _logger.LogInformation("Customer created event: {CustomerId}", customerId);
            OnEntityChanged(eventArgs);
        }

        /// <summary>
        /// Simulates a customer update event
        /// In a real implementation, this would be called when an actual customer is updated
        /// </summary>
        /// <param name="customerId">The ID of the updated customer</param>
        /// <param name="customerData">Updated customer data</param>
        public void SimulateCustomerUpdated(string customerId, Dictionary<string, object?>? customerData = null)
        {
            if (!_isActive)
            {
                throw new InvalidOperationException($"CustomerEventSource '{Id}' is not active");
            }

            if (customerId == null)
            {
                throw new ArgumentNullException(nameof(customerId), "Customer ID cannot be null");
            }

            if (string.IsNullOrWhiteSpace(customerId))
            {
                throw new ArgumentException("Customer ID cannot be empty or whitespace", nameof(customerId));
            }

            var eventArgs = new EntityChangeEventArgs(
                EntityType,
                customerId,
                "updated",
                customerData ?? new Dictionary<string, object?>(),
                new Dictionary<string, object?>
                {
                    { "source", Id },
                    { "timestamp", DateTimeOffset.UtcNow },
                    { "eventVersion", "1.0" }
                }
            );

            _logger.LogInformation("Customer updated event: {CustomerId}", customerId);
            OnEntityChanged(eventArgs);
        }

        /// <summary>
        /// Simulates a customer deletion event
        /// In a real implementation, this would be called when an actual customer is deleted
        /// </summary>
        /// <param name="customerId">The ID of the deleted customer</param>
        /// <param name="customerData">Final customer data before deletion</param>
        public void SimulateCustomerDeleted(string customerId, Dictionary<string, object?>? customerData = null)
        {
            if (!_isActive)
            {
                throw new InvalidOperationException($"CustomerEventSource '{Id}' is not active");
            }

            if (customerId == null)
            {
                throw new ArgumentNullException(nameof(customerId), "Customer ID cannot be null");
            }

            if (string.IsNullOrWhiteSpace(customerId))
            {
                throw new ArgumentException("Customer ID cannot be empty or whitespace", nameof(customerId));
            }

            var eventArgs = new EntityChangeEventArgs(
                EntityType,
                customerId,
                "deleted",
                customerData ?? new Dictionary<string, object?>(),
                new Dictionary<string, object?>
                {
                    { "source", Id },
                    { "timestamp", DateTimeOffset.UtcNow },
                    { "eventVersion", "1.0" }
                }
            );

            _logger.LogInformation("Customer deleted event: {CustomerId}", customerId);
            OnEntityChanged(eventArgs);
        }

        /// <summary>
        /// Raises the EntityChanged event
        /// </summary>
        /// <param name="eventArgs">The entity change event arguments</param>
        protected virtual void OnEntityChanged(EntityChangeEventArgs eventArgs)
        {
            try
            {
                EntityChanged?.Invoke(this, eventArgs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while raising EntityChanged event for customer {CustomerId}", eventArgs.EntityId);
                // Don't rethrow - we don't want event handler exceptions to break the source
            }
        }

        /// <summary>
        /// Disposes the customer event source
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the customer event source
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                if (_isActive)
                {
                    _ = ShutdownAsync(); // Fire and forget shutdown
                }
                
                _simulationTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}
