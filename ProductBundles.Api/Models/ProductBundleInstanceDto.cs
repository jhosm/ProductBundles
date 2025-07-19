namespace ProductBundles.Api.Models
{
    /// <summary>
    /// Data transfer object for ProductBundleInstance information
    /// </summary>
    public class ProductBundleInstanceDto
    {
        /// <summary>
        /// Gets or sets the unique identifier for this bundle instance
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the identifier of the ProductBundle this instance is attached to
        /// </summary>
        public string ProductBundleId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the version of the ProductBundle this instance is attached to
        /// </summary>
        public string ProductBundleVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the dictionary of property values for this instance
        /// Property name is the key, property value is the value
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    }
    
    /// <summary>
    /// Data transfer object for creating a new ProductBundleInstance
    /// </summary>
    public class CreateProductBundleInstanceDto
    {
        /// <summary>
        /// Gets or sets the identifier of the ProductBundle this instance is attached to
        /// </summary>
        public string ProductBundleId { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the version of the ProductBundle this instance is attached to
        /// </summary>
        public string ProductBundleVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the dictionary of property values for this instance
        /// Property name is the key, property value is the value
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    }
    
    /// <summary>
    /// Data transfer object for updating an existing ProductBundleInstance
    /// </summary>
    public class UpdateProductBundleInstanceDto
    {
        /// <summary>
        /// Gets or sets the version of the ProductBundle this instance is attached to
        /// </summary>
        public string ProductBundleVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the dictionary of property values for this instance
        /// Property name is the key, property value is the value
        /// </summary>
        public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();
    }
}
