using ProductBundles.Sdk;
using System;
using System.Collections.Generic;

namespace ProductBundles.SamplePlugin
{
    public class AnotherSamplePlugin : IAmAProductBundle
    {
        public string Id => "anothersample";
        public string FriendlyName => "Another Sample Plugin";
        public string Description => "Another sample plugin to demonstrate multiple plugins in one DLL";
        public string Version => "2.1.0";
        
        public IReadOnlyList<Property> Properties { get; }
        
        public IReadOnlyList<RecurringBackgroundJob> RecurringBackgroundJobs { get; }
        
        public AnotherSamplePlugin()
        {
            Properties = new List<Property>
            {
                new Property("MaxProcessingSteps", "Maximum number of processing steps to execute", 3),
                new Property("ProcessingDelay", "Delay between processing steps in milliseconds", 200),
                new Property("EnableVerboseLogging", "Whether to enable verbose logging output", true),
                new Property("Author", "The plugin author", "Sample Plugin Team"),
                new Property("License", "The plugin license", "MIT"),
                new Property("SupportedPlatforms", "Platforms this plugin supports", "Windows, Linux, macOS"),
                new Property("LastUpdated", "When the plugin was last updated", DateTime.Now.ToString("yyyy-MM-dd"))
            };
            
            RecurringBackgroundJobs = new List<RecurringBackgroundJob>
            {
                new RecurringBackgroundJob(
                    "DataProcessing", 
                    "0 */3 * * *", 
                    "Processes accumulated data every 3 hours",
                    new Dictionary<string, object?> { { "eventName", "data.process" }, { "batchSize", 100 } }
                ),
                new RecurringBackgroundJob(
                    "SystemCleanup", 
                    "30 4 * * *", 
                    "Performs system cleanup daily at 4:30 AM",
                    new Dictionary<string, object?> { { "eventName", "system.cleanup" }, { "cleanTempFiles", true } }
                ),
                new RecurringBackgroundJob(
                    "MonthlyArchive", 
                    "0 1 1 * *", 
                    "Archives old data on the first day of each month",
                    new Dictionary<string, object?> { { "eventName", "data.archive" }, { "retentionDays", 90 } }
                ),
                new RecurringBackgroundJob(
                    "QuickStatusCheck", 
                    "*/15 * * * *", 
                    "Quick status check every 15 minutes during business hours",
                    new Dictionary<string, object?> { { "eventName", "status.check" }, { "lightweight", true } }
                )
            };
        }

        public void Initialize()
        {
            Console.WriteLine($"[{FriendlyName}] Starting initialization sequence...");
            Console.WriteLine($"[{FriendlyName}] Checking system requirements...");
            Console.WriteLine($"[{FriendlyName}] Initialization complete!");
        }

        public ProductBundleInstance HandleEvent(string eventName, ProductBundleInstance bundleInstance)
        {
            Console.WriteLine($"[{FriendlyName}] Beginning execution phase...");
            Console.WriteLine($"[{FriendlyName}] Event triggered: {eventName}");
            Console.WriteLine($"[{FriendlyName}] Bundle Instance ID: {bundleInstance.Id}");
            Console.WriteLine($"[{FriendlyName}] Product Bundle ID: {bundleInstance.ProductBundleId}");
            Console.WriteLine($"[{FriendlyName}] Product Bundle Version: {bundleInstance.ProductBundleVersion}");
            Console.WriteLine($"[{FriendlyName}] Processing data with {bundleInstance.Properties.Count} properties...");
            
            var processedData = new List<string>();
            
            // Process each property
            foreach (var kvp in bundleInstance.Properties)
            {
                Console.WriteLine($"[{FriendlyName}] Processing property '{kvp.Key}' with value: {kvp.Value}");
                processedData.Add($"{kvp.Key}={kvp.Value}");
            }
            
            // Simulate some processing
            for (int i = 1; i <= 3; i++)
            {
                Console.WriteLine($"[{FriendlyName}] Processing step {i}/3...");
                System.Threading.Thread.Sleep(200);
            }
            
            Console.WriteLine($"[{FriendlyName}] All tasks completed successfully!");
            
            // Return comprehensive result as ProductBundleInstance
            var resultInstance = new ProductBundleInstance(
                id: Guid.NewGuid().ToString(),
                productBundleId: bundleInstance.ProductBundleId,
                productBundleVersion: bundleInstance.ProductBundleVersion
            );
            
            // Add result properties
            resultInstance.Properties["status"] = "completed";
            resultInstance.Properties["event"] = eventName;
            resultInstance.Properties["processingSteps"] = 3;
            resultInstance.Properties["processedData"] = processedData;
            resultInstance.Properties["executionTime"] = DateTime.Now;
            resultInstance.Properties["success"] = true;
            resultInstance.Properties["originalInstanceId"] = bundleInstance.Id;
            
            return resultInstance;
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
            
            // Perform version-specific upgrade logic
            if (bundleInstance.ProductBundleVersion != Version)
            {
                Console.WriteLine($"[{FriendlyName}] Performing version-specific upgrade tasks...");
                
                // Example: Update LastUpdated property to current date
                upgradedInstance.Properties["LastUpdated"] = DateTime.Now.ToString("yyyy-MM-dd");
                
                // Example: Ensure MaxProcessingSteps is at least 3 for newer versions
                if (upgradedInstance.Properties.ContainsKey("MaxProcessingSteps"))
                {
                    var currentValue = Convert.ToInt32(upgradedInstance.Properties["MaxProcessingSteps"]);
                    if (currentValue < 3)
                    {
                        upgradedInstance.Properties["MaxProcessingSteps"] = 3;
                        Console.WriteLine($"[{FriendlyName}] Updated MaxProcessingSteps from {currentValue} to 3");
                    }
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
            Console.WriteLine($"[{FriendlyName}] Cleaning up temporary files...");
            Console.WriteLine($"[{FriendlyName}] Closing connections...");
            Console.WriteLine($"[{FriendlyName}] Disposal complete!");
        }
    }
}
