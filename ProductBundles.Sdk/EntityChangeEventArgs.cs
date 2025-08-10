using System;
using System.Collections.Generic;

namespace ProductBundles.Sdk
{
    /// <summary>
    /// Event arguments for entity change events
    /// </summary>
    public class EntityChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the type of entity that changed (e.g., "customer", "order", "product")
        /// </summary>
        public string EntityType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier of the entity that changed
        /// </summary>
        public string EntityId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the type of change event (e.g., "created", "updated", "deleted")
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the entity data payload
        /// </summary>
        public Dictionary<string, object?> EntityData { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// Gets or sets the timestamp when the change occurred
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets additional metadata about the event
        /// </summary>
        public Dictionary<string, object?> Metadata { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// Initializes a new instance of the EntityChangeEventArgs class
        /// </summary>
        public EntityChangeEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the EntityChangeEventArgs class with the specified parameters
        /// </summary>
        /// <param name="entityType">The type of entity that changed</param>
        /// <param name="entityId">The unique identifier of the entity</param>
        /// <param name="eventType">The type of change event</param>
        /// <param name="entityData">The entity data payload</param>
        /// <param name="metadata">Additional metadata about the event</param>
        public EntityChangeEventArgs(string entityType, string entityId, string eventType, 
            Dictionary<string, object?>? entityData = null, Dictionary<string, object?>? metadata = null)
        {
            EntityType = entityType ?? throw new ArgumentNullException(nameof(entityType));
            EntityId = entityId ?? throw new ArgumentNullException(nameof(entityId));
            EventType = eventType ?? throw new ArgumentNullException(nameof(eventType));
            EntityData = entityData ?? new Dictionary<string, object?>();
            Metadata = metadata ?? new Dictionary<string, object?>();
            Timestamp = DateTime.UtcNow;
        }
    }
}
