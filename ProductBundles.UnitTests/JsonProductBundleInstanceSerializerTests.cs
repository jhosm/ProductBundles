using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Serialization;
using ProductBundles.Sdk;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class JsonProductBundleInstanceSerializerTests
    {
        [TestMethod]
        public void Constructor_Default_InitializesCorrectly()
        {
            // Arrange & Act
            var serializer = new JsonProductBundleInstanceSerializer();

            // Assert
            Assert.IsNotNull(serializer);
            Assert.AreEqual("JSON", serializer.FormatName);
            Assert.AreEqual(".json", serializer.FileExtension);
        }

        [TestMethod]
        public void Constructor_WithCustomOptions_InitializesCorrectly()
        {
            // Arrange
            var customOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            // Act
            var serializer = new JsonProductBundleInstanceSerializer(customOptions);

            // Assert
            Assert.IsNotNull(serializer);
            Assert.AreEqual("JSON", serializer.FormatName);
            Assert.AreEqual(".json", serializer.FileExtension);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Constructor_WithNullOptions_ThrowsArgumentNullException()
        {
            // Act
            new JsonProductBundleInstanceSerializer(null!);
        }

        [TestMethod]
        public void FormatName_ReturnsCorrectValue()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();

            // Act & Assert
            Assert.AreEqual("JSON", serializer.FormatName);
        }

        [TestMethod]
        public void FileExtension_ReturnsCorrectValue()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();

            // Act & Assert
            Assert.AreEqual(".json", serializer.FileExtension);
        }

        [TestMethod]
        public void Serialize_SimpleInstance_ReturnsValidJson()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var instance = new ProductBundleInstance("test-id", "test-bundle", "1.0.0");

            // Act
            var result = serializer.Serialize(instance);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("test-id"));
            Assert.IsTrue(result.Contains("test-bundle"));
            Assert.IsTrue(result.Contains("1.0.0"));
            Assert.IsTrue(result.Contains("\"id\""));
            Assert.IsTrue(result.Contains("\"productBundleId\""));
            Assert.IsTrue(result.Contains("\"productBundleVersion\""));
        }

        [TestMethod]
        public void Serialize_InstanceWithProperties_ReturnsValidJson()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var properties = new Dictionary<string, object?>
            {
                { "StringProperty", "test-value" },
                { "IntProperty", 42 },
                { "BoolProperty", true }
            };
            var instance = new ProductBundleInstance("test-id", "test-bundle", "1.0.0", properties);

            // Act
            var result = serializer.Serialize(instance);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("StringProperty"));
            Assert.IsTrue(result.Contains("test-value"));
            Assert.IsTrue(result.Contains("IntProperty"));
            Assert.IsTrue(result.Contains("42"));
            Assert.IsTrue(result.Contains("BoolProperty"));
            Assert.IsTrue(result.Contains("true"));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Serialize_NullInstance_ThrowsArgumentNullException()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();

            // Act
            serializer.Serialize(null!);
        }

        [TestMethod]
        public void Deserialize_SimpleJson_ReturnsCorrectInstance()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var json = """
                {
                    "id": "test-id",
                    "productBundleId": "test-bundle",
                    "productBundleVersion": "1.0.0",
                    "properties": {}
                }
                """;

            // Act
            var result = serializer.Deserialize(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test-id", result.Id);
            Assert.AreEqual("test-bundle", result.ProductBundleId);
            Assert.AreEqual("1.0.0", result.ProductBundleVersion);
            Assert.IsNotNull(result.Properties);
            Assert.AreEqual(0, result.Properties.Count);
        }

        [TestMethod]
        public void Deserialize_JsonWithProperties_ReturnsCorrectInstance()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var json = """
                {
                    "id": "test-id",
                    "productBundleId": "test-bundle",
                    "productBundleVersion": "1.0.0",
                    "properties": {
                        "StringProperty": "test-value",
                        "IntProperty": 42,
                        "BoolProperty": true
                    }
                }
                """;

            // Act
            var result = serializer.Deserialize(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("test-id", result.Id);
            Assert.AreEqual("test-bundle", result.ProductBundleId);
            Assert.AreEqual("1.0.0", result.ProductBundleVersion);
            Assert.IsNotNull(result.Properties);
            Assert.AreEqual(3, result.Properties.Count);
            Assert.AreEqual("test-value", result.Properties["StringProperty"]);
            Assert.AreEqual(42.0, result.Properties["IntProperty"]); // JSON numbers become double
            Assert.AreEqual(true, result.Properties["BoolProperty"]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Deserialize_NullString_ThrowsArgumentNullException()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();

            // Act
            serializer.Deserialize(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Deserialize_EmptyString_ThrowsArgumentNullException()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();

            // Act
            serializer.Deserialize(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(JsonException))]
        public void Deserialize_InvalidJson_ThrowsJsonException()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var invalidJson = "{ invalid json }";

            // Act
            serializer.Deserialize(invalidJson);
        }

        [TestMethod]
        public void TryDeserialize_ValidJson_ReturnsTrue()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var json = """
                {
                    "id": "test-id",
                    "productBundleId": "test-bundle",
                    "productBundleVersion": "1.0.0",
                    "properties": {}
                }
                """;

            // Act
            var result = serializer.TryDeserialize(json, out var instance);

            // Assert
            Assert.IsTrue(result);
            Assert.IsNotNull(instance);
            Assert.AreEqual("test-id", instance.Id);
            Assert.AreEqual("test-bundle", instance.ProductBundleId);
            Assert.AreEqual("1.0.0", instance.ProductBundleVersion);
        }

        [TestMethod]
        public void TryDeserialize_InvalidJson_ReturnsFalse()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var invalidJson = "{ invalid json }";

            // Act
            var result = serializer.TryDeserialize(invalidJson, out var instance);

            // Assert
            Assert.IsFalse(result);
            Assert.IsNull(instance);
        }

        [TestMethod]
        public void TryDeserialize_NullOrEmptyString_ReturnsFalse()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();

            // Act & Assert - Null string
            var result1 = serializer.TryDeserialize(null!, out var instance1);
            Assert.IsFalse(result1);
            Assert.IsNull(instance1);

            // Act & Assert - Empty string
            var result2 = serializer.TryDeserialize(string.Empty, out var instance2);
            Assert.IsFalse(result2);
            Assert.IsNull(instance2);
        }

        [TestMethod]
        public void Serialize_InstanceWithComplexProperties_ReturnsValidJson()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var complexProperties = new Dictionary<string, object?>
            {
                { "NullProperty", null },
                { "DateTimeProperty", DateTime.Parse("2023-12-01T10:30:00Z") },
                { "NestedObject", new { Name = "Test", Value = 123 } },
                { "ArrayProperty", new[] { "item1", "item2", "item3" } },
                { "DoubleProperty", 3.14159 },
                { "LongProperty", 9223372036854775807L }
            };
            var instance = new ProductBundleInstance("complex-id", "complex-bundle", "2.0.0", complexProperties);

            // Act
            var result = serializer.Serialize(instance);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("complex-id"));
            Assert.IsTrue(result.Contains("complex-bundle"));
            Assert.IsTrue(result.Contains("2.0.0"));
            Assert.IsTrue(result.Contains("NullProperty"));
            Assert.IsTrue(result.Contains("DateTimeProperty"));
            Assert.IsTrue(result.Contains("NestedObject"));
            Assert.IsTrue(result.Contains("ArrayProperty"));
        }

        [TestMethod]
        public void Deserialize_JsonWithComplexProperties_HandlesAllTypes()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var json = """
                {
                    "id": "complex-id",
                    "productBundleId": "complex-bundle",
                    "productBundleVersion": "2.0.0",
                    "properties": {
                        "NullProperty": null,
                        "StringProperty": "test-value",
                        "NumberProperty": 42.5,
                        "BoolProperty": false,
                        "DateTimeProperty": "2023-12-01T10:30:00Z",
                        "NestedObject": {
                            "Name": "Test",
                            "Value": 123
                        },
                        "ArrayProperty": ["item1", "item2", "item3"]
                    }
                }
                """;

            // Act
            var result = serializer.Deserialize(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("complex-id", result.Id);
            Assert.AreEqual("complex-bundle", result.ProductBundleId);
            Assert.AreEqual("2.0.0", result.ProductBundleVersion);
            Assert.AreEqual(7, result.Properties.Count);
            Assert.IsNull(result.Properties["NullProperty"]);
            Assert.AreEqual("test-value", result.Properties["StringProperty"]);
            Assert.AreEqual(42.5, result.Properties["NumberProperty"]);
            Assert.AreEqual(false, result.Properties["BoolProperty"]);
            Assert.IsInstanceOfType(result.Properties["DateTimeProperty"], typeof(DateTime));
            Assert.IsInstanceOfType(result.Properties["NestedObject"], typeof(Dictionary<string, object?>));
            Assert.IsInstanceOfType(result.Properties["ArrayProperty"], typeof(object[]));
        }

        [TestMethod]
        public void SerializeDeserialize_RoundTrip_PreservesData()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var originalProperties = new Dictionary<string, object?>
            {
                { "StringProp", "original-value" },
                { "NumberProp", 999.99 },
                { "BoolProp", true },
                { "NullProp", null }
            };
            var originalInstance = new ProductBundleInstance("round-trip-id", "round-trip-bundle", "3.0.0", originalProperties);

            // Act
            var serialized = serializer.Serialize(originalInstance);
            var deserialized = serializer.Deserialize(serialized);

            // Assert
            Assert.AreEqual(originalInstance.Id, deserialized.Id);
            Assert.AreEqual(originalInstance.ProductBundleId, deserialized.ProductBundleId);
            Assert.AreEqual(originalInstance.ProductBundleVersion, deserialized.ProductBundleVersion);
            Assert.AreEqual(originalInstance.Properties.Count, deserialized.Properties.Count);
            Assert.AreEqual(originalInstance.Properties["StringProp"], deserialized.Properties["StringProp"]);
            Assert.AreEqual(originalInstance.Properties["NumberProp"], deserialized.Properties["NumberProp"]);
            Assert.AreEqual(originalInstance.Properties["BoolProp"], deserialized.Properties["BoolProp"]);
            Assert.AreEqual(originalInstance.Properties["NullProp"], deserialized.Properties["NullProp"]);
        }

        [TestMethod]
        public void Deserialize_JsonWithMissingProperties_CreatesEmptyPropertiesDictionary()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var json = """
                {
                    "id": "minimal-id",
                    "productBundleId": "minimal-bundle",
                    "productBundleVersion": "1.0.0"
                }
                """;

            // Act
            var result = serializer.Deserialize(json);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("minimal-id", result.Id);
            Assert.AreEqual("minimal-bundle", result.ProductBundleId);
            Assert.AreEqual("1.0.0", result.ProductBundleVersion);
            Assert.IsNotNull(result.Properties);
            Assert.AreEqual(0, result.Properties.Count);
        }

        [TestMethod]
        public void Serialize_InstanceWithNullProperties_HandlesGracefully()
        {
            // Arrange
            var serializer = new JsonProductBundleInstanceSerializer();
            var instance = new ProductBundleInstance("null-props-id", "null-props-bundle", "1.0.0", null);

            // Act
            var result = serializer.Serialize(instance);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("null-props-id"));
            Assert.IsTrue(result.Contains("null-props-bundle"));
            Assert.IsTrue(result.Contains("1.0.0"));
            Assert.IsTrue(result.Contains("properties"));
        }

        [TestMethod]
        public void Constructor_WithNonIndentedOptions_ProducesCompactJson()
        {
            // Arrange
            var compactOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
            var serializer = new JsonProductBundleInstanceSerializer(compactOptions);
            var instance = new ProductBundleInstance("compact-id", "compact-bundle", "1.0.0");

            // Act
            var result = serializer.Serialize(instance);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsFalse(result.Contains("\n")); // No newlines in compact JSON
            Assert.IsFalse(result.Contains("  ")); // No indentation
            Assert.IsTrue(result.Contains("compact-id"));
        }
    }
}
