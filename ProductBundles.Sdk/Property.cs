namespace ProductBundles.Sdk
{
    /// <summary>
    /// Represents a property of a product bundle
    /// </summary>
    public class Property
    {
        /// <summary>
        /// Gets or sets the name of the property
        /// </summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the description of the property
        /// </summary>
        public string Description { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the default value of the property
        /// </summary>
        public object? DefaultValue { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the Property class
        /// </summary>
        public Property() { }
        
        /// <summary>
        /// Initializes a new instance of the Property class with specified values
        /// </summary>
        /// <param name="name">The name of the property</param>
        /// <param name="description">The description of the property</param>
        /// <param name="value">The value of the property</param>
        public Property(string name, string description, object? value = null)
        {
            Name = name;
            Description = description;
            DefaultValue = value;
        }
    }
}
