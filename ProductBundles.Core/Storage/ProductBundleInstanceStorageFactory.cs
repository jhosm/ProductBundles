using Microsoft.Extensions.Logging;
using ProductBundles.Core.Serialization;

namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// Factory class for creating ProductBundleInstance storage implementations
    /// </summary>
    public static class ProductBundleInstanceStorageFactory
    {
        /// <summary>
        /// Creates a file system storage implementation
        /// </summary>
        /// <param name="storageDirectory">The directory to store instance files</param>
        /// <param name="serializerFormat">The serialization format to use (default: "json")</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <returns>A configured file system storage instance</returns>
        public static IProductBundleInstanceStorage CreateFileSystemStorage(
            string storageDirectory,
            ProductBundleInstanceSerializerFactory.SerializationFormat serializerFormat = ProductBundleInstanceSerializerFactory.SerializationFormat.Json,
            ILogger<FileSystemProductBundleInstanceStorage>? logger = null)
        {
            if (string.IsNullOrWhiteSpace(storageDirectory))
                throw new ArgumentException("Storage directory cannot be null or empty", nameof(storageDirectory));

            var serializer = ProductBundleInstanceSerializerFactory.CreateSerializer(serializerFormat);
            return new FileSystemProductBundleInstanceStorage(storageDirectory, serializer, logger);
        }

        /// <summary>
        /// Creates a file system storage implementation with a custom serializer
        /// </summary>
        /// <param name="storageDirectory">The directory to store instance files</param>
        /// <param name="serializer">The serializer to use for saving/loading instances</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <returns>A configured file system storage instance</returns>
        public static IProductBundleInstanceStorage CreateFileSystemStorage(
            string storageDirectory,
            IProductBundleInstanceSerializer serializer,
            ILogger<FileSystemProductBundleInstanceStorage>? logger = null)
        {
            if (string.IsNullOrWhiteSpace(storageDirectory))
                throw new ArgumentException("Storage directory cannot be null or empty", nameof(storageDirectory));

            if (serializer == null)
                throw new ArgumentNullException(nameof(serializer));

            return new FileSystemProductBundleInstanceStorage(storageDirectory, serializer, logger);
        }

        /// <summary>
        /// Creates a file system storage implementation with default settings
        /// </summary>
        /// <param name="storageDirectory">The directory to store instance files</param>
        /// <returns>A configured file system storage instance with JSON serialization</returns>
        public static IProductBundleInstanceStorage CreateDefaultFileSystemStorage(string storageDirectory)
        {
            return CreateFileSystemStorage(storageDirectory, ProductBundleInstanceSerializerFactory.SerializationFormat.Json);
        }

        /// <summary>
        /// Creates a storage implementation based on a connection string or configuration
        /// </summary>
        /// <param name="connectionString">The connection string or configuration for the storage</param>
        /// <param name="storageType">The type of storage to create (e.g., "filesystem", "database", etc.)</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        /// <returns>A configured storage instance</returns>
        /// <exception cref="NotSupportedException">Thrown when the storage type is not supported</exception>
        public static IProductBundleInstanceStorage CreateStorage(
            string connectionString,
            string storageType,
            ILogger? logger = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            if (string.IsNullOrWhiteSpace(storageType))
                throw new ArgumentException("Storage type cannot be null or empty", nameof(storageType));

            return storageType.ToLowerInvariant() switch
            {
                "filesystem" or "file" => CreateFileSystemStorage(
                    connectionString, 
                    ProductBundleInstanceSerializerFactory.SerializationFormat.Json, 
                    logger as ILogger<FileSystemProductBundleInstanceStorage>),
                _ => throw new NotSupportedException($"Storage type '{storageType}' is not supported")
            };
        }
    }
}
