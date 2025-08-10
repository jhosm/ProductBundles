using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductBundles.Sdk
{
    /// <summary>
    /// Interface that identifies a class as an entity source plugin that monitors external systems for entity changes
    /// </summary>
    public interface IAmAnEntitySource : IDisposable
    {
        /// <summary>
        /// Gets the entity source identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the friendly name of the entity source
        /// </summary>
        string FriendlyName { get; }

        /// <summary>
        /// Gets the description of the entity source
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Gets the version of the entity source
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Gets the entity type this source monitors (e.g., "customer", "order", "product")
        /// </summary>
        string EntityType { get; }

        /// <summary>
        /// Gets the event types this source can emit (e.g., "created", "updated", "deleted")
        /// </summary>
        IReadOnlyList<string> SupportedEventTypes { get; }

        /// <summary>
        /// Initialize the entity source (setup connections, subscriptions, etc.)
        /// </summary>
        /// <returns>A task representing the initialization operation</returns>
        Task InitializeAsync();

        /// <summary>
        /// Start monitoring for entity changes
        /// </summary>
        /// <returns>A task representing the start operation</returns>
        Task StartMonitoringAsync();

        /// <summary>
        /// Stop monitoring for entity changes
        /// </summary>
        /// <returns>A task representing the stop operation</returns>
        Task StopMonitoringAsync();

        /// <summary>
        /// Event raised when an entity change is detected
        /// </summary>
        event EventHandler<EntityChangeEventArgs>? EntityChanged;
    }
}
