using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using ProductBundles.Sdk;

namespace ProductBundles.Core
{
    /// <summary>
    /// Service responsible for loading product bundle plugins
    /// </summary>
    public class ProductBundlesLoader
    {
        private readonly string _pluginsPath;
        private readonly List<IAmAProductBundle> _loadedPlugins;
        private readonly ILogger<ProductBundlesLoader> _logger;

        /// <summary>
        /// Initializes a new instance of the ProductBundlesLoader class
        /// </summary>
        /// <param name="pluginsPath">Path to the plugins directory</param>
        /// <param name="logger">Logger instance</param>
        public ProductBundlesLoader(string pluginsPath = "plugins", ILogger<ProductBundlesLoader>? logger = null)
        {
            _pluginsPath = pluginsPath;
            _loadedPlugins = new List<IAmAProductBundle>();
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ProductBundlesLoader>.Instance;
        }

        /// <summary>
        /// Gets all loaded plugins
        /// </summary>
        public IReadOnlyList<IAmAProductBundle> LoadedPlugins => _loadedPlugins.AsReadOnly();

        /// <summary>
        /// Loads all plugins from the plugins directory
        /// </summary>
        /// <returns>List of loaded plugin instances</returns>
        public IReadOnlyList<IAmAProductBundle> LoadPlugins()
        {
            _logger.LogInformation("Loading plugins from: {PluginsPath}", Path.GetFullPath(_pluginsPath));
            
            if (!Directory.Exists(_pluginsPath))
            {
                _logger.LogWarning("Plugins directory '{PluginsPath}' does not exist. Creating it...", _pluginsPath);
                Directory.CreateDirectory(_pluginsPath);
                return _loadedPlugins.AsReadOnly();
            }

            var dllFiles = Directory.GetFiles(_pluginsPath, "*.dll", SearchOption.AllDirectories);
            _logger.LogInformation("Found {DllCount} DLL files", dllFiles.Length);

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    LoadPluginFromAssembly(dllFile);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading plugin from {DllFile}", dllFile);
                }
            }

            _logger.LogInformation("Successfully loaded {PluginCount} plugins", _loadedPlugins.Count);
            return _loadedPlugins.AsReadOnly();
        }

        /// <summary>
        /// Gets a plugin by its ID
        /// </summary>
        /// <param name="id">The plugin ID</param>
        /// <returns>The plugin instance or null if not found</returns>
        public IAmAProductBundle GetPluginById(string id)
        {
            return _loadedPlugins.FirstOrDefault(p => p.Id == id);
        }

        /// <summary>
        /// Loads plugins from a specific assembly file
        /// </summary>
        /// <param name="assemblyPath">Path to the assembly file</param>
        private void LoadPluginFromAssembly(string assemblyPath)
        {
            _logger.LogDebug("Loading assembly: {AssemblyName}", Path.GetFileName(assemblyPath));
            
            // Load the assembly
            var assembly = Assembly.LoadFrom(assemblyPath);
            
            // Find all types that implement IAmAProductBundle
            var pluginTypes = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(IAmAProductBundle).IsAssignableFrom(t))
                .ToList();

            _logger.LogDebug("Found {PluginTypeCount} plugin types in {AssemblyName}", pluginTypes.Count, Path.GetFileName(assemblyPath));

            foreach (var pluginType in pluginTypes)
            {
                try
                {
                    // Create instance of the plugin
                    var pluginInstance = Activator.CreateInstance(pluginType) as IAmAProductBundle;
                    
                    if (pluginInstance != null)
                    {
                        _logger.LogInformation("Successfully instantiated plugin: {PluginTypeName}", pluginType.Name);
                        _logger.LogDebug("Plugin details - Id: {PluginId}, Name: {PluginName}, Version: {PluginVersion}", 
                            pluginInstance.Id, pluginInstance.FriendlyName, pluginInstance.Version);
                        
                        _loadedPlugins.Add(pluginInstance);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to instantiate plugin: {PluginTypeName}", pluginType.Name);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error instantiating plugin {PluginTypeName}", pluginType.Name);
                }
            }
        }
    }
}
