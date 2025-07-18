using ProductBundles.Sdk;

namespace ProductBundles.Core.Serialization
{
    /// <summary>
    /// Interface for serializing and deserializing ProductBundleInstance objects
    /// </summary>
    public interface IProductBundleInstanceSerializer
    {
        /// <summary>
        /// Serializes a ProductBundleInstance to a string representation
        /// </summary>
        /// <param name="instance">The ProductBundleInstance to serialize</param>
        /// <returns>String representation of the instance</returns>
        string Serialize(ProductBundleInstance instance);
        
        /// <summary>
        /// Deserializes a string representation back to a ProductBundleInstance
        /// </summary>
        /// <param name="serializedData">The serialized string data</param>
        /// <returns>The deserialized ProductBundleInstance</returns>
        ProductBundleInstance Deserialize(string serializedData);
        
        /// <summary>
        /// Attempts to deserialize a string representation back to a ProductBundleInstance
        /// </summary>
        /// <param name="serializedData">The serialized string data</param>
        /// <param name="instance">The deserialized ProductBundleInstance if successful</param>
        /// <returns>True if deserialization was successful, false otherwise</returns>
        bool TryDeserialize(string serializedData, out ProductBundleInstance? instance);
        
        /// <summary>
        /// Gets the format name supported by this serializer
        /// </summary>
        string FormatName { get; }
        
        /// <summary>
        /// Gets the file extension typically used for this format
        /// </summary>
        string FileExtension { get; }
    }
}
