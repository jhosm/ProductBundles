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
    /// Loads all ProductBundleInstance objects from storage and processes each one
    /// </summary>
    /// <param name="productBundleId">The ID of the ProductBundle to execute</param>
    /// <param name="recurringJobName">The name of the recurring job being executed</param>
    /// <param name="parameters">Additional parameters for the job execution</param>
    [Hangfire.Queue("recurring")]
    public async Task ExecuteRecurringJobAsync(string productBundleId, string recurringJobName, Dictionary<string, object?> parameters)
    {
        await ExecuteOperationAsync(
            operationName: $"recurring job '{recurringJobName}'",
            productBundleId: productBundleId,
            operation: async (plugin) =>
            {
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
                    var success = await ProcessInstanceSafelyAsync(instance, "recurring job processing", async (inst) =>
                    {
                        // Add recurring job metadata to the instance properties
                        var instanceCopy = CreateInstanceCopyWithJobMetadata(inst, parameters, recurringJobName, recurringJob);
                        
                        // Execute the plugin's HandleEvent method
                        var result = plugin.HandleEvent(eventName, instanceCopy);
                        
                        // Update the instance in storage
                        return await UpdateInstanceInStorageAsync(result, inst.Id, $"recurring job '{recurringJobName}'");
                    });

                    processedCount++;
                    if (success) updatedCount++;
                }

                _logger.LogInformation("Completed recurring job '{RecurringJobName}' for ProductBundle '{ProductBundleId}'. Processed {ProcessedCount} instances, updated {UpdatedCount} instances", 
                    recurringJobName, productBundleId, processedCount, updatedCount);
            });
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

            await ExecuteOperationAsync(
                operationName: $"instance execution for '{instanceId}'",
                productBundleId: instance.ProductBundleId,
                operation: async (plugin) =>
                {
                    // Execute the plugin
                    var result = plugin.HandleEvent(eventName, instance);

                    // Update the original instance with the result
                    await UpdateInstanceInStorageAsync(result, instanceId, "ProductBundle execution");

                    _logger.LogInformation("Successfully executed ProductBundle '{ProductBundleId}' for instance '{InstanceId}'. Result updated in storage.", 
                        instance.ProductBundleId, instanceId);
                });
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
        await ExecuteOperationAsync(
            operationName: "bulk upgrade",
            productBundleId: productBundleId,
            operation: async (plugin) =>
            {
                var instances = await _instanceStorage.GetByProductBundleIdAsync(productBundleId);
                var upgradedCount = 0;

                foreach (var instance in instances)
                {
                    if (instance.ProductBundleVersion != plugin.Version)
                    {
                        var success = await ProcessInstanceSafelyAsync(instance, "upgrade", async (inst) =>
                        {
                            var upgraded = plugin.UpgradeProductBundleInstance(inst);
                            var updated = await _instanceStorage.UpdateAsync(upgraded);

                            if (updated)
                            {
                                _logger.LogDebug("Upgraded instance '{InstanceId}' from version '{OldVersion}' to '{NewVersion}'", 
                                    inst.Id, inst.ProductBundleVersion, plugin.Version);
                                return true;
                            }
                            return false;
                        });

                        if (success) upgradedCount++;
                    }
                }

                _logger.LogInformation("Completed bulk upgrade for ProductBundle '{ProductBundleId}'. Upgraded {UpgradedCount} of {TotalCount} instances", 
                    productBundleId, upgradedCount, instances.Count());
            });
    }

    #region Helper Methods

    /// <summary>
    /// Executes an operation with standardized plugin loading, error handling, and logging
    /// </summary>
    /// <param name="operationName">The name of the operation for logging purposes</param>
    /// <param name="productBundleId">The ProductBundle ID to load</param>
    /// <param name="operation">The operation to execute with the loaded plugin</param>
    private async Task ExecuteOperationAsync(
        string operationName, 
        string productBundleId, 
        Func<IAmAProductBundle, Task> operation)
    {
        _logger.LogInformation("Starting {OperationName} for ProductBundle '{ProductBundleId}'", 
            operationName, productBundleId);

        try
        {
            var plugin = await GetProductBundleAsync(productBundleId);
            if (plugin == null) return;

            await operation(plugin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during {OperationName} for ProductBundle '{ProductBundleId}'", 
                operationName, productBundleId);
            throw;
        }
    }

    /// <summary>
    /// Gets a plugin by ID or logs a warning if not found
    /// </summary>
    /// <param name="productBundleId">The ProductBundle ID to load</param>
    /// <returns>The plugin or null if not found</returns>
    private async Task<IAmAProductBundle?> GetProductBundleAsync(string productBundleId)
    {
        var plugin = _pluginLoader.GetPluginById(productBundleId);
        if (plugin == null)
        {
            _logger.LogWarning("ProductBundle '{ProductBundleId}' not found", productBundleId);
        }
        return await Task.FromResult(plugin);
    }

    /// <summary>
    /// Safely processes a ProductBundleInstance with error handling and logging
    /// </summary>
    /// <param name="instance">The instance to process</param>
    /// <param name="operationType">The type of operation being performed for logging</param>
    /// <param name="processor">The processing function</param>
    /// <returns>True if processing was successful, false otherwise</returns>
    private async Task<bool> ProcessInstanceSafelyAsync(
        ProductBundleInstance instance, 
        string operationType, 
        Func<ProductBundleInstance, Task<bool>> processor)
    {
        try
        {
            return await processor(instance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {OperationType} for instance '{InstanceId}'", 
                operationType, instance.Id);
            return false;
        }
    }

    /// <summary>
    /// Updates an instance in storage with consistent logging
    /// </summary>
    /// <param name="result">The result instance to update with</param>
    /// <param name="originalInstanceId">The original instance ID</param>
    /// <param name="operationContext">Context of the operation for logging</param>
    /// <returns>True if update was successful, false otherwise</returns>
    private async Task<bool> UpdateInstanceInStorageAsync(
        ProductBundleInstance result, 
        string originalInstanceId, 
        string operationContext)
    {
        // Use the original instance ID to maintain continuity
        var instanceToUpdate = new ProductBundleInstance(originalInstanceId, result.ProductBundleId, result.ProductBundleVersion)
        {
            Properties = result.Properties
        };

        var updated = await _instanceStorage.UpdateAsync(instanceToUpdate);
        
        if (updated)
        {
            _logger.LogDebug("Successfully updated instance '{InstanceId}' after {OperationContext}", 
                originalInstanceId, operationContext);
        }
        else
        {
            _logger.LogWarning("Failed to update instance '{InstanceId}' after {OperationContext}", 
                originalInstanceId, operationContext);
        }
        
        return updated;
    }

    /// <summary>
    /// Creates a copy of a ProductBundleInstance with recurring job metadata
    /// </summary>
    /// <param name="instance">The original instance</param>
    /// <param name="parameters">Job parameters</param>
    /// <param name="recurringJobName">The recurring job name</param>
    /// <param name="recurringJob">The recurring job definition</param>
    /// <returns>A new instance with job metadata</returns>
    private ProductBundleInstance CreateInstanceCopyWithJobMetadata(
        ProductBundleInstance instance, 
        Dictionary<string, object?> parameters, 
        string recurringJobName, 
        RecurringBackgroundJob recurringJob)
    {
        var copy = new ProductBundleInstance(instance.Id, instance.ProductBundleId, instance.ProductBundleVersion);
        
        // Copy all original properties
        foreach (var prop in instance.Properties)
        {
            copy.Properties[prop.Key] = prop.Value;
        }
        
        // Add recurring job metadata
        copy.Properties["_recurringJobName"] = recurringJobName;
        copy.Properties["_recurringJobDescription"] = recurringJob.Description;
        copy.Properties["_executionTimestamp"] = DateTime.UtcNow;
        
        // Add any additional parameters from the job
        foreach (var param in parameters)
        {
            copy.Properties[$"_job_{param.Key}"] = param.Value;
        }
        
        return copy;
    }

    #endregion
}
