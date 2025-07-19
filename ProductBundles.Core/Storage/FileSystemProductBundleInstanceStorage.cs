using Microsoft.Extensions.Logging;
using ProductBundles.Core.Serialization;
using ProductBundles.Sdk;
using System.Collections.Concurrent;

namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// File system implementation of ProductBundleInstance storage
    /// </summary>
    public class FileSystemProductBundleInstanceStorage : IProductBundleInstanceStorage
    {
        private readonly string _storageDirectory;
        private readonly IProductBundleInstanceSerializer _serializer;
        private readonly ILogger<FileSystemProductBundleInstanceStorage> _logger;
        private readonly SemaphoreSlim _semaphore;
        private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileLocks;

        /// <summary>
        /// Initializes a new instance of the FileSystemProductBundleInstanceStorage class
        /// </summary>
        /// <param name="storageDirectory">The directory to store instance files</param>
        /// <param name="serializer">The serializer to use for saving/loading instances</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        public FileSystemProductBundleInstanceStorage(
            string storageDirectory,
            IProductBundleInstanceSerializer serializer,
            ILogger<FileSystemProductBundleInstanceStorage>? logger = null)
        {
            _storageDirectory = storageDirectory ?? throw new ArgumentNullException(nameof(storageDirectory));
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSystemProductBundleInstanceStorage>.Instance;
            _semaphore = new SemaphoreSlim(1, 1);
            _fileLocks = new ConcurrentDictionary<string, SemaphoreSlim>();

            // Ensure storage directory exists
            if (!Directory.Exists(_storageDirectory))
            {
                Directory.CreateDirectory(_storageDirectory);
                _logger.LogInformation("Created storage directory: {StorageDirectory}", _storageDirectory);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CreateAsync(ProductBundleInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (string.IsNullOrWhiteSpace(instance.Id))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(instance));

            var filePath = GetFilePath(instance.Id);
            var fileLock = GetFileLock(instance.Id);

            await fileLock.WaitAsync();
            try
            {
                if (File.Exists(filePath))
                {
                    return false;
                }

                var serializedData = _serializer.Serialize(instance);
                await File.WriteAllTextAsync(filePath, serializedData);
                
                _logger.LogInformation("Created ProductBundleInstance {InstanceId} at {FilePath}", 
                    instance.Id, filePath);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<ProductBundleInstance?> GetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            var filePath = GetFilePath(id);
            var fileLock = GetFileLock(id);

            await fileLock.WaitAsync();
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("ProductBundleInstance {InstanceId} not found at {FilePath}", id, filePath);
                    return null;
                }

                var serializedData = await File.ReadAllTextAsync(filePath);
                if (_serializer.TryDeserialize(serializedData, out var instance))
                {
                    _logger.LogDebug("Retrieved ProductBundleInstance {InstanceId} from {FilePath}", id, filePath);
                    return instance;
                }
                else
                {
                    _logger.LogWarning("Failed to deserialize ProductBundleInstance {InstanceId} from {FilePath}", id, filePath);
                    return null;
                }
            }
            finally
            {
                fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductBundleInstance>> GetAllAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var instances = new List<ProductBundleInstance>();
                var files = Directory.GetFiles(_storageDirectory, $"*{_serializer.FileExtension}");
                
                _logger.LogDebug("Found {FileCount} instance files in {StorageDirectory}", files.Length, _storageDirectory);

                foreach (var filePath in files)
                {
                    try
                    {
                        var serializedData = await File.ReadAllTextAsync(filePath);
                        if (_serializer.TryDeserialize(serializedData, out var instance) && instance != null)
                        {
                            instances.Add(instance);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to deserialize instance from file: {FilePath}", filePath);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error reading instance file: {FilePath}", filePath);
                    }
                }

                _logger.LogInformation("Retrieved {InstanceCount} ProductBundleInstances from storage", instances.Count);
                return instances;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<ProductBundleInstance>> GetByProductBundleIdAsync(string productBundleId)
        {
            if (string.IsNullOrWhiteSpace(productBundleId))
                throw new ArgumentException("ProductBundle ID cannot be null or empty", nameof(productBundleId));

            var allInstances = await GetAllAsync();
            var filteredInstances = allInstances.Where(i => i.ProductBundleId == productBundleId).ToList();
            
            _logger.LogDebug("Found {InstanceCount} ProductBundleInstances for ProductBundle {ProductBundleId}", 
                filteredInstances.Count, productBundleId);
            
            return filteredInstances;
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateAsync(ProductBundleInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (string.IsNullOrWhiteSpace(instance.Id))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(instance));

            var filePath = GetFilePath(instance.Id);
            var fileLock = GetFileLock(instance.Id);

            await fileLock.WaitAsync();
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                var serializedData = _serializer.Serialize(instance);
                await File.WriteAllTextAsync(filePath, serializedData);
                
                _logger.LogInformation("Updated ProductBundleInstance {InstanceId} at {FilePath}", 
                    instance.Id, filePath);
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                fileLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            var filePath = GetFilePath(id);
            var fileLock = GetFileLock(id);

            await fileLock.WaitAsync();
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogDebug("ProductBundleInstance {InstanceId} not found for deletion at {FilePath}", id, filePath);
                    return false;
                }

                File.Delete(filePath);
                _logger.LogInformation("Deleted ProductBundleInstance {InstanceId} from {FilePath}", id, filePath);
                return true;
            }
            finally
            {
                fileLock.Release();
                // Clean up the file lock if no longer needed
                _fileLocks.TryRemove(id, out _);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            var filePath = GetFilePath(id);
            var exists = File.Exists(filePath);
            
            _logger.LogDebug("ProductBundleInstance {InstanceId} exists: {Exists}", id, exists);
            return await Task.FromResult(exists);
        }

        /// <inheritdoc/>
        public async Task<int> GetCountAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                var files = Directory.GetFiles(_storageDirectory, $"*{_serializer.FileExtension}");
                var count = files.Length;
                
                _logger.LogDebug("Total ProductBundleInstance count: {Count}", count);
                return count;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetCountByProductBundleIdAsync(string productBundleId)
        {
            if (string.IsNullOrWhiteSpace(productBundleId))
                throw new ArgumentException("ProductBundle ID cannot be null or empty", nameof(productBundleId));

            var instances = await GetByProductBundleIdAsync(productBundleId);
            var count = instances.Count();
            
            _logger.LogDebug("ProductBundleInstance count for ProductBundle {ProductBundleId}: {Count}", 
                productBundleId, count);
            
            return count;
        }

        private string GetFilePath(string id)
        {
            // Sanitize the ID to make it safe for file names
            var sanitizedId = string.Join("_", id.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_storageDirectory, $"{sanitizedId}{_serializer.FileExtension}");
        }

        private SemaphoreSlim GetFileLock(string id)
        {
            return _fileLocks.GetOrAdd(id, _ => new SemaphoreSlim(1, 1));
        }

        /// <summary>
        /// Disposes the storage instance and releases resources
        /// </summary>
        public void Dispose()
        {
            _semaphore?.Dispose();
            foreach (var fileLock in _fileLocks.Values)
            {
                fileLock.Dispose();
            }
            _fileLocks.Clear();
        }
    }
}
