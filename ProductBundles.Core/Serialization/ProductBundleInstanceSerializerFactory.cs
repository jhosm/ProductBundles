using System.Text.Json;

namespace ProductBundles.Core.Serialization
{
    /// <summary>
    /// Factory class for creating ProductBundleInstance serializers
    /// </summary>
    public static class ProductBundleInstanceSerializerFactory
    {
        /// <summary>
        /// Enumeration of supported serialization formats
        /// </summary>
        public enum SerializationFormat
        {
            /// <summary>
            /// JSON format using System.Text.Json
            /// </summary>
            Json,
            
            /// <summary>
            /// XML format (future implementation)
            /// </summary>
            Xml
        }

        /// <summary>
        /// Creates a serializer for the specified format
        /// </summary>
        /// <param name="format">The serialization format to create a serializer for</param>
        /// <returns>An instance of IProductBundleInstanceSerializer</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when format is not supported</exception>
        public static IProductBundleInstanceSerializer CreateSerializer(SerializationFormat format)
        {
            return format switch
            {
                SerializationFormat.Json => new JsonProductBundleInstanceSerializer(),
                SerializationFormat.Xml => throw new NotImplementedException("XML serialization is not yet implemented"),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported serialization format")
            };
        }

        /// <summary>
        /// Creates a JSON serializer with custom options
        /// </summary>
        /// <param name="jsonOptions">Custom JSON serialization options</param>
        /// <returns>An instance of JsonProductBundleInstanceSerializer</returns>
        public static JsonProductBundleInstanceSerializer CreateJsonSerializer(JsonSerializerOptions jsonOptions)
        {
            return new JsonProductBundleInstanceSerializer(jsonOptions);
        }

        /// <summary>
        /// Creates a JSON serializer with default options
        /// </summary>
        /// <returns>An instance of JsonProductBundleInstanceSerializer</returns>
        public static JsonProductBundleInstanceSerializer CreateJsonSerializer()
        {
            return new JsonProductBundleInstanceSerializer();
        }

        /// <summary>
        /// Gets all supported serialization formats
        /// </summary>
        /// <returns>An enumerable of supported formats</returns>
        public static IEnumerable<SerializationFormat> GetSupportedFormats()
        {
            return new[] { SerializationFormat.Json };
        }

        /// <summary>
        /// Gets the format name for a given serialization format
        /// </summary>
        /// <param name="format">The serialization format</param>
        /// <returns>The format name</returns>
        public static string GetFormatName(SerializationFormat format)
        {
            return format switch
            {
                SerializationFormat.Json => "JSON",
                SerializationFormat.Xml => "XML",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported serialization format")
            };
        }

        /// <summary>
        /// Gets the file extension for a given serialization format
        /// </summary>
        /// <param name="format">The serialization format</param>
        /// <returns>The file extension including the dot</returns>
        public static string GetFileExtension(SerializationFormat format)
        {
            return format switch
            {
                SerializationFormat.Json => ".json",
                SerializationFormat.Xml => ".xml",
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported serialization format")
            };
        }
    }
}
