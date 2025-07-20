using ProductBundles.Core;
using ProductBundles.Core.Storage;
using ProductBundles.Sdk;
using Hangfire;

namespace ProductBundles.Api.Services;

/// <summary>
/// Background service for executing ProductBundle operations via Hangfire
/// </summary>
public class ProductBundleBackgroundService
{
    private readonly ProductBundlesLoader _pluginLoader;
    private readonly IProductBundleInstanceStorage _instanceStorage;
    private readonly ILogger<ProductBundleBackgroundService> _logger;

    public ProductBundleBackgroundService(
        ProductBundlesLoader pluginLoader,
        IProductBundleInstanceStorage instanceStorage,
        ILogger<ProductBundleBackgroundService> logger)
    {
        _pluginLoader = pluginLoader;
        _instanceStorage = instanceStorage;
        _logger = logger;
    }

    /// <summary>
    /// Executes a ProductBundle plugin asynchronously in response to a recurring background job
    /// Loads all ProductBundleInstance objects from storage and processes each one
    /// </summary>
    /// <param name="productBundleId">The ID of the ProductBundle to execute</param>
    /// <param name="recurringJobName">The name of the recurring job being executed</param>
    /// <param name="parameters">Additional parameters for the job execution</param>
    [Hangfire.Queue("recurring")]
    public async Task ExecuteRecurringJobAsync(string productBundleId, string recurringJobName, Dictionary<string, object?> parameters)
    {
        _logger.LogInformation("Executing recurring job '{RecurringJobName}' for ProductBundle '{ProductBundleId}'", 
            recurringJobName, productBundleId);

        try
        {
            // Load the plugin
            var plugin = _pluginLoader.GetPluginById(productBundleId);
            if (plugin == null)
            {
                _logger.LogWarning("ProductBundle '{ProductBundleId}' not found for recurring job '{RecurringJobName}'", 
                    productBundleId, recurringJobName);
                return;
            }

            // Find the recurring job definition
            var recurringJob = plugin.RecurringBackgroundJobs
                .FirstOrDefault(job => job.Name == recurringJobName);
            
            if (recurringJob == null)
            {
                _logger.LogWarning("Recurring job '{RecurringJobName}' not found in ProductBundle '{ProductBundleId}'", 
                    recurringJobName, productBundleId);
                return;
            }

            // Get the event name from parameters (or use a default)
            var eventName = parameters.GetValueOrDefault("eventName")?.ToString() ?? $"recurring.{recurringJobName}";

            // Load ProductBundleInstance objects for this specific ProductBundle from storage
            var allInstances = await _instanceStorage.GetByProductBundleIdAsync(productBundleId);
            var processedCount = 0;
            var updatedCount = 0;

            foreach (var instance in allInstances)
            {
                try
                {
                    // Add recurring job metadata to the instance properties
                    var instanceCopy = new ProductBundleInstance(
                        instance.Id,
                        instance.ProductBundleId, 
                        instance.ProductBundleVersion,
                        new Dictionary<string, object?>(instance.Properties)
                    );

                    // Add job parameters and metadata
                    foreach (var param in parameters)
                    {
                        instanceCopy.Properties[param.Key] = param.Value;
                    }

                    instanceCopy.Properties["_recurringJobName"] = recurringJobName;
                    instanceCopy.Properties["_cronSchedule"] = recurringJob.CronSchedule;
                    instanceCopy.Properties["_executionTimestamp"] = DateTime.UtcNow;
                    instanceCopy.Properties["_jobDescription"] = recurringJob.Description;

                    // Execute the plugin's HandleEvent method
                    var result = plugin.HandleEvent(eventName, instanceCopy);
                    processedCount++;

                    // Update the instance in storage with the result
                    var updated = await _instanceStorage.UpdateAsync(result);
                    
                    if (updated)
                    {
                        updatedCount++;
                        _logger.LogDebug("Successfully processed and updated instance '{InstanceId}' for recurring job '{RecurringJobName}'", 
                            instance.Id, recurringJobName);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update instance '{InstanceId}' after processing recurring job '{RecurringJobName}'", 
                            instance.Id, recurringJobName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing instance '{InstanceId}' for recurring job '{RecurringJobName}'", 
                        instance.Id, recurringJobName);
                    // Continue processing other instances
                }
            }

            _logger.LogInformation("Completed recurring job '{RecurringJobName}' for ProductBundle '{ProductBundleId}'. Processed {ProcessedCount} instances, updated {UpdatedCount} instances", 
                recurringJobName, productBundleId, processedCount, updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing recurring job '{RecurringJobName}' for ProductBundle '{ProductBundleId}'", 
                recurringJobName, productBundleId);
            throw;
        }
    }

