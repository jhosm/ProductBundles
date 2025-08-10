using ProductBundles.Core;
using ProductBundles.Sdk;
using Hangfire;
using System.Linq.Expressions;

namespace ProductBundles.Api.Services;

/// <summary>
/// Manages Hangfire recurring jobs based on ProductBundle RecurringBackgroundJobs definitions
/// </summary>
public class HangfireRecurringJobManager
{
    private readonly ProductBundlesLoader _pluginLoader;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<HangfireRecurringJobManager> _logger;

    public HangfireRecurringJobManager(
        ProductBundlesLoader pluginLoader,
        IRecurringJobManager recurringJobManager,
        ILogger<HangfireRecurringJobManager> logger)
    {
        _pluginLoader = pluginLoader;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    /// <summary>
    /// Initializes all recurring jobs from loaded ProductBundles
    /// </summary>
    public void InitializeRecurringJobs()
    {
        _logger.LogInformation("Initializing recurring jobs from ProductBundles...");

        // Load plugins if not already loaded
        var plugins = _pluginLoader.LoadedPlugins.Any() ? _pluginLoader.LoadedPlugins : _pluginLoader.LoadPlugins();

        var totalJobsRegistered = 0;

        foreach (var plugin in plugins)
        {
            try
            {
                var jobsRegistered = RegisterPluginRecurringJobs(plugin);
                totalJobsRegistered += jobsRegistered;

                _logger.LogInformation("Registered {JobCount} recurring jobs for ProductBundle '{ProductBundleId}'", 
                    jobsRegistered, plugin.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register recurring jobs for ProductBundle '{ProductBundleId}'", 
                    plugin.Id);
            }
        }

        _logger.LogInformation("Successfully initialized {TotalJobs} recurring jobs from {PluginCount} ProductBundles", 
            totalJobsRegistered, plugins.Count);
    }

    /// <summary>
    /// Registers recurring jobs for a specific ProductBundle
    /// </summary>
    /// <param name="plugin">The ProductBundle plugin</param>
    /// <returns>Number of jobs registered</returns>
    public int RegisterPluginRecurringJobs(IAmAProductBundle plugin)
    {
        if (plugin.RecurringBackgroundJobs == null || !plugin.RecurringBackgroundJobs.Any())
        {
            _logger.LogDebug("No recurring jobs defined for ProductBundle '{ProductBundleId}'", plugin.Id);
            return 0;
        }

        var jobsRegistered = 0;

        foreach (var recurringJob in plugin.RecurringBackgroundJobs)
        {
            try
            {
                // Validate cron expression
                if (string.IsNullOrWhiteSpace(recurringJob.CronSchedule))
                {
                    _logger.LogWarning("Invalid cron schedule for job '{JobName}' in ProductBundle '{ProductBundleId}': empty or null", 
                        recurringJob.Name, plugin.Id);
                    continue;
                }

                // Create unique job ID: productBundleId + recurringJobName (as requested)
                var hangfireJobId = $"{plugin.Id}.{recurringJob.Name}";

                // Create the job expression for async method using the Hangfire wrapper
                Expression<Func<HangfireBackgroundJobWrapper, Task>> jobExpression = wrapper => 
                    wrapper.ExecuteRecurringJobAsync(plugin.Id, recurringJob.Name, recurringJob.Parameters ?? new Dictionary<string, object?>());

                // Register the recurring job with Hangfire using the non-obsolete overload
                _recurringJobManager.AddOrUpdate(
                    recurringJobId: hangfireJobId,
                    methodCall: jobExpression,
                    cronExpression: recurringJob.CronSchedule,
                    options: new RecurringJobOptions
                    {
                        TimeZone = TimeZoneInfo.Local
                    }
                );

                jobsRegistered++;

                _logger.LogInformation("Registered recurring job '{JobId}' with schedule '{CronSchedule}' for ProductBundle '{ProductBundleId}'", 
                    hangfireJobId, recurringJob.CronSchedule, plugin.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to register recurring job '{JobName}' for ProductBundle '{ProductBundleId}'", 
                    recurringJob.Name, plugin.Id);
            }
        }

        return jobsRegistered;
    }

    /// <summary>
    /// Removes all recurring jobs for a specific ProductBundle
    /// </summary>
    /// <param name="productBundleId">The ProductBundle ID</param>
    public void RemovePluginRecurringJobs(string productBundleId)
    {
        _logger.LogInformation("Removing recurring jobs for ProductBundle '{ProductBundleId}'", productBundleId);

        try
        {
            var plugin = _pluginLoader.GetPluginById(productBundleId);
            if (plugin?.RecurringBackgroundJobs != null)
            {
                foreach (var recurringJob in plugin.RecurringBackgroundJobs)
                {
                    var hangfireJobId = $"{productBundleId}.{recurringJob.Name}";
                    _recurringJobManager.RemoveIfExists(hangfireJobId);

                    _logger.LogInformation("Removed recurring job '{JobId}'", hangfireJobId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove recurring jobs for ProductBundle '{ProductBundleId}'", productBundleId);
            throw;
        }
    }
}