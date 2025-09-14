using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Configuration;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class StorageConfigurationTests
    {
        #region Valid Configuration Tests

        [TestMethod]
        public void Validate_FileSystemProvider_ValidConfiguration_ReturnsValid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "FileSystem",
                FileSystem = new FileSystemStorageOptions
                {
                    StorageDirectory = "/path/to/storage"
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_MongoDBProvider_ValidConfiguration_ReturnsValid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = "TestDB",
                    CollectionName = "TestCollection"
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_SqlServerProvider_ValidConfiguration_ReturnsValid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = "Server=localhost;Database=TestDB;Integrated Security=true;"
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        [TestMethod]
        public void Validate_CaseInsensitiveProvider_ValidConfiguration_ReturnsValid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "FILESYSTEM", // Test case insensitivity
                FileSystem = new FileSystemStorageOptions
                {
                    StorageDirectory = "/path/to/storage"
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(0, result.Errors.Count);
        }

        #endregion

        #region Invalid Configuration Tests

        [TestMethod]
        public void Validate_UnknownProvider_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "UnknownProvider"
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("Unknown storage provider 'UnknownProvider'"));
        }

        [TestMethod]
        public void Validate_FileSystemProvider_MissingConfiguration_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "FileSystem",
                FileSystem = null
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("FileSystem configuration is required"));
        }

        [TestMethod]
        public void Validate_FileSystemProvider_EmptyStorageDirectory_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "FileSystem",
                FileSystem = new FileSystemStorageOptions
                {
                    StorageDirectory = ""
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("FileSystem.StorageDirectory is required"));
        }

        [TestMethod]
        public void Validate_FileSystemProvider_WhitespaceStorageDirectory_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "FileSystem",
                FileSystem = new FileSystemStorageOptions
                {
                    StorageDirectory = "   "
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("FileSystem.StorageDirectory is required"));
        }

        [TestMethod]
        public void Validate_MongoDBProvider_MissingConfiguration_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = null
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("MongoDB configuration is required"));
        }

        [TestMethod]
        public void Validate_MongoDBProvider_MissingConnectionString_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "",
                    DatabaseName = "TestDB"
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("MongoDB.ConnectionString is required"));
        }

        [TestMethod]
        public void Validate_MongoDBProvider_MissingDatabaseName_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = ""
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("MongoDB.DatabaseName is required"));
        }

        [TestMethod]
        public void Validate_MongoDBProvider_MissingBothConnectionStringAndDatabase_ReturnsMultipleErrors()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "",
                    DatabaseName = ""
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(2, result.Errors.Count);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("MongoDB.ConnectionString is required")));
            Assert.IsTrue(result.Errors.Any(e => e.Contains("MongoDB.DatabaseName is required")));
        }

        [TestMethod]
        public void Validate_SqlServerProvider_MissingConfiguration_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = null
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("SqlServer configuration is required"));
        }

        [TestMethod]
        public void Validate_SqlServerProvider_MissingConnectionString_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "SqlServer",
                SqlServer = new SqlServerStorageOptions
                {
                    ConnectionString = ""
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("SqlServer.ConnectionString is required"));
        }

        #endregion

        #region Edge Cases and Null Values

        [TestMethod]
        public void Validate_NullProvider_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = null!
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("Unknown storage provider"));
        }

        [TestMethod]
        public void Validate_EmptyProvider_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = ""
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("Unknown storage provider"));
        }

        [TestMethod]
        public void Validate_WhitespaceProvider_ReturnsInvalid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "   "
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.AreEqual(1, result.Errors.Count);
            Assert.IsTrue(result.Errors[0].Contains("Unknown storage provider"));
        }

        #endregion

        #region ValidationResult Tests

        [TestMethod]
        public void ValidationResult_ValidConfiguration_IsValidTrue()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "FileSystem",
                FileSystem = new FileSystemStorageOptions
                {
                    StorageDirectory = "/valid/path"
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual(string.Empty, result.GetFormattedErrors());
        }

        [TestMethod]
        public void ValidationResult_InvalidConfiguration_IsValidFalse()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "InvalidProvider"
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            Assert.IsFalse(string.IsNullOrEmpty(result.GetFormattedErrors()));
            Assert.IsTrue(result.GetFormattedErrors().Contains("Storage configuration validation failed:"));
        }

        [TestMethod]
        public void ValidationResult_MultipleErrors_FormattedCorrectly()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "",
                    DatabaseName = ""
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsFalse(result.IsValid);
            var formattedErrors = result.GetFormattedErrors();
            Assert.IsTrue(formattedErrors.Contains("Storage configuration validation failed:"));
            Assert.IsTrue(formattedErrors.Contains("- MongoDB.ConnectionString is required"));
            Assert.IsTrue(formattedErrors.Contains("- MongoDB.DatabaseName is required"));
        }

        #endregion

        #region Default Values Tests

        [TestMethod]
        public void StorageConfiguration_DefaultProvider_IsFileSystem()
        {
            // Arrange & Act
            var config = new StorageConfiguration();

            // Assert
            Assert.AreEqual("FileSystem", config.Provider);
        }

        [TestMethod]
        public void MongoStorageOptions_DefaultCollectionName_IsProductBundleInstances()
        {
            // Arrange & Act
            var options = new MongoStorageOptions();

            // Assert
            Assert.AreEqual("ProductBundleInstances", options.CollectionName);
        }

        [TestMethod]
        public void Validate_MongoDBProvider_DefaultCollectionName_IsValid()
        {
            // Arrange
            var config = new StorageConfiguration
            {
                Provider = "MongoDB",
                MongoDB = new MongoStorageOptions
                {
                    ConnectionString = "mongodb://localhost:27017",
                    DatabaseName = "TestDB"
                    // CollectionName uses default value
                }
            };

            // Act
            var result = config.Validate();

            // Assert
            Assert.IsTrue(result.IsValid);
            Assert.AreEqual("ProductBundleInstances", config.MongoDB.CollectionName);
        }

        #endregion
    }
}
