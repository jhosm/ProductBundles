using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductBundles.Core.Serialization;
using ProductBundles.Core.Storage;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ProductBundles.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring ProductBundle services in the DI container
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds ProductBundle serialization services to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Optional action to configure JSON serialization options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleInstanceSerialization(
            this IServiceCollection services, 
            Action<JsonSerializerOptions>? configureOptions = null)
        {
            // Register JSON serializer options
            services.AddSingleton<JsonSerializerOptions>(provider =>
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                
                configureOptions?.Invoke(options);
                return options;
            });

            // Register the default serializer
            services.TryAddSingleton<IProductBundleInstanceSerializer, JsonProductBundleInstanceSerializer>();

            return services;
        }

        /// <summary>
        /// Adds ProductBundle file system storage services to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="storageDirectory">The directory to store instance files</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleInstanceFileSystemStorage(
            this IServiceCollection services, 
            string storageDirectory)
        {
            if (string.IsNullOrWhiteSpace(storageDirectory))
                throw new ArgumentException("Storage directory cannot be null or empty", nameof(storageDirectory));

            // Register storage directory as a singleton
            services.AddSingleton<ProductBundleInstanceStorageOptions>(new ProductBundleInstanceStorageOptions
            {
                StorageDirectory = storageDirectory
            });

            // Register the storage implementation
            services.TryAddSingleton<IProductBundleInstanceStorage>(provider =>
            {
                var options = provider.GetRequiredService<ProductBundleInstanceStorageOptions>();
                var serializer = provider.GetRequiredService<IProductBundleInstanceSerializer>();
                var logger = provider.GetService<ILogger<FileSystemProductBundleInstanceStorage>>();
                
                return new FileSystemProductBundleInstanceStorage(
                    options.StorageDirectory, 
                    serializer, 
                    logger);
            });

            return services;
        }

        /// <summary>
        /// Adds ProductBundle file system storage services to the DI container with configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configure">Action to configure storage options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleInstanceFileSystemStorage(
            this IServiceCollection services, 
            Action<ProductBundleInstanceStorageOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new ProductBundleInstanceStorageOptions();
            configure(options);

            if (string.IsNullOrWhiteSpace(options.StorageDirectory))
                throw new ArgumentException("Storage directory must be specified in options", nameof(configure));

            return services.AddProductBundleInstanceFileSystemStorage(options.StorageDirectory);
        }

        /// <summary>
        /// Adds all ProductBundle services (serialization and storage) to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="storageDirectory">The directory to store instance files</param>
        /// <param name="configureJsonOptions">Optional action to configure JSON serialization options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleInstanceServices(
            this IServiceCollection services,
            string storageDirectory,
            Action<JsonSerializerOptions>? configureJsonOptions = null)
        {
            services.AddProductBundleInstanceSerialization(configureJsonOptions);
            services.AddProductBundleInstanceFileSystemStorage(storageDirectory);
            
            return services;
        }

        /// <summary>
        /// Adds all ProductBundle services (serialization and storage) to the DI container with configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureStorage">Action to configure storage options</param>
        /// <param name="configureJsonOptions">Optional action to configure JSON serialization options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleInstanceServices(
            this IServiceCollection services,
            Action<ProductBundleInstanceStorageOptions> configureStorage,
            Action<JsonSerializerOptions>? configureJsonOptions = null)
        {
            services.AddProductBundleInstanceSerialization(configureJsonOptions);
            services.AddProductBundleInstanceFileSystemStorage(configureStorage);
            
            return services;
        }
    }
}
