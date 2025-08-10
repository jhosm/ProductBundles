using Microsoft.Extensions.Logging;
using ProductBundles.Core.BackgroundJobs;
using ProductBundles.Sdk;
using System.Collections.Concurrent;

namespace ProductBundles.Core.EntitySources
{
    /// <summary>
    /// Manages entity sources and coordinates entity change event dispatching to ProductBundle processors.
    /// Acts as the central hub for entity-driven events in the ProductBundle system.
    /// </summary>
    public class EntitySourceManager : IEntitySourceManager, IDisposable
    {
        private readonly ConcurrentDictionary<string, IEntitySource> _entitySources = new();
        private readonly ConcurrentDictionary<string, IBackgroundJobProcessor> _processors = new();
        private readonly ILogger<EntitySourceManager> _logger;
        private readonly object _lockObject = new();
        private bool _disposed = false;

        /// <summary>
        /// Gets the collection of registered entity sources
        /// </summary>
        public IReadOnlyDictionary<string, IEntitySource> EntitySources => _entitySources;

        /// <summary>
        /// Gets the collection of registered background job processors
        /// </summary>
        public IReadOnlyDictionary<string, IBackgroundJobProcessor> Processors => _processors;

        /// <summary>
        /// Initializes a new instance of the EntitySourceManager
        /// </summary>
        /// <param name="logger">Logger for the entity source manager</param>
        public EntitySourceManager(ILogger<EntitySourceManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Registers an entity source with the manager
        /// </summary>
        /// <param name="entitySource">The entity source to register</param>
        /// <returns>A task that represents the registration operation</returns>
        public async Task RegisterEntitySourceAsync(IEntitySource entitySource)
        {
            if (entitySource == null)
                throw new ArgumentNullException(nameof(entitySource));

            if (string.IsNullOrWhiteSpace(entitySource.Id))
                throw new ArgumentException("Entity source must have a valid Id", nameof(entitySource));

            if (_disposed)
                throw new ObjectDisposedException(nameof(EntitySourceManager));

            lock (_lockObject)
            {
                if (_entitySources.ContainsKey(entitySource.Id))
                {
                    _logger.LogWarning("Entity source with Id '{EntitySourceId}' is already registered", entitySource.Id);
                    return;
                }

                // Subscribe to entity change events
                entitySource.EntityChanged += OnEntityChanged;

                if (_entitySources.TryAdd(entitySource.Id, entitySource))
                {
                    _logger.LogInformation("Registered entity source '{EntitySourceId}' for entity type '{EntityType}'",
                        entitySource.Id, entitySource.EntityType);
                }
                else
                {
                    // Cleanup subscription if registration failed
                    entitySource.EntityChanged -= OnEntityChanged;
                    throw new InvalidOperationException($"Failed to register entity source '{entitySource.Id}'");
                }
            }

            try
            {
                await entitySource.InitializeAsync();
                _logger.LogInformation("Initialized entity source '{EntitySourceId}'", entitySource.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize entity source '{EntitySourceId}'", entitySource.Id);
                
                // Remove from registry if initialization failed
                lock (_lockObject)
                {
                    _entitySources.TryRemove(entitySource.Id, out _);
                    entitySource.EntityChanged -= OnEntityChanged;
                }
                throw;
            }
        }

        /// <summary>
        /// Unregisters an entity source from the manager
        /// </summary>
        /// <param name="entitySourceId">The ID of the entity source to unregister</param>
        /// <returns>A task that represents the unregistration operation</returns>
        public async Task UnregisterEntitySourceAsync(string entitySourceId)
        {
            if (string.IsNullOrWhiteSpace(entitySourceId))
                throw new ArgumentException("Entity source ID cannot be null or empty", nameof(entitySourceId));

            if (_disposed)
                throw new ObjectDisposedException(nameof(EntitySourceManager));

            if (!_entitySources.TryRemove(entitySourceId, out var entitySource))
            {
                _logger.LogWarning("Entity source with Id '{EntitySourceId}' was not found for unregistration", entitySourceId);
                return;
            }

            try
            {
                // Unsubscribe from events first
                entitySource.EntityChanged -= OnEntityChanged;

                // Shutdown the entity source
                await entitySource.ShutdownAsync();
                
                _logger.LogInformation("Unregistered entity source '{EntitySourceId}'", entitySourceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while unregistering entity source '{EntitySourceId}'", entitySourceId);
                throw;
            }
        }

        /// <summary>
        /// Registers a background job processor that will receive entity change events
        /// </summary>
        /// <param name="processorId">Unique identifier for the processor</param>
        /// <param name="processor">The processor to register</param>
        public void RegisterProcessor(string processorId, IBackgroundJobProcessor processor)
        {
            if (string.IsNullOrWhiteSpace(processorId))
                throw new ArgumentException("Processor ID cannot be null or empty", nameof(processorId));

            if (processor == null)
                throw new ArgumentNullException(nameof(processor));

            if (_disposed)
                throw new ObjectDisposedException(nameof(EntitySourceManager));

            if (_processors.TryAdd(processorId, processor))
            {
                _logger.LogInformation("Registered processor '{ProcessorId}'", processorId);
            }
            else
            {
                _logger.LogWarning("Processor with Id '{ProcessorId}' is already registered", processorId);
            }
        }

        /// <summary>
        /// Unregisters a background job processor
        /// </summary>
        /// <param name="processorId">The ID of the processor to unregister</param>
        public void UnregisterProcessor(string processorId)
        {
            if (string.IsNullOrWhiteSpace(processorId))
                throw new ArgumentException("Processor ID cannot be null or empty", nameof(processorId));

            if (_disposed)
                throw new ObjectDisposedException(nameof(EntitySourceManager));

            if (_processors.TryRemove(processorId, out _))
            {
                _logger.LogInformation("Unregistered processor '{ProcessorId}'", processorId);
            }
            else
            {
                _logger.LogWarning("Processor with Id '{ProcessorId}' was not found for unregistration", processorId);
            }
        }

        /// <summary>
        /// Handles entity change events from registered entity sources
        /// </summary>
        private async void OnEntityChanged(object? sender, EntityChangeEventArgs e)
        {
            if (_disposed || e == null)
                return;

            var sourceId = (sender as IEntitySource)?.Id ?? "Unknown";
            
            _logger.LogDebug("Received entity change event from source '{SourceId}': {EntityType}.{EntityId} -> {EventType}",
                sourceId, e.EntityType, e.EntityId, e.EventType);

            // Dispatch to all registered processors concurrently
            var tasks = _processors.Values.Select(async processor =>
            {
                try
                {
                    await processor.ProcessEntityEventAsync(e);
                    _logger.LogDebug("Successfully processed entity event in processor for entity {EntityType}.{EntityId}",
                        e.EntityType, e.EntityId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing entity event in processor for entity {EntityType}.{EntityId}",
                        e.EntityType, e.EntityId);
                    // Continue processing other processors even if one fails
                }
            }).ToArray();

            try
            {
                await Task.WhenAll(tasks);
                _logger.LogInformation("Dispatched entity change event to {ProcessorCount} processors: {EntityType}.{EntityId} -> {EventType}",
                    tasks.Length, e.EntityType, e.EntityId, e.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "One or more processors failed to process entity change event: {EntityType}.{EntityId} -> {EventType}",
                    e.EntityType, e.EntityId, e.EventType);
            }
        }

        /// <summary>
        /// Disposes the entity source manager and all registered entity sources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the entity source manager
        /// </summary>
        /// <param name="disposing">True if disposing managed resources</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _logger.LogInformation("Disposing EntitySourceManager with {EntitySourceCount} entity sources",
                    _entitySources.Count);

                // Shutdown all entity sources
                var shutdownTasks = _entitySources.Values.Select(async source =>
                {
                    try
                    {
                        source.EntityChanged -= OnEntityChanged;
                        await source.ShutdownAsync();
                        _logger.LogDebug("Shutdown entity source '{EntitySourceId}'", source.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error shutting down entity source '{EntitySourceId}'", source.Id);
                    }
                }).ToArray();

                try
                {
                    Task.WaitAll(shutdownTasks, TimeSpan.FromSeconds(30));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Timeout or error during entity source shutdown");
                }

                _entitySources.Clear();
                _processors.Clear();
                _disposed = true;
            }
        }
    }
}
