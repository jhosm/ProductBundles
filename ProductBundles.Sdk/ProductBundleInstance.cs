namespace ProductBundles.Sdk
{
    /// <summary>
    /// Represents an instance of a product bundle with its configuration and state
    /// </summary>
    public class ProductBundleInstance
    {
        /// <summary>
        /// Gets or sets the unique identifier for this bundle instance
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the dictionary of property values for this instance
        /// Property name is the key, property value is the value
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
        
        /// <summary>
        /// Gets or sets the identifier of the ProductBundle this instance is attached to
        /// </summary>
        public string ProductBundleId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the version of the ProductBundle this instance is attached to
        /// </summary>
        public string ProductBundleVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Initializes a new instance of the ProductBundleInstance class
        /// </summary>
        public ProductBundleInstance() { }
        
        /// <summary>
        /// Initializes a new instance of the ProductBundleInstance class with specified values
        /// </summary>
        /// <param name="id">The unique identifier for this bundle instance</param>
        /// <param name="productBundleId">The identifier of the ProductBundle this instance is attached to</param>
        /// <param name="productBundleVersion">The version of the ProductBundle this instance is attached to</param>
        public ProductBundleInstance(string id, string productBundleId, string productBundleVersion)
        {
            Id = id;
            ProductBundleId = productBundleId;
            ProductBundleVersion = productBundleVersion;
        }
        
        /// <summary>
        /// Initializes a new instance of the ProductBundleInstance class with specified values and properties
        /// </summary>
        /// <param name="id">The unique identifier for this bundle instance</param>
        /// <param name="productBundleId">The identifier of the ProductBundle this instance is attached to</param>
        /// <param name="productBundleVersion">The version of the ProductBundle this instance is attached to</param>
        /// <param name="properties">The dictionary of property values for this instance</param>
        public ProductBundleInstance(string id, string productBundleId, string productBundleVersion, Dictionary<string, object?> properties)
        {
            Id = id;
            ProductBundleId = productBundleId;
            ProductBundleVersion = productBundleVersion;
            Properties = properties ?? new Dictionary<string, object?>();
        }
    }
}