    /// <summary>
    /// Executes a ProductBundle plugin with a specific instance
    /// </summary>
    /// <param name="instanceId">The ID of the ProductBundleInstance to process</param>
    /// <param name="eventName">The event name to trigger</param>
    [Hangfire.Queue("productbundles")]
    public async Task ExecuteProductBundleAsync(string instanceId, string eventName = "background.execute")
    {
        _logger.LogInformation("Executing ProductBundle for instance '{InstanceId}' with event '{EventName}'", 
            instanceId, eventName);

        try
        {
            // Get the instance from storage
            var instance = await _instanceStorage.GetAsync(instanceId);
            if (instance == null)
            {
                _logger.LogWarning("ProductBundleInstance '{InstanceId}' not found", instanceId);
                return;
            }

            // Get the plugin
            var plugin = _pluginLoader.GetPluginById(instance.ProductBundleId);
            if (plugin == null)
            {
                _logger.LogWarning("ProductBundle '{ProductBundleId}' not found for instance '{InstanceId}'", 
                    instance.ProductBundleId, instanceId);
                return;
            }

            // Execute the plugin
            var result = plugin.HandleEvent(eventName, instance);

            // Update the original instance with the result
            var updated = await _instanceStorage.UpdateAsync(result);

            if (updated)
            {
                _logger.LogInformation("Successfully executed ProductBundle '{ProductBundleId}' for instance '{InstanceId}'. Result updated in storage.", 
                    instance.ProductBundleId, instanceId);
            }
            else
            {
                _logger.LogWarning("Failed to update instance '{InstanceId}' after ProductBundle execution", instanceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing ProductBundle for instance '{InstanceId}'", instanceId);
            throw;
        }
    }

    /// <summary>
    /// Bulk upgrade all instances of a specific ProductBundle
    /// </summary>
    /// <param name="productBundleId">The ProductBundle ID to upgrade instances for</param>
    [Hangfire.Queue("productbundles")]
    public async Task UpgradeProductBundleInstancesAsync(string productBundleId)
    {
        _logger.LogInformation("Starting bulk upgrade for ProductBundle '{ProductBundleId}'", productBundleId);

        try
        {
            var plugin = _pluginLoader.GetPluginById(productBundleId);
            if (plugin == null)
            {
                _logger.LogWarning("ProductBundle '{ProductBundleId}' not found for upgrade", productBundleId);
                return;
            }

            var instances = await _instanceStorage.GetByProductBundleIdAsync(productBundleId);
            var upgradedCount = 0;

            foreach (var instance in instances)
            {
                if (instance.ProductBundleVersion != plugin.Version)
                {
                    try
                    {
                        var upgraded = plugin.UpgradeProductBundleInstance(instance);
                        var updated = await _instanceStorage.UpdateAsync(upgraded);

                        if (updated)
                        {
                            upgradedCount++;
                            _logger.LogDebug("Upgraded instance '{InstanceId}' from version '{OldVersion}' to '{NewVersion}'", 
                                instance.Id, instance.ProductBundleVersion, plugin.Version);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to upgrade instance '{InstanceId}' for ProductBundle '{ProductBundleId}'", 
                            instance.Id, productBundleId);
                    }
                }
            }

            _logger.LogInformation("Completed bulk upgrade for ProductBundle '{ProductBundleId}'. Upgraded {UpgradedCount} of {TotalCount} instances", 
                productBundleId, upgradedCount, instances.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during bulk upgrade for ProductBundle '{ProductBundleId}'", productBundleId);
            throw;
        }
    }
}
