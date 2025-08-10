using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductBundles.Core.BackgroundJobs;

/// <summary>
/// Interface for processing background jobs without being tied to a specific implementation
/// </summary>
public interface IBackgroundJobProcessor
{
    /// <summary>
    /// Executes a ProductBundle plugin with a specific instance
    /// </summary>
    /// <param name="instanceId">The ID of the ProductBundleInstance to process</param>
    /// <param name="eventName">The event name to trigger</param>
    Task ExecuteProductBundleAsync(string instanceId, string eventName = "background.execute");

    /// <summary>
    /// Executes a recurring job for a ProductBundle
    /// </summary>
    /// <param name="productBundleId">The ID of the ProductBundle to execute</param>
    /// <param name="recurringJobName">The name of the recurring job being executed</param>
    /// <param name="parameters">Additional parameters for the job execution</param>
    Task ExecuteRecurringJobAsync(string productBundleId, string recurringJobName, Dictionary<string, object?> parameters);

    /// <summary>
    /// Bulk upgrade all instances of a specific ProductBundle
    /// </summary>
    /// <param name="productBundleId">The ProductBundle ID to upgrade instances for</param>
    Task UpgradeProductBundleInstancesAsync(string productBundleId);
}
