using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using ProductBundles.Sdk;

namespace ProductBundles.Core
{
    /// <summary>
    /// Service responsible for managing and executing product bundle plugins
    /// </summary>
    public class PluginManager
    {
        private readonly PluginLoader _pluginLoader;
        private readonly ILogger<PluginManager> _logger;

        /// <summary>
        /// Initializes a new instance of the PluginManager class
        /// </summary>
        /// <param name="pluginLoader">The plugin loader instance</param>
        /// <param name="logger">Logger instance</param>
        public PluginManager(PluginLoader pluginLoader, ILogger<PluginManager>? logger = null)
        {
            _pluginLoader = pluginLoader ?? throw new ArgumentNullException(nameof(pluginLoader));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<PluginManager>.Instance;
        }

        /// <summary>
        /// Gets all loaded plugins
        /// </summary>
        public IReadOnlyList<IAmAProductBundle> LoadedPlugins => _pluginLoader.LoadedPlugins;

        /// <summary>
        /// Initializes all loaded plugins
        /// </summary>
        public void InitializePlugins()
        {
            _logger.LogInformation("Initializing {PluginCount} plugins", LoadedPlugins.Count);
            
            foreach (var plugin in LoadedPlugins)
            {
                try
                {
                    plugin.Initialize();
                    _logger.LogInformation("Initialized plugin: {PluginName}", plugin.FriendlyName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error initializing plugin {PluginName}", plugin.FriendlyName);
                }
            }
        }

        /// <summary>
        /// Executes all loaded plugins
        /// </summary>
        public void ExecutePlugins()
        {
            ExecutePlugins("default", new Dictionary<string, object?>());
        }

        /// <summary>
        /// Executes all loaded plugins with specified event and property values
        /// </summary>
        /// <param name="eventName">The event that triggered the execution</param>
        /// <param name="propertyValues">Dictionary of property values to pass to plugins</param>
        public void ExecutePlugins(string eventName, Dictionary<string, object?> propertyValues)
        {
            _logger.LogInformation("Executing {PluginCount} plugins for event: {EventName}", LoadedPlugins.Count, eventName);
            
            foreach (var plugin in LoadedPlugins)
            {
                try
                {
                    // Get the plugin's default property values
                    var defaultValues = plugin.GetDefaultPropertyValues();
                    
                    // Create a dictionary with defaults and merge with provided values
                    var pluginPropertyValues = new Dictionary<string, object?>(defaultValues);
                    
                    // Override with provided values
                    if (propertyValues != null)
                    {
                        foreach (var kvp in propertyValues)
                        {
                            pluginPropertyValues[kvp.Key] = kvp.Value;
                        }
                    }
                    
                    // Create a ProductBundleInstance for this execution
                    var bundleInstance = new ProductBundleInstance(
                        id: Guid.NewGuid().ToString(),
                        productBundleId: plugin.Id,
                        productBundleVersion: plugin.Version,
                        properties: pluginPropertyValues
                    );
                    
                    var result = plugin.Execute(eventName, bundleInstance);
                    _logger.LogInformation("Executed plugin: {PluginName}", plugin.FriendlyName);
                    
                    // Optionally log the result
                    if (result.Properties.ContainsKey("status"))
                    {
                        _logger.LogDebug("Plugin {PluginName} returned status: {Status}", plugin.FriendlyName, result.Properties["status"]);
                    }
                    
                    // Log result instance details
                    _logger.LogDebug("Plugin {PluginName} result instance ID: {ResultInstanceId}", plugin.FriendlyName, result.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error executing plugin {PluginName}", plugin.FriendlyName);
                }
            }
        }

        /// <summary>
        /// Disposes all loaded plugins
        /// </summary>
        public void DisposePlugins()
        {
            _logger.LogInformation("Disposing {PluginCount} plugins", LoadedPlugins.Count);
            
            foreach (var plugin in LoadedPlugins)
            {
                try
                {
                    plugin.Dispose();
                    _logger.LogDebug("Disposed plugin: {PluginName}", plugin.FriendlyName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing plugin {PluginName}", plugin.FriendlyName);
                }
            }
        }

        /// <summary>
        /// Gets a plugin by its ID
        /// </summary>
        /// <param name="id">The plugin ID</param>
        /// <returns>The plugin instance or null if not found</returns>
        public IAmAProductBundle GetPluginById(string id)
        {
            return _pluginLoader.GetPluginById(id);
        }
        
        /// <summary>
        /// Upgrades a ProductBundleInstance to the current version of the specified plugin
        /// </summary>
        /// <param name="bundleInstance">The ProductBundleInstance to upgrade</param>
        /// <returns>The upgraded ProductBundleInstance, or null if the plugin is not found</returns>
        public ProductBundleInstance? UpgradeInstance(ProductBundleInstance bundleInstance)
        {
            var plugin = GetPluginById(bundleInstance.ProductBundleId);
            if (plugin == null)
            {
                _logger.LogWarning("Plugin with ID '{PluginId}' not found for upgrade", bundleInstance.ProductBundleId);
                return null;
            }
            
            _logger.LogInformation("Upgrading instance {InstanceId} from version {OldVersion} to {NewVersion}", 
                bundleInstance.Id, bundleInstance.ProductBundleVersion, plugin.Version);
            
            try
            {
                var upgradedInstance = plugin.UpgradeProductBundleInstance(bundleInstance);
                _logger.LogInformation("Successfully upgraded instance {InstanceId}", bundleInstance.Id);
                return upgradedInstance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upgrading instance {InstanceId}", bundleInstance.Id);
                return null;
            }
        }
    }
}
