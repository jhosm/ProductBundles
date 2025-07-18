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
        }

        public void Initialize()
        {
            Console.WriteLine($"[{FriendlyName}] Initializing...");
            // Perform initialization tasks here
        }

        public ProductBundleInstance Execute(string eventName, ProductBundleInstance bundleInstance)
        {
            Console.WriteLine($"[{FriendlyName}] Executing main functionality...");
            Console.WriteLine($"[{FriendlyName}] Event: {eventName}");
            Console.WriteLine($"[{FriendlyName}] Bundle Instance ID: {bundleInstance.Id}");
            Console.WriteLine($"[{FriendlyName}] Product Bundle ID: {bundleInstance.ProductBundleId}");
            Console.WriteLine($"[{FriendlyName}] Product Bundle Version: {bundleInstance.ProductBundleVersion}");
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
            
            // Return result as ProductBundleInstance
            var resultInstance = new ProductBundleInstance(
                id: Guid.NewGuid().ToString(),
                productBundleId: bundleInstance.ProductBundleId,
                productBundleVersion: bundleInstance.ProductBundleVersion
            );
            
            // Add result properties
            resultInstance.Properties["status"] = "success";
            resultInstance.Properties["message"] = "Sample plugin executed successfully";
            resultInstance.Properties["timestamp"] = DateTime.Now;
            resultInstance.Properties["processedProperties"] = bundleInstance.Properties.Count;
            resultInstance.Properties["originalInstanceId"] = bundleInstance.Id;
            resultInstance.Properties["eventName"] = eventName;
            
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
