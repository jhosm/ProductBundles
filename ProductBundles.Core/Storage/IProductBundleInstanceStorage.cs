using ProductBundles.Sdk;

namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// Interface for CRUD operations on ProductBundleInstance objects
    /// </summary>
    public interface IProductBundleInstanceStorage
    {
        /// <summary>
        /// Creates a new ProductBundleInstance in storage
        /// </summary>
        /// <param name="instance">The ProductBundleInstance to create</param>
        /// <returns>True if the instance was created successfully, false if it already exists</returns>
        Task<bool> CreateAsync(ProductBundleInstance instance);
        
        /// <summary>
        /// Retrieves a ProductBundleInstance by its ID
        /// </summary>
        /// <param name="id">The ID of the instance to retrieve</param>
        /// <returns>The ProductBundleInstance if found, null otherwise</returns>
        Task<ProductBundleInstance?> GetAsync(string id);
        
        /// <summary>
        /// Retrieves all ProductBundleInstance objects in storage
        /// </summary>
        /// <returns>Collection of all ProductBundleInstance objects</returns>
        Task<IEnumerable<ProductBundleInstance>> GetAllAsync();
        
        /// <summary>
        /// Retrieves all ProductBundleInstance objects for a specific ProductBundle
        /// </summary>
        /// <param name="productBundleId">The ProductBundle ID to filter by</param>
        /// <returns>Collection of ProductBundleInstance objects for the specified ProductBundle</returns>
        Task<IEnumerable<ProductBundleInstance>> GetByProductBundleIdAsync(string productBundleId);
        
        /// <summary>
        /// Updates an existing ProductBundleInstance in storage
        /// </summary>
        /// <param name="instance">The ProductBundleInstance to update</param>
        /// <returns>True if the instance was updated successfully, false if it didn't exist</returns>
        Task<bool> UpdateAsync(ProductBundleInstance instance);
        
        /// <summary>
        /// Deletes a ProductBundleInstance from storage
        /// </summary>
        /// <param name="id">The ID of the instance to delete</param>
        /// <returns>True if the instance was deleted, false if it didn't exist</returns>
        Task<bool> DeleteAsync(string id);
        
        /// <summary>
        /// Checks if a ProductBundleInstance with the specified ID exists in storage
        /// </summary>
        /// <param name="id">The ID to check</param>
        /// <returns>True if the instance exists, false otherwise</returns>
        Task<bool> ExistsAsync(string id);
        
        /// <summary>
        /// Gets the count of ProductBundleInstance objects in storage
        /// </summary>
        /// <returns>The total count of instances</returns>
        Task<int> GetCountAsync();
        
        /// <summary>
        /// Gets the count of ProductBundleInstance objects for a specific ProductBundle
        /// </summary>
        /// <param name="productBundleId">The ProductBundle ID to count</param>
        /// <returns>The count of instances for the specified ProductBundle</returns>
        Task<int> GetCountByProductBundleIdAsync(string productBundleId);
    }
}
