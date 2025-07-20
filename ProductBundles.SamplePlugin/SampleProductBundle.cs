using ProductBundles.Sdk;
using System;
using System.Collections.Generic;

namespace ProductBundles.SamplePlugin
{
    public class SampleProductBundle : IAmAProductBundle
    {
        public string Id => "sampleplug";
        public string FriendlyName => "Sample Product Bundle";
        public string Description => "A sample plugin demonstrating the IAmAProductBundle interface";
        public string Version => "1.0.0";
        
        public IReadOnlyList<Property> Properties { get; }
        
        public IReadOnlyList<RecurringBackgroundJob> RecurringBackgroundJobs { get; }
        
        public SampleProductBundle()
        {
            Properties = new List<Property>
            {
                new Property("ExecutionTimeout", "Timeout for execution in milliseconds", 5000),
                new Property("WorkSimulationDelay", "Delay to simulate work processing", 500),
                new Property("EnableDebugMode", "Whether debug mode is enabled", false),
                new Property("ConfigurationPath", "Path to configuration file", "./config/sample.json"),
                new Property("OutputDirectory", "Directory for output files", "./output"),
                new Property("MaxRetryAttempts", "Maximum number of retry attempts", 3),
                new Property("Priority", "Plugin execution priority", "Medium")
            };
            
            RecurringBackgroundJobs = new List<RecurringBackgroundJob>
            {
                new RecurringBackgroundJob(
                    "HealthCheck", 
                    "*/5 * * * *", 
                    "Performs a health check every 5 minutes",
                    new Dictionary<string, object?> { { "eventName", "health.check" }, { "timeout", 30 } }
                ),
                new RecurringBackgroundJob(
                    "DailyMaintenance", 
                    "0 2 * * *", 
                    "Runs daily maintenance tasks at 2 AM",
                    new Dictionary<string, object?> { { "eventName", "maintenance.daily" }, { "cleanupOldLogs", true } }
                ),
                new RecurringBackgroundJob(
                    "WeeklyReport", 
                    "0 9 * * 1", 
                    "Generates weekly reports every Monday at 9 AM",
                    new Dictionary<string, object?> { { "eventName", "reporting.weekly" }, { "includeMetrics", true } }
                )
            };
        }

        public void Initialize()
        {
            Console.WriteLine($"[{FriendlyName}] Initializing...");
            // Perform initialization tasks here
        }

        public ProductBundleInstance HandleEvent(string eventName, ProductBundleInstance bundleInstance)
        {
            // Handle event count tracking
            if (bundleInstance.Properties.ContainsKey("handleEventCount"))
            {
                var currentCount = Convert.ToInt32(bundleInstance.Properties["handleEventCount"]);
                bundleInstance.Properties["handleEventCount"] = currentCount + 1;
            }
            else
            {
                bundleInstance.Properties["handleEventCount"] = 1;
            }
            
            Console.WriteLine($"[{FriendlyName}] Executing main functionality...");
            Console.WriteLine($"[{FriendlyName}] Event: {eventName}");
            Console.WriteLine($"[{FriendlyName}] Bundle Instance ID: {bundleInstance.Id}");
            Console.WriteLine($"[{FriendlyName}] Product Bundle ID: {bundleInstance.ProductBundleId}");
            Console.WriteLine($"[{FriendlyName}] Product Bundle Version: {bundleInstance.ProductBundleVersion}");
            Console.WriteLine($"[{FriendlyName}] Handle Event Count: {bundleInstance.Properties["handleEventCount"]}");
            Console.WriteLine($"[{FriendlyName}] Received {bundleInstance.Properties.Count} property values");
            
            // Display received properties
            foreach (var kvp in bundleInstance.Properties)
            {
                Console.WriteLine($"[{FriendlyName}] Property '{kvp.Key}': {kvp.Value}");
            }
            
            Console.WriteLine($"[{FriendlyName}] This is where the plugin would do its work!");
            
            // Simulate some work
            System.Threading.Thread.Sleep(500);
            
            Console.WriteLine($"[{FriendlyName}] Execution completed successfully!");
            
            
            // Add result properties
            bundleInstance.Properties["status"] = "success";
            bundleInstance.Properties["message"] = "Sample plugin executed successfully";
            bundleInstance.Properties["timestamp"] = DateTime.Now;
            bundleInstance.Properties["eventName"] = eventName;
            
            return bundleInstance;
        }

        public ProductBundleInstance UpgradeProductBundleInstance(ProductBundleInstance bundleInstance)
        {
            Console.WriteLine($"[{FriendlyName}] Upgrading ProductBundleInstance...");
            Console.WriteLine($"[{FriendlyName}] Original Instance ID: {bundleInstance.Id}");
            Console.WriteLine($"[{FriendlyName}] Original Version: {bundleInstance.ProductBundleVersion}");
            Console.WriteLine($"[{FriendlyName}] Target Version: {Version}");
            
            // Create upgraded instance with current bundle version
            var upgradedInstance = new ProductBundleInstance(
                id: bundleInstance.Id, // Keep the same instance ID
                productBundleId: Id, // Use current bundle ID
                productBundleVersion: Version // Upgrade to current version
            );
            
            // Copy existing properties
            foreach (var kvp in bundleInstance.Properties)
            {
                upgradedInstance.Properties[kvp.Key] = kvp.Value;
            }
            
            // Add any new properties that might be missing in the old version
            foreach (var property in Properties)
            {
                if (!upgradedInstance.Properties.ContainsKey(property.Name))
                {
                    upgradedInstance.Properties[property.Name] = property.DefaultValue;
                    Console.WriteLine($"[{FriendlyName}] Added new property '{property.Name}' with default value: {property.DefaultValue}");
                }
            }
            
            // Add upgrade metadata
            upgradedInstance.Properties["_upgraded"] = true;
            upgradedInstance.Properties["_originalVersion"] = bundleInstance.ProductBundleVersion;
            upgradedInstance.Properties["_upgradeTimestamp"] = DateTime.Now;
            
            Console.WriteLine($"[{FriendlyName}] Upgrade completed successfully!");
            return upgradedInstance;
        }

        public void Dispose()
        {
            Console.WriteLine($"[{FriendlyName}] Disposing resources...");
            // Clean up resources here
        }
    }
}
