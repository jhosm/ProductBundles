using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using ProductBundles.Sdk;

namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// MongoDB implementation of ProductBundleInstance storage
    /// </summary>
    public class MongoProductBundleInstanceStorage : IProductBundleInstanceStorage
    {
        private readonly IMongoCollection<ProductBundleInstance> _collection;
        private readonly ILogger<MongoProductBundleInstanceStorage> _logger;

        /// <summary>
        /// Initializes a new instance of the MongoProductBundleInstanceStorage class
        /// </summary>
        /// <param name="connectionString">MongoDB connection string</param>
        /// <param name="databaseName">Name of the MongoDB database</param>
        /// <param name="collectionName">Name of the MongoDB collection (defaults to "productBundleInstances")</param>
        /// <param name="logger">Optional logger for diagnostics</param>
        public MongoProductBundleInstanceStorage(
            string connectionString,
            string databaseName,
            string collectionName = "productBundleInstances",
            ILogger<MongoProductBundleInstanceStorage>? logger = null)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be null or empty", nameof(databaseName));
            if (string.IsNullOrWhiteSpace(collectionName))
                throw new ArgumentException("Collection name cannot be null or empty", nameof(collectionName));

            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<MongoProductBundleInstanceStorage>.Instance;

            try
            {
                var client = new MongoClient(connectionString);
                var database = client.GetDatabase(databaseName);
                _collection = database.GetCollection<ProductBundleInstance>(collectionName);

                _logger.LogInformation("MongoDB storage initialized successfully. Database: {DatabaseName}, Collection: {CollectionName}", 
                    databaseName, collectionName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize MongoDB storage");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> CreateAsync(ProductBundleInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (string.IsNullOrWhiteSpace(instance.Id))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(instance));

            try
            {
                _logger.LogDebug("Creating ProductBundleInstance with ID: {InstanceId}", instance.Id);

                // Check if instance already exists
                var existingInstance = await _collection.Find(x => x.Id == instance.Id).FirstOrDefaultAsync();
                if (existingInstance != null)
                {
                    _logger.LogWarning("ProductBundleInstance with ID {InstanceId} already exists", instance.Id);
                    return false;
                }

                // Insert the new instance
                await _collection.InsertOneAsync(instance);
                
                _logger.LogInformation("Successfully created ProductBundleInstance with ID: {InstanceId}", instance.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ProductBundleInstance with ID: {InstanceId}", instance.Id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(id));

            try
            {
                _logger.LogDebug("Deleting ProductBundleInstance with ID: {InstanceId}", id);

                var deleteResult = await _collection.DeleteOneAsync(x => x.Id == id);

                if (deleteResult.DeletedCount == 0)
                {
                    _logger.LogWarning("ProductBundleInstance with ID {InstanceId} not found for deletion", id);
                    return false;
                }

                _logger.LogInformation("Successfully deleted ProductBundleInstance with ID: {InstanceId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete ProductBundleInstance with ID: {InstanceId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(id));

            try
            {
                _logger.LogDebug("Checking existence of ProductBundleInstance with ID: {InstanceId}", id);

                var count = await _collection.CountDocumentsAsync(x => x.Id == id);
                var exists = count > 0;

                _logger.LogDebug("ProductBundleInstance with ID {InstanceId} exists: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check existence of ProductBundleInstance with ID: {InstanceId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<ProductBundleInstance?> GetAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(id));

            try
            {
                _logger.LogDebug("Retrieving ProductBundleInstance with ID: {InstanceId}", id);

                var instance = await _collection.Find(x => x.Id == id).FirstOrDefaultAsync();
                
                if (instance == null)
                {
                    _logger.LogDebug("ProductBundleInstance with ID {InstanceId} not found", id);
                    return null;
                }

                _logger.LogDebug("Successfully retrieved ProductBundleInstance with ID: {InstanceId}", id);
                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve ProductBundleInstance with ID: {InstanceId}", id);
                throw;
            }
        }



        /// <inheritdoc/>
        public async Task<PaginatedResult<ProductBundleInstance>> GetByProductBundleIdAsync(string productBundleId, PaginationRequest paginationRequest)
        {
            if (string.IsNullOrWhiteSpace(productBundleId))
                throw new ArgumentException("ProductBundle ID cannot be null or empty", nameof(productBundleId));
            
            if (paginationRequest == null)
                throw new ArgumentNullException(nameof(paginationRequest));

            try
            {
                _logger.LogDebug("Retrieving paginated ProductBundleInstance objects for ProductBundle ID: {ProductBundleId}, Page: {PageNumber}, PageSize: {PageSize}", 
                    productBundleId, paginationRequest.PageNumber, paginationRequest.PageSize);

                var filter = Builders<ProductBundleInstance>.Filter.Eq(x => x.ProductBundleId, productBundleId);
                
                // Get total count for pagination metadata
                var totalItems = (int)await _collection.CountDocumentsAsync(filter);
                
                // Get paginated results
                var instances = await _collection.Find(filter)
                    .Skip(paginationRequest.Skip)
                    .Limit(paginationRequest.PageSize)
                    .ToListAsync();

                var isLastPage = paginationRequest.Skip + instances.Count >= totalItems;
                
                _logger.LogDebug("Successfully retrieved page {PageNumber} with {ItemCount} items for ProductBundle ID: {ProductBundleId}", 
                    paginationRequest.PageNumber, 
                    instances.Count, 
                    productBundleId);
                
                return new PaginatedResult<ProductBundleInstance>(
                    instances, 
                    paginationRequest.PageNumber, 
                    paginationRequest.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve paginated ProductBundleInstance objects for ProductBundle ID: {ProductBundleId}", productBundleId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetCountAsync()
        {
            try
            {
                _logger.LogDebug("Getting total count of ProductBundleInstance objects");

                var count = await _collection.CountDocumentsAsync(_ => true);
                var result = (int)count;

                _logger.LogDebug("Total count of ProductBundleInstance objects: {Count}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total count of ProductBundleInstance objects");
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<int> GetCountByProductBundleIdAsync(string productBundleId)
        {
            if (string.IsNullOrWhiteSpace(productBundleId))
                throw new ArgumentException("ProductBundle ID cannot be null or empty", nameof(productBundleId));

            try
            {
                _logger.LogDebug("Getting count of ProductBundleInstance objects for ProductBundle ID: {ProductBundleId}", productBundleId);

                var count = await _collection.CountDocumentsAsync(x => x.ProductBundleId == productBundleId);
                var result = (int)count;

                _logger.LogDebug("Count of ProductBundleInstance objects for ProductBundle ID {ProductBundleId}: {Count}", productBundleId, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get count of ProductBundleInstance objects for ProductBundle ID: {ProductBundleId}", productBundleId);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> UpdateAsync(ProductBundleInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (string.IsNullOrWhiteSpace(instance.Id))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(instance));

            try
            {
                _logger.LogDebug("Updating ProductBundleInstance with ID: {InstanceId}", instance.Id);

                var filter = Builders<ProductBundleInstance>.Filter.Eq(x => x.Id, instance.Id);
                var result = await _collection.ReplaceOneAsync(filter, instance);
                
                if (result.MatchedCount == 0)
                {
                    _logger.LogWarning("ProductBundleInstance with ID {InstanceId} not found for update", instance.Id);
                    return false;
                }

                _logger.LogInformation("Successfully updated ProductBundleInstance with ID: {InstanceId}", instance.Id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update ProductBundleInstance with ID: {InstanceId}", instance.Id);
                throw;
            }
        }
    }
}
