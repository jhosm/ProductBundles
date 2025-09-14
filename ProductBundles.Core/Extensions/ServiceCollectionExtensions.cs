using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProductBundles.Core.Resilience;
using ProductBundles.Core.Serialization;
using ProductBundles.Core.Storage;
using ProductBundles.Core.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Text.Json;
using MongoDB.Driver;

namespace ProductBundles.Core.Extensions
{
    /// <summary>
    /// Extension methods for configuring ProductBundle services in the DI container.
    /// Each method registers a specific service type for better composability.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        #region Serialization Services

        /// <summary>
        /// Adds JSON serialization services for ProductBundle instances
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configureOptions">Optional action to configure JSON serialization options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleJsonSerialization(
            this IServiceCollection services, 
            Action<JsonSerializerOptions>? configureOptions = null)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            configureOptions?.Invoke(options);
            services.AddSingleton(options);
            services.TryAddSingleton<IProductBundleInstanceSerializer, JsonProductBundleInstanceSerializer>();

            return services;
        }

        /// <summary>
        /// Adds custom serialization services for ProductBundle instances
        /// </summary>
        /// <typeparam name="TSerializer">The serializer implementation type</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleCustomSerialization<TSerializer>(
            this IServiceCollection services)
            where TSerializer : class, IProductBundleInstanceSerializer
        {
            services.TryAddSingleton<IProductBundleInstanceSerializer, TSerializer>();
            return services;
        }

        #endregion

        #region Storage Services

        /// <summary>
        /// Adds storage services for ProductBundle instances based on configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configuration">The configuration instance</param>
        /// <param name="sectionName">The configuration section name (defaults to "ProductBundleStorage")</param>
        /// <param name="logger">Optional logger for configuration process (if null, configuration logging is skipped)</param>
        /// <returns>The service collection for chaining</returns>
        /// <exception cref="InvalidOperationException">Thrown when storage configuration is invalid</exception>
        public static IServiceCollection AddProductBundleStorageFromConfiguration(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "ProductBundleStorage")
        {
            var storageConfig = configuration.GetSection(sectionName).Get<StorageConfiguration>();
            
            if (storageConfig == null)
            {
                throw new InvalidOperationException($"Storage configuration section '{sectionName}' not found or is empty. Please configure storage in appsettings.json");
            }

            // Validate the configuration
            var validationResult = storageConfig.Validate();
            if (!validationResult.IsValid)
            {
                throw new InvalidOperationException(validationResult.GetFormattedErrors());
            }

            // Register storage based on provider type
            switch (storageConfig.Provider.ToLowerInvariant())
            {
                case "filesystem":
                    var fsDirectory = storageConfig.FileSystem!.StorageDirectory;
                    services.AddProductBundleFileSystemStorage(fsDirectory);
                    break;

                case "mongodb":
                    var mongoConfig = storageConfig.MongoDB!;
                    services.AddProductBundleMongoStorage(
                        mongoConfig.ConnectionString, 
                        mongoConfig.DatabaseName, 
                        mongoConfig.CollectionName);
                    break;

                case "sqlserver":
                    var sqlConnectionString = storageConfig.SqlServer!.ConnectionString;
                    services.AddProductBundleSqlServerStorage(sqlConnectionString);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported storage provider: {storageConfig.Provider}");
            }
            return services;
        }

        /// <summary>
        /// Adds file system storage for ProductBundle instances
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="storageDirectory">The directory to store instance files</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleFileSystemStorage(
            this IServiceCollection services, 
            string storageDirectory)
        {
            if (string.IsNullOrWhiteSpace(storageDirectory))
                throw new ArgumentException("Storage directory cannot be null or empty", nameof(storageDirectory));

            services.AddSingleton<ProductBundleInstanceStorageOptions>(new ProductBundleInstanceStorageOptions
            {
                StorageDirectory = storageDirectory
            });

            services.TryAddSingleton<IProductBundleInstanceStorage>(provider =>
            {
                var options = provider.GetRequiredService<ProductBundleInstanceStorageOptions>();
                var serializer = provider.GetRequiredService<IProductBundleInstanceSerializer>();
                var logger = provider.GetService<ILogger<FileSystemProductBundleInstanceStorage>>();
                logger?.LogInformation("Configuring FileSystem storage with directory: '{StorageDirectory}'", storageDirectory);                
                return new FileSystemProductBundleInstanceStorage(options.StorageDirectory, serializer, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds file system storage for ProductBundle instances with configuration
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configure">Action to configure storage options</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleFileSystemStorage(
            this IServiceCollection services, 
            Action<ProductBundleInstanceStorageOptions> configure)
        {
            if (configure == null)
                throw new ArgumentNullException(nameof(configure));

            var options = new ProductBundleInstanceStorageOptions();
            configure(options);

            if (string.IsNullOrWhiteSpace(options.StorageDirectory))
                throw new ArgumentException("Storage directory must be specified in options", nameof(configure));

            return services.AddProductBundleFileSystemStorage(options.StorageDirectory);
        }

        /// <summary>
        /// Adds MongoDB storage for ProductBundle instances
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">The MongoDB connection string</param>
        /// <param name="databaseName">The MongoDB database name</param>
        /// <param name="collectionName">The MongoDB collection name (optional, defaults to "ProductBundleInstances")</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleMongoStorage(
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

            // Register MongoDB infrastructure services
            services.AddMongoClient(connectionString);
            services.AddMongoDatabase(databaseName);
            services.AddMongoCollection(collectionName);

            // Register the storage implementation
            services.TryAddSingleton<IProductBundleInstanceStorage>(provider =>
            {
                var logger = provider.GetService<ILogger<MongoProductBundleInstanceStorage>>();
                logger?.LogInformation("Configuring MongoDB storage with Database: '{DatabaseName}', Collection: '{CollectionName}'", 
                    databaseName, collectionName);

                return new MongoProductBundleInstanceStorage(connectionString, databaseName, collectionName, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds SQL Server storage for ProductBundle instances
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">The SQL Server connection string</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleSqlServerStorage(
            this IServiceCollection services,
            string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            services.TryAddSingleton<IProductBundleInstanceStorage>(provider =>
            {
                var logger = provider.GetService<ILogger<SqlServerVersionedProductBundleInstanceStorage>>();
                var safeConnectionString = connectionString.Contains("Password") 
                    ? connectionString.Substring(0, Math.Min(15, connectionString.Length)) + "..." 
                    : connectionString;
                logger?.LogInformation("Configuring SQL Server storage with connection: '{ConnectionString}'", safeConnectionString);
                return new SqlServerVersionedProductBundleInstanceStorage(connectionString, logger);
            });

            return services;
        }

        /// <summary>
        /// Adds custom storage implementation for ProductBundle instances
        /// </summary>
        /// <typeparam name="TStorage">The storage implementation type</typeparam>
        /// <param name="services">The service collection</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddProductBundleCustomStorage<TStorage>(
            this IServiceCollection services)
            where TStorage : class, IProductBundleInstanceStorage
        {
            services.TryAddSingleton<IProductBundleInstanceStorage, TStorage>();
            return services;
        }


        #endregion

        #region MongoDB Infrastructure Services

        /// <summary>
        /// Adds MongoDB client services to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="connectionString">The MongoDB connection string</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMongoClient(
            this IServiceCollection services,
            string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            services.TryAddSingleton<IMongoClient>(provider => new MongoClient(connectionString));
            return services;
        }

        /// <summary>
        /// Adds MongoDB database services to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="databaseName">The MongoDB database name</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMongoDatabase(
            this IServiceCollection services,
            string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));

            services.TryAddSingleton<IMongoDatabase>(provider =>
            {
                var client = provider.GetRequiredService<IMongoClient>();
                return client.GetDatabase(databaseName);
            });

            return services;
        }

        /// <summary>
        /// Adds MongoDB collection services to the DI container
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="collectionName">The MongoDB collection name</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddMongoCollection(
            this IServiceCollection services,
            string collectionName)
        {
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

            services.TryAddSingleton<IMongoCollection<ProductBundles.Sdk.ProductBundleInstance>>(provider =>
            {
                var database = provider.GetRequiredService<IMongoDatabase>();
                return database.GetCollection<ProductBundles.Sdk.ProductBundleInstance>(collectionName);
            });

            return services;
        }

        #endregion

        #region Resilience Services

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

        #endregion

    }
}
