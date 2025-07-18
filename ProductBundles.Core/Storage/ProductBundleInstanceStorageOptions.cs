namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// Configuration options for ProductBundle instance storage
    /// </summary>
    public class ProductBundleInstanceStorageOptions
    {
        /// <summary>
        /// Gets or sets the directory where instance files are stored
        /// </summary>
        public string StorageDirectory { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum number of concurrent operations allowed
        /// </summary>
        public int MaxConcurrentOperations { get; set; } = 10;

        /// <summary>
        /// Gets or sets whether to create the storage directory if it doesn't exist
        /// </summary>
        public bool CreateDirectoryIfNotExists { get; set; } = true;
    }
}
