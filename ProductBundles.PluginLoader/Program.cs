using ProductBundles.Core.Serialization;  
using ProductBundles.Core.Storage;
using ProductBundles.Core.Extensions;
using ProductBundles.Sdk;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace ProductBundles.PluginLoader
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Product Bundles Plugin Loader ===");
            Console.WriteLine();

            // Delete existing storage directory
            var storageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "storage");
            if (Directory.Exists(storageDirectory))
            {
                Directory.Delete(storageDirectory, true);
            }

            // Set up dependency injection container
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder => builder
                    .AddConsole()
                    .SetMinimumLevel(LogLevel.Information))
                .AddProductBundleInstanceServices(
                    Path.Combine(Directory.GetCurrentDirectory(), "storage"),
                    jsonOptions =>
                    {
                        jsonOptions.WriteIndented = true;
                        jsonOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    })
                .BuildServiceProvider();

            var pluginLoaderLogger = serviceProvider.GetRequiredService<ILogger<ProductBundles.Core.ProductBundlesLoader>>();
            // Create plugin loader instance with logger
            var pluginLoader = new ProductBundles.Core.ProductBundlesLoader("plugins", pluginLoaderLogger);
            
            try
            {
                // Load all plugins from the plugins directory
                var plugins = pluginLoader.LoadPlugins();
                Console.WriteLine();

                if (plugins.Count() == 0)
                {
                    Console.WriteLine("No plugins found. Place your plugin DLLs in the 'plugins' folder.");
                    Console.WriteLine("Press any key to exit...");
                    Console.ReadKey();
                    return;
                }

                // Display loaded plugins
                Console.WriteLine("=== Loaded Plugins ===");
                foreach (var plugin in plugins)
                {
                    Console.WriteLine($"Plugin: {plugin.FriendlyName}");
                    Console.WriteLine($"  ID: {plugin.Id}");
                    Console.WriteLine($"  Description: {plugin.Description}");
                    Console.WriteLine($"  Version: {plugin.Version}");
                    Console.WriteLine();
                }

                Console.WriteLine("=== Plugin execution completed ===");
                Console.WriteLine();

                // Demonstrate ProductBundleInstance serialization
                Console.WriteLine("=== ProductBundleInstance Serialization Demo ===");
                DemonstrateSerializationFeatures(serviceProvider);
                
                // Demonstrate storage features
                Console.WriteLine("=== ProductBundleInstance Storage Demo ===");
                await DemonstrateStorageFeatures(serviceProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                serviceProvider.Dispose();
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Demonstrates the serialization features of ProductBundleInstance
        /// </summary>
        private static void DemonstrateSerializationFeatures(IServiceProvider serviceProvider)
        {
            try
            {
                // Get serializer from DI container
                var serializer = serviceProvider.GetRequiredService<IProductBundleInstanceSerializer>();

                Console.WriteLine($"Using serializer: {serializer.FormatName}");
                Console.WriteLine($"File extension: {serializer.FileExtension}");
                Console.WriteLine();

                // Create a sample ProductBundleInstance with various data types
                var sampleInstance = new ProductBundleInstance("demo-123", "sampleplug", "1.0.0");
                
                // Add various property types to demonstrate serialization
                sampleInstance.Properties["stringValue"] = "Hello, World!";
                sampleInstance.Properties["intValue"] = 42;
                sampleInstance.Properties["doubleValue"] = 3.14159;
                sampleInstance.Properties["boolValue"] = true;
                sampleInstance.Properties["dateValue"] = DateTime.Now;
                sampleInstance.Properties["arrayValue"] = new[] { "apple", "banana", "cherry" };
                sampleInstance.Properties["nullValue"] = null;
                
                // Nested object
                sampleInstance.Properties["nestedObject"] = new Dictionary<string, object?>
                {
                    ["nested1"] = "value1",
                    ["nested2"] = 123,
                    ["nested3"] = true
                };

                Console.WriteLine("Original instance:");
                Console.WriteLine($"  ID: {sampleInstance.Id}");
                Console.WriteLine($"  ProductBundleId: {sampleInstance.ProductBundleId}");
                Console.WriteLine($"  ProductBundleVersion: {sampleInstance.ProductBundleVersion}");
                Console.WriteLine($"  Properties count: {sampleInstance.Properties.Count}");
                Console.WriteLine();

                // Serialize the instance
                Console.WriteLine("Serializing instance...");
                var serializedData = serializer.Serialize(sampleInstance);
                Console.WriteLine("✓ Serialization successful");
                Console.WriteLine($"Serialized data length: {serializedData.Length} characters");
                Console.WriteLine();

                // Show the serialized data (truncated for readability)
                Console.WriteLine("Serialized data (first 500 characters):");
                Console.WriteLine(serializedData.Length > 500 ? serializedData.Substring(0, 500) + "..." : serializedData);
                Console.WriteLine();

                // Deserialize the instance
                Console.WriteLine("Deserializing instance...");
                var deserializedInstance = serializer.Deserialize(serializedData);
                Console.WriteLine("✓ Deserialization successful");
                Console.WriteLine();

                // Verify the deserialized instance
                Console.WriteLine("Deserialized instance:");
                Console.WriteLine($"  ID: {deserializedInstance.Id}");
                Console.WriteLine($"  ProductBundleId: {deserializedInstance.ProductBundleId}");
                Console.WriteLine($"  ProductBundleVersion: {deserializedInstance.ProductBundleVersion}");
                Console.WriteLine($"  Properties count: {deserializedInstance.Properties.Count}");
                Console.WriteLine();

                // Verify round-trip integrity
                Console.WriteLine("Verifying round-trip integrity...");
                var integrityCheck = VerifyRoundTripIntegrity(sampleInstance, deserializedInstance);
                Console.WriteLine($"✓ Round-trip integrity: {(integrityCheck ? "PASSED" : "FAILED")}");
                Console.WriteLine();

                // Test TryDeserialize with invalid data
                Console.WriteLine("Testing TryDeserialize with invalid data...");
                var invalidData = "{ invalid json }";
                var success = serializer.TryDeserialize(invalidData, out var failedInstance);
                Console.WriteLine($"✓ TryDeserialize with invalid data: {(!success ? "PASSED" : "FAILED")} (expected to fail)");
                Console.WriteLine();

                Console.WriteLine("=== Serialization demonstration completed ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Serialization demo error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Demonstrates the storage features of ProductBundleInstance
        /// </summary>
        private static async Task DemonstrateStorageFeatures(IServiceProvider serviceProvider)
        {
            try
            {
                // Get storage from DI container
                var storage = serviceProvider.GetRequiredService<IProductBundleInstanceStorage>();

                Console.WriteLine("Using dependency injection for storage and serialization");
                Console.WriteLine();

                // Create some sample instances
                var instance1 = new ProductBundleInstance("sample-001", "sampleplug", "1.0.0");
                instance1.Properties["name"] = "Sample Instance 1";
                instance1.Properties["created"] = DateTime.Now;
                instance1.Properties["active"] = true;

                var instance2 = new ProductBundleInstance("sample-002", "sampleplug", "1.0.0");
                instance2.Properties["name"] = "Sample Instance 2";
                instance2.Properties["created"] = DateTime.Now.AddHours(-1);
                instance2.Properties["active"] = false;

                var instance3 = new ProductBundleInstance("another-001", "anotherplugin", "1.2.0");
                instance3.Properties["name"] = "Another Plugin Instance";
                instance3.Properties["created"] = DateTime.Now.AddDays(-1);
                instance3.Properties["version"] = "1.2.0";

                // Create instances
                Console.WriteLine("Creating instances...");
                await storage.CreateAsync(instance1);
                await storage.CreateAsync(instance2);
                await storage.CreateAsync(instance3);
                Console.WriteLine("✓ Created 3 instances");
                Console.WriteLine();

                // Get count
                var totalCount = await storage.GetCountAsync();
                Console.WriteLine($"Total instances in storage: {totalCount}");
                Console.WriteLine();

                // Retrieve single instance
                Console.WriteLine("Retrieving single instance...");
                var retrievedInstance = await storage.GetAsync("sample-001");
                if (retrievedInstance != null)
                {
                    Console.WriteLine($"✓ Retrieved instance: {retrievedInstance.Id}");
                    Console.WriteLine($"  Name: {retrievedInstance.Properties.GetValueOrDefault("name", "N/A")}");
                    Console.WriteLine($"  Active: {retrievedInstance.Properties.GetValueOrDefault("active", false)}");
                }
                Console.WriteLine();

                // Retrieve by ProductBundle ID
                Console.WriteLine("Retrieving instances by ProductBundle ID...");
                var paginationRequest = new PaginationRequest(pageNumber: 1, pageSize: 1000);
                var paginatedResult = await storage.GetByProductBundleIdAsync("sampleplug", paginationRequest);
                var samplePluginInstances = paginatedResult.Items;
                Console.WriteLine($"Found {samplePluginInstances.Count()} instances for 'sampleplug':");
                foreach (var instance in samplePluginInstances)
                {
                    Console.WriteLine($"  {instance.Id} - {instance.Properties.GetValueOrDefault("name", "N/A")}");
                }
                Console.WriteLine();

                // Update an instance
                Console.WriteLine("Updating instance...");
                instance1.Properties["name"] = "Updated Sample Instance 1";
                instance1.Properties["updated"] = DateTime.Now;
                await storage.UpdateAsync(instance1);
                Console.WriteLine("✓ Updated instance sample-001");
                Console.WriteLine();

                // Verify update
                var updatedInstance = await storage.GetAsync("sample-001");
                if (updatedInstance != null)
                {
                    Console.WriteLine($"Verified update: {updatedInstance.Properties.GetValueOrDefault("name", "N/A")}");
                }
                Console.WriteLine();

                // Check existence
                Console.WriteLine("Checking existence...");
                var exists = await storage.ExistsAsync("sample-001");
                Console.WriteLine($"Instance 'sample-001' exists: {exists}");
                var notExists = await storage.ExistsAsync("non-existent");
                Console.WriteLine($"Instance 'non-existent' exists: {notExists}");
                Console.WriteLine();

                // Delete an instance
                Console.WriteLine("Deleting instance...");
                var deleted = await storage.DeleteAsync("sample-002");
                Console.WriteLine($"✓ Deleted instance: {deleted}");
                Console.WriteLine();

                // Verify deletion
                var deletedInstance = await storage.GetAsync("sample-002");
                Console.WriteLine($"Verified deletion (should be null): {deletedInstance?.Id ?? "null"}");
                Console.WriteLine();

                // Final count
                var finalCount = await storage.GetCountAsync();
                Console.WriteLine($"Final count: {finalCount}");
                Console.WriteLine();

                Console.WriteLine("=== Storage demonstration completed ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Storage demo error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Verifies that the original and deserialized instances are equivalent
        /// </summary>
        /// <param name="original">The original ProductBundleInstance</param>
        /// <param name="deserialized">The deserialized ProductBundleInstance</param>
        /// <returns>True if the instances are equivalent, false otherwise</returns>
        private static bool VerifyRoundTripIntegrity(ProductBundleInstance original, ProductBundleInstance deserialized)
        {
            if (original.Id != deserialized.Id) return false;
            if (original.ProductBundleId != deserialized.ProductBundleId) return false;
            if (original.ProductBundleVersion != deserialized.ProductBundleVersion) return false;
            if (original.Properties.Count != deserialized.Properties.Count) return false;

            foreach (var kvp in original.Properties)
            {
                if (!deserialized.Properties.ContainsKey(kvp.Key)) return false;
                
                var originalValue = kvp.Value;
                var deserializedValue = deserialized.Properties[kvp.Key];
                
                // Handle null values
                if (originalValue == null && deserializedValue == null) continue;
                if (originalValue == null || deserializedValue == null) return false;
                
                // Handle different value types after JSON deserialization
                if (!CompareValues(originalValue, deserializedValue)) return false;
            }

            return true;
        }

        /// <summary>
        /// Compares two values considering JSON deserialization type conversions
        /// </summary>
        /// <param name="original">The original value</param>
        /// <param name="deserialized">The deserialized value</param>
        /// <returns>True if the values are equivalent, false otherwise</returns>
        private static bool CompareValues(object original, object deserialized)
        {
            // Handle primitive types
            if (original is string || original is bool || original is int || original is double || original is float)
            {
                return original.ToString() == deserialized.ToString();
            }

            // Handle DateTime (JSON converts to string)
            if (original is DateTime originalDate && deserialized is string deserializedDateString)
            {
                return DateTime.TryParse(deserializedDateString, out var parsedDate) && 
                       Math.Abs((originalDate - parsedDate).TotalSeconds) < 1;
            }

            // Handle arrays (JSON converts to object[])
            if (original is Array originalArray && deserialized is object[] deserializedArray)
            {
                if (originalArray.Length != deserializedArray.Length) return false;
                for (int i = 0; i < originalArray.Length; i++)
                {
                    if (!CompareValues(originalArray.GetValue(i)!, deserializedArray[i])) return false;
                }
                return true;
            }

            // Handle dictionaries (nested objects)
            if (original is Dictionary<string, object?> originalDict && 
                deserialized is Dictionary<string, object?> deserializedDict)
            {
                if (originalDict.Count != deserializedDict.Count) return false;
                foreach (var kvp in originalDict)
                {
                    if (!deserializedDict.ContainsKey(kvp.Key)) return false;
                    if (kvp.Value == null && deserializedDict[kvp.Key] == null) continue;
                    if (kvp.Value == null || deserializedDict[kvp.Key] == null) return false;
                    if (!CompareValues(kvp.Value, deserializedDict[kvp.Key]!)) return false;
                }
                return true;
            }

            // Fallback to string comparison for other types
            return original.ToString() == deserialized.ToString();
        }
    }
}
