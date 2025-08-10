using ProductBundles.Core.BackgroundJobs;
using Hangfire;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductBundles.Api.Services;

/// <summary>
/// Hangfire-specific wrapper that bridges Hangfire jobs to the abstracted background job processor
/// </summary>
public class HangfireBackgroundJobWrapper
{
    private readonly IBackgroundJobProcessor _backgroundJobProcessor;

    public HangfireBackgroundJobWrapper(IBackgroundJobProcessor backgroundJobProcessor)
    {
        _backgroundJobProcessor = backgroundJobProcessor;
    }

    /// <summary>
    /// Hangfire job wrapper for executing recurring jobs
    /// </summary>
    /// <param name="productBundleId">The ID of the ProductBundle to execute</param>
    /// <param name="recurringJobName">The name of the recurring job being executed</param>
    /// <param name="parameters">Additional parameters for the job execution</param>
    [Queue("recurring")]
    public async Task ExecuteRecurringJobAsync(string productBundleId, string recurringJobName, Dictionary<string, object?> parameters)
    {
        await _backgroundJobProcessor.ExecuteRecurringJobAsync(productBundleId, recurringJobName, parameters);
    }

    /// <summary>
    /// Hangfire job wrapper for executing ProductBundle instances
    /// </summary>
    /// <param name="instanceId">The ID of the ProductBundleInstance to process</param>
    /// <param name="eventName">The event name to trigger</param>
    [Queue("productbundles")]
    public async Task ExecuteProductBundleAsync(string instanceId, string eventName = "background.execute")
    {
        await _backgroundJobProcessor.ExecuteProductBundleAsync(instanceId, eventName);
    }

    /// <summary>
    /// Hangfire job wrapper for bulk upgrading ProductBundle instances
    /// </summary>
    /// <param name="productBundleId">The ProductBundle ID to upgrade instances for</param>
    [Queue("productbundles")]
    public async Task UpgradeProductBundleInstancesAsync(string productBundleId)
    {
        await _backgroundJobProcessor.UpgradeProductBundleInstancesAsync(productBundleId);
    }
}
