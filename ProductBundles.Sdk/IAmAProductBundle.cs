namespace ProductBundles.Sdk
{
    /// <summary>
    /// Interface that identifies a class as a product bundle plugin
    /// </summary>
    public interface IAmAProductBundle
    {
        /// <summary>
        /// Gets the bundle identifier
        /// </summary>
        string Id { get; }
        
        /// <summary>
        /// Gets the friendly name of the bundle
        /// </summary>
        string FriendlyName { get; }
        
        /// <summary>
        /// Gets the description of the bundle
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// Gets the version of the bundle
        /// </summary>
        string Version { get; }
        
        /// <summary>
        /// Gets the list of properties associated with the bundle
        /// </summary>
        IReadOnlyList<Property> Properties { get; }
        
        /// <summary>
        /// Initializes the bundle
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// Executes the bundle's main functionality
        /// </summary>
        /// <param name="eventName">The event that explains why the method was called</param>
        /// <param name="bundleInstance">The product bundle instance containing configuration and property values</param>
        /// <returns>A ProductBundleInstance containing the results of the execution</returns>
        ProductBundleInstance Execute(string eventName, ProductBundleInstance bundleInstance);
        
        /// <summary>
        /// Upgrades an existing ProductBundleInstance to the current ProductBundle version
        /// </summary>
        /// <param name="bundleInstance">The ProductBundleInstance to upgrade</param>
        /// <returns>A new ProductBundleInstance upgraded to the current ProductBundle version</returns>
        ProductBundleInstance UpgradeProductBundleInstance(ProductBundleInstance bundleInstance);
        
        /// <summary>
        /// Disposes resources used by the bundle
        /// </summary>
        void Dispose();
    }
}
