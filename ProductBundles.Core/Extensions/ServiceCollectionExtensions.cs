using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductBundles.Core.Resilience;
using ProductBundles.Core.Serialization;
using ProductBundles.Core.Storage;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using MongoDB.Driver;

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
            // Create and configure JSON serializer options immediately
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            // Call configuration action immediately during registration
            configureOptions?.Invoke(options);
            
            // Register the configured options as a singleton
            services.AddSingleton(options);

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
        /// Adds ProductBundle MongoDB storage services to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">The MongoDB connection string</param>
        /// <param name="databaseName">The MongoDB database name</param>
        /// <param name="collectionName">The MongoDB collection name (optional, defaults to "ProductBundleInstances")</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleInstanceMongoStorage(
            this IServiceCollection services,
            string connectionString,
            string databaseName,
            string? collectionName = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));

            collectionName ??= "ProductBundleInstances";

            // Register MongoDB client as singleton
            services.TryAddSingleton<IMongoClient>(provider =>
            {
                return new MongoClient(connectionString);
            });

            // Register MongoDB database as singleton
            services.TryAddSingleton<IMongoDatabase>(provider =>
            {
                var client = provider.GetRequiredService<IMongoClient>();
                return client.GetDatabase(databaseName);
            });

            // Register MongoDB collection as singleton
            services.TryAddSingleton<IMongoCollection<ProductBundles.Sdk.ProductBundleInstance>>(provider =>
            {
                var database = provider.GetRequiredService<IMongoDatabase>();
                return database.GetCollection<ProductBundles.Sdk.ProductBundleInstance>(collectionName);
            });

            // Register the storage implementation
            services.TryAddSingleton<IProductBundleInstanceStorage>(provider =>
            {
                var logger = provider.GetService<ILogger<MongoProductBundleInstanceStorage>>();
                
                return new MongoProductBundleInstanceStorage(connectionString, databaseName, collectionName, logger);
            });

            return services;
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

        /// <summary>
        /// Adds ProductBundle SQL Server storage services to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">The SQL Server connection string</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleInstanceSqlServerStorage(
            this IServiceCollection services,
            string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            // Register the storage implementation
            services.TryAddSingleton<IProductBundleInstanceStorage>(provider =>
            {
                var logger = provider.GetService<ILogger<SqlServerVersionedProductBundleInstanceStorage>>();
                return new SqlServerVersionedProductBundleInstanceStorage(connectionString, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds all ProductBundle services with SQL Server storage to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">The SQL Server connection string</param>
        /// <param name="configureJsonOptions">Optional action to configure JSON serialization options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleInstanceSqlServerServices(
            this IServiceCollection services,
            string connectionString,
            Action<JsonSerializerOptions>? configureJsonOptions = null)
        {
            services.AddProductBundleInstanceSerialization(configureJsonOptions);
            services.AddProductBundleInstanceSqlServerStorage(connectionString);
            
            return services;
        }

        /// <summary>
        /// Adds plugin resilience services to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="timeout">Optional timeout for plugin operations (defaults to 30 seconds)</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddPluginResilience(
            this IServiceCollection services,
            TimeSpan? timeout = null)
        {
            services.TryAddSingleton<ResilienceManager>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ResilienceManager>>();
                return new ResilienceManager(logger, timeout);
            });
            
            return services;
        }
    }
}
