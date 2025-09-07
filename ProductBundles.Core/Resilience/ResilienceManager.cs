using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;
using ProductBundles.Sdk;
using System;
using System.Threading.Tasks;

namespace ProductBundles.Core.Resilience;

/// <summary>
/// Manages plugin resilience using Polly with timeout protection
/// </summary>
public class ResilienceManager
{
    private readonly ILogger<ResilienceManager> _logger;
    private readonly ResiliencePipeline _pipeline;

    public ResilienceManager(ILogger<ResilienceManager> logger, TimeSpan? timeout = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        var timeoutValue = timeout ?? TimeSpan.FromSeconds(30);
        
        _pipeline = new ResiliencePipelineBuilder()
            .AddTimeout(new TimeoutStrategyOptions
            {
                Timeout = timeoutValue,
                OnTimeout = args =>
                {
                    _logger.LogWarning("Plugin operation timed out after {TimeoutSeconds} seconds", 
                        timeoutValue.TotalSeconds);
                    return ValueTask.CompletedTask;
                }
            })
            .Build();
    }

    /// <summary>
    /// Executes a plugin's HandleEvent method with timeout protection
    /// </summary>
    /// <param name="plugin">The plugin to execute</param>
    /// <param name="eventName">The event name</param>
    /// <param name="bundleInstance">The bundle instance</param>
    /// <returns>The result or null if execution failed</returns>
    public async Task<ProductBundleInstance?> ExecuteHandleEventAsync(
        IAmAProductBundle plugin,
        string eventName,
        ProductBundleInstance bundleInstance)
    {
        if (plugin == null) throw new ArgumentNullException(nameof(plugin));
        if (string.IsNullOrEmpty(eventName)) throw new ArgumentException("Event name cannot be null or empty", nameof(eventName));
        if (bundleInstance == null) throw new ArgumentNullException(nameof(bundleInstance));

        try
        {
            _logger.LogDebug("Executing plugin '{PluginId}' HandleEvent for event '{EventName}'", 
                plugin.Id, eventName);

            var result = await _pipeline.ExecuteAsync(async cancellationToken =>
            {
                // Execute the plugin in a task with proper cancellation support
                var tcs = new TaskCompletionSource<ProductBundleInstance>();
                
                // Register cancellation to complete the task with cancellation
                using var registration = cancellationToken.Register(() => 
                    tcs.TrySetCanceled(cancellationToken));
                
                // Start the plugin execution on a background thread
                _ = Task.Run(() =>
                {
                    try
                    {
                        var pluginResult = plugin.HandleEvent(eventName, bundleInstance);
                        tcs.TrySetResult(pluginResult);
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                });
                
                return await tcs.Task;
            });

            _logger.LogDebug("Plugin '{PluginId}' HandleEvent completed successfully for event '{EventName}'", 
                plugin.Id, eventName);

            return result;
        }
        catch (TimeoutRejectedException)
        {
            _logger.LogError("Plugin '{PluginId}' HandleEvent timed out for event '{EventName}'", 
                plugin.Id, eventName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plugin '{PluginId}' HandleEvent failed for event '{EventName}': {ErrorMessage}", 
                plugin.Id, eventName, ex.Message);
            return null;
        }
    }
}
