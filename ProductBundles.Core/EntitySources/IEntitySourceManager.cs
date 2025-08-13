using ProductBundles.Core.BackgroundJobs;
using ProductBundles.Sdk;

namespace ProductBundles.Core.EntitySources
{
    /// <summary>
    /// Interface for managing entity sources and coordinating entity change event dispatching
    /// </summary>
    public interface IEntitySourceManager
    {
        /// <summary>
        /// Gets all registered entity sources by their IDs
        /// </summary>
        IReadOnlyDictionary<string, IAmAnEntitySource> EntitySources { get; }

        /// <summary>
        /// Gets the collection of registered background job processors
        /// </summary>
        IReadOnlyDictionary<string, IBackgroundJobProcessor> Processors { get; }

        /// <summary>
        /// Registers an entity source for monitoring
        /// </summary>
        /// <param name="entitySource">The entity source to register</param>
        /// <returns>A task that represents the registration operation</returns>
        Task RegisterEntitySourceAsync(IAmAnEntitySource entitySource);

        /// <summary>
        /// Unregisters an entity source from the manager
        /// </summary>
        /// <param name="entitySourceId">The ID of the entity source to unregister</param>
        /// <returns>A task that represents the unregistration operation</returns>
        Task UnregisterEntitySourceAsync(string entitySourceId);

        /// <summary>
        /// Registers a background job processor that will receive entity change events
        /// </summary>
        /// <param name="processorId">Unique identifier for the processor</param>
        /// <param name="processor">The processor to register</param>
        void RegisterProcessor(string processorId, IBackgroundJobProcessor processor);

        /// <summary>
        /// Unregisters a background job processor
        /// </summary>
        /// <param name="processorId">The ID of the processor to unregister</param>
        void UnregisterProcessor(string processorId);
    }
}
