using System.Globalization;
using System.Text.Json;
using ProductBundles.Sdk;

namespace ProductBundles.Core.Serialization
{
    /// <summary>
    /// JSON implementation of IProductBundleInstanceSerializer using System.Text.Json
    /// </summary>
    public class JsonProductBundleInstanceSerializer : IProductBundleInstanceSerializer
    {
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the JsonProductBundleInstanceSerializer class
        /// </summary>
        public JsonProductBundleInstanceSerializer()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
        }

        /// <summary>
        /// Initializes a new instance of the JsonProductBundleInstanceSerializer class with custom options
        /// </summary>
        /// <param name="jsonOptions">Custom JSON serialization options</param>
        public JsonProductBundleInstanceSerializer(JsonSerializerOptions jsonOptions)
        {
            _jsonOptions = jsonOptions ?? throw new ArgumentNullException(nameof(jsonOptions));
        }

        /// <summary>
        /// Gets the format name supported by this serializer
        /// </summary>
        public string FormatName => "JSON";

        /// <summary>
        /// Gets the file extension typically used for this format
        /// </summary>
        public string FileExtension => ".json";

        /// <summary>
        /// Serializes a ProductBundleInstance to a JSON string representation
        /// </summary>
        /// <param name="instance">The ProductBundleInstance to serialize</param>
        /// <returns>JSON string representation of the instance</returns>
        /// <exception cref="ArgumentNullException">Thrown when instance is null</exception>
        /// <exception cref="JsonException">Thrown when serialization fails</exception>
        public string Serialize(ProductBundleInstance instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            try
            {
                // Create a serialization-friendly object that handles the Properties dictionary correctly
                var serializableObject = new
                {
                    id = instance.Id,
                    productBundleId = instance.ProductBundleId,
                    productBundleVersion = instance.ProductBundleVersion,
                    properties = instance.Properties?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value) ?? new Dictionary<string, object?>()
                };

                return JsonSerializer.Serialize(serializableObject, _jsonOptions);
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                throw new JsonException($"Failed to serialize ProductBundleInstance with ID '{instance.Id}': {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Deserializes a JSON string representation back to a ProductBundleInstance
        /// </summary>
        /// <param name="serializedData">The serialized JSON string data</param>
        /// <returns>The deserialized ProductBundleInstance</returns>
        /// <exception cref="ArgumentNullException">Thrown when serializedData is null</exception>
        /// <exception cref="JsonException">Thrown when deserialization fails</exception>
        public ProductBundleInstance Deserialize(string serializedData)
        {
            if (string.IsNullOrEmpty(serializedData))
            {
                throw new ArgumentNullException(nameof(serializedData));
            }

            try
            {
                using var document = JsonDocument.Parse(serializedData);
                var root = document.RootElement;

                var id = root.GetProperty("id").GetString() ?? string.Empty;
                var productBundleId = root.GetProperty("productBundleId").GetString() ?? string.Empty;
                var productBundleVersion = root.GetProperty("productBundleVersion").GetString() ?? string.Empty;

                var properties = new Dictionary<string, object?>();
                
                if (root.TryGetProperty("properties", out var propertiesElement))
                {
                    foreach (var property in propertiesElement.EnumerateObject())
                    {
                        properties[property.Name] = ConvertJsonElementToObject(property.Value);
                    }
                }

                return new ProductBundleInstance(id, productBundleId, productBundleVersion, properties);
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                throw new JsonException($"Failed to deserialize JSON data to ProductBundleInstance: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Attempts to deserialize a JSON string representation back to a ProductBundleInstance
        /// </summary>
        /// <param name="serializedData">The serialized JSON string data</param>
        /// <param name="instance">The deserialized ProductBundleInstance if successful</param>
        /// <returns>True if deserialization was successful, false otherwise</returns>
        public bool TryDeserialize(string serializedData, out ProductBundleInstance? instance)
        {
            instance = null;

            if (string.IsNullOrEmpty(serializedData))
            {
                return false;
            }

            try
            {
                instance = Deserialize(serializedData);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Converts a JsonElement to its corresponding .NET object type
        /// </summary>
        /// <param name="element">The JsonElement to convert</param>
        /// <returns>The converted object</returns>
        private static object? ConvertJsonElementToObject(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                    prop => prop.Name,
                    prop => ConvertJsonElementToObject(prop.Value)
                ),
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonElementToObject).ToArray(),
                JsonValueKind.String => ConvertStringValue(element.GetString()),
                JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString()
            };
        }

        /// <summary>
        /// Converts a string value to its most appropriate .NET type
        /// </summary>
        /// <param name="stringValue">The string value to convert</param>
        /// <returns>The converted object (DateTime if it's a valid date, otherwise string)</returns>
        private static object? ConvertStringValue(string? stringValue)
        {
            if (string.IsNullOrEmpty(stringValue))
            {
                return stringValue;
            }

            // Try to parse as DateTime first (handles ISO 8601 format and other common formats)
            if (DateTime.TryParse(stringValue, null, DateTimeStyles.AdjustToUniversal, out var dateTime))
            {
                // Additional check to ensure it's likely a serialized DateTime
                // Look for common DateTime patterns (ISO 8601, etc.)
                if (stringValue.Contains('T') || stringValue.Contains('-') && stringValue.Contains(':'))
                {
                    return dateTime;
                }
            }

            // Return as string if it's not a recognizable DateTime format
            return stringValue;
        }
    }
}
