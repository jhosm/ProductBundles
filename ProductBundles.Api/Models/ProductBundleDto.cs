using ProductBundles.Sdk;

namespace ProductBundles.Api.Models
{
    /// <summary>
    /// Data transfer object for ProductBundle information
    /// </summary>
    public class ProductBundleDto
    {
        /// <summary>
        /// Gets or sets the bundle identifier
        /// </summary>
        public string Id { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the friendly name of the bundle
        /// </summary>
        public string FriendlyName { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the description of the bundle
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the version of the bundle
        /// </summary>
        public string Version { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the list of properties associated with the bundle
        /// </summary>
        public IList<PropertyDto> Properties { get; set; } = new List<PropertyDto>();
    }
    
    /// <summary>
    /// Data transfer object for Property information
    /// </summary>
    public class PropertyDto
    {
        /// <summary>
        /// Gets or sets the property name
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the property description
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the default value of the property
        /// </summary>
        public object? DefaultValue { get; set; }
        
        /// <summary>
        /// Gets or sets whether the property is required
        /// </summary>
        public bool IsRequired { get; set; }
    }
}
