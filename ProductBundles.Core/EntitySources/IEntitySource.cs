using ProductBundles.Core.BackgroundJobs;

using ProductBundles.Sdk;

namespace ProductBundles.Core.EntitySources
{
    /// <summary>
    /// Interface for entity sources that can trigger events for ProductBundle plugins.
    /// Entity sources monitor specific types of entities (e.g., customers, orders) and 
    /// notify the system when relevant changes occur.
    /// </summary>
    public interface IEntitySource
    {
        /// <summary>
        /// Gets the unique identifier for this entity source
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Gets the friendly name for this entity source
        /// </summary>
        string FriendlyName { get; }
        
        /// <summary>
        /// Gets the entity type this source monitors (e.g., "customer", "order")
        /// </summary>
        string EntityType { get; }
        
        /// <summary>
        /// Gets a value indicating whether this entity source is currently active
        /// </summary>
        bool IsActive { get; }
        
        /// <summary>
        /// Event raised when an entity change occurs
        /// </summary>
        event EventHandler<EntityChangeEventArgs>? EntityChanged;
        
        /// <summary>
        /// Initializes the entity source and starts monitoring for changes
        /// </summary>
        /// <returns>A task that represents the initialization operation</returns>
        Task InitializeAsync();
        
        /// <summary>
        /// Stops monitoring and cleans up resources
        /// </summary>
        /// <returns>A task that represents the shutdown operation</returns>
        Task ShutdownAsync();
    }
}
