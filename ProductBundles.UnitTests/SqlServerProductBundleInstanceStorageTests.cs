using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Storage;
using ProductBundles.Sdk;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class SqlServerProductBundleInstanceStorageTests
    {
        private const string TestConnectionString = "Server=localhost,1433;Database=ProductBundlesTest;User Id=sa;Password=ProductBundles123!;TrustServerCertificate=true;MultipleActiveResultSets=true;";
        private ILogger<SqlServerVersionedProductBundleInstanceStorage>? _logger;
        private SqlServerVersionedProductBundleInstanceStorage? _storage;

        [TestInitialize]
        public async Task Setup()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = loggerFactory.CreateLogger<SqlServerVersionedProductBundleInstanceStorage>();
            
            try
            {
                // Clean up any existing test data first
                await CleanupTestData();
                
                // Then create storage instance - this will initialize the database with correct schema
                _storage = new SqlServerVersionedProductBundleInstanceStorage(TestConnectionString, _logger);
            }
            catch (PlatformNotSupportedException)
            {
                // LocalDB not supported on this platform (e.g., macOS)
                Assert.Inconclusive("SQL Server LocalDB not supported on this platform. Tests require SQL Server instance.");
            }
            catch (Exception ex)
            {
                // If we can't connect to SQL Server, skip these tests
                Assert.Inconclusive($"SQL Server not available for testing: {ex.Message}");
            }
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            if (_storage != null)
            {
                await CleanupTestData();
            }
        }

        private async Task CleanupTestData()
        {
            try
            {
                using var connection = new SqlConnection(TestConnectionString);
                await connection.OpenAsync();
                
                // Drop tables to ensure clean schema for versioned storage
                var dropVersionsTableSql = @"
                    IF EXISTS (SELECT * FROM sysobjects WHERE name='ProductBundleInstanceVersions' AND xtype='U')
                    DROP TABLE ProductBundleInstanceVersions";
                using var dropVersionsCommand = new SqlCommand(dropVersionsTableSql, connection);
                await dropVersionsCommand.ExecuteNonQueryAsync();
                
                var dropInstancesTableSql = @"
                    IF EXISTS (SELECT * FROM sysobjects WHERE name='ProductBundleInstances' AND xtype='U')
                    DROP TABLE ProductBundleInstances";
                using var dropInstancesCommand = new SqlCommand(dropInstancesTableSql, connection);
                await dropInstancesCommand.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test cleanup failed: {ex.Message}");
            }
        }

        [TestMethod]
        public async Task CreateAsync_WithValidInstance_ReturnsTrue()
        {
            // Arrange
            var instance = new ProductBundleInstance("test-id", "test-bundle", "1.0.0");
            instance.Properties["testProperty"] = "testValue";

            // Act
            var result = await _storage!.CreateAsync(instance);

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task CreateAsync_WithDuplicateId_ReturnsFalse()
        {
            // Arrange
            var instance1 = new ProductBundleInstance("duplicate-id", "test-bundle", "1.0.0");
            var instance2 = new ProductBundleInstance("duplicate-id", "test-bundle", "1.0.0");

            // Act
            await _storage!.CreateAsync(instance1);
            var result = await _storage.CreateAsync(instance2);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task CreateAsync_WithNullInstance_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _storage!.CreateAsync(null!));
        }

        [TestMethod]
        public async Task CreateAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Arrange
            var instance = new ProductBundleInstance("", "test-bundle", "1.0.0");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _storage!.CreateAsync(instance));
        }

        [TestMethod]
        public async Task GetAsync_WithExistingId_ReturnsInstance()
        {
            // Arrange
            var originalInstance = new ProductBundleInstance("get-test-id", "test-bundle", "1.0.0");
            originalInstance.Properties["testProperty"] = "testValue";
            originalInstance.Properties["numberProperty"] = 42;
            
            await _storage!.CreateAsync(originalInstance);

            // Act
            var retrievedInstance = await _storage.GetAsync("get-test-id");

            // Assert
            Assert.IsNotNull(retrievedInstance);
            Assert.AreEqual("get-test-id", retrievedInstance.Id);
            Assert.AreEqual("test-bundle", retrievedInstance.ProductBundleId);
            Assert.AreEqual("1.0.0", retrievedInstance.ProductBundleVersion);
            Assert.AreEqual("testValue", ((JsonElement)retrievedInstance.Properties["testProperty"]!).GetString());
            Assert.AreEqual(42, ((JsonElement)retrievedInstance.Properties["numberProperty"]!).GetInt32());
        }

        [TestMethod]
        public async Task GetAsync_WithNonExistentId_ReturnsNull()
        {
            // Act
            var result = await _storage!.GetAsync("non-existent-id");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _storage!.GetAsync(""));
        }

        [TestMethod]
        public async Task UpdateAsync_WithExistingInstance_ReturnsTrue()
        {
            // Arrange
            var originalInstance = new ProductBundleInstance("update-test-id", "test-bundle", "1.0.0");
            originalInstance.Properties["originalProperty"] = "originalValue";
            
            await _storage!.CreateAsync(originalInstance);

            var updatedInstance = new ProductBundleInstance("update-test-id", "test-bundle", "2.0.0");
            updatedInstance.Properties["updatedProperty"] = "updatedValue";

            // Act
            var result = await _storage.UpdateAsync(updatedInstance);

            // Assert
            Assert.IsTrue(result);

            // Verify the update
            var retrievedInstance = await _storage.GetAsync("update-test-id");
            Assert.IsNotNull(retrievedInstance);
            Assert.AreEqual("2.0.0", retrievedInstance.ProductBundleVersion);
            Assert.AreEqual("updatedValue", ((JsonElement)retrievedInstance.Properties["updatedProperty"]!).GetString());
        }

        [TestMethod]
        public async Task UpdateAsync_WithNonExistentInstance_ReturnsFalse()
        {
            // Arrange
            var instance = new ProductBundleInstance("non-existent-update-id", "test-bundle", "1.0.0");

            // Act
            var result = await _storage!.UpdateAsync(instance);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateAsync_WithNullInstance_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _storage!.UpdateAsync(null!));
        }

        [TestMethod]
        public async Task DeleteAsync_WithExistingId_ReturnsTrue()
        {
            // Arrange
            var instance = new ProductBundleInstance("delete-test-id", "test-bundle", "1.0.0");
            await _storage!.CreateAsync(instance);

            // Act
            var result = await _storage.DeleteAsync("delete-test-id");

            // Assert
            Assert.IsTrue(result);

            // Verify deletion
            var retrievedInstance = await _storage.GetAsync("delete-test-id");
            Assert.IsNull(retrievedInstance);
        }

        [TestMethod]
        public async Task DeleteAsync_WithNonExistentId_ReturnsFalse()
        {
            // Act
            var result = await _storage!.DeleteAsync("non-existent-delete-id");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task DeleteAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _storage!.DeleteAsync(""));
        }

        [TestMethod]
        public async Task ExistsAsync_WithExistingId_ReturnsTrue()
        {
            // Arrange
            var instance = new ProductBundleInstance("exists-test-id", "test-bundle", "1.0.0");
            await _storage!.CreateAsync(instance);

            // Act
            var result = await _storage.ExistsAsync("exists-test-id");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ExistsAsync_WithNonExistentId_ReturnsFalse()
        {
            // Act
            var result = await _storage!.ExistsAsync("non-existent-exists-id");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ExistsAsync_WithEmptyId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _storage!.ExistsAsync(""));
        }

        [TestMethod]
        public async Task GetCountAsync_WithEmptyStorage_ReturnsZero()
        {
            // Act
            var count = await _storage!.GetCountAsync();

            // Assert
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task GetCountAsync_WithMultipleInstances_ReturnsCorrectCount()
        {
            // Arrange
            var instance1 = new ProductBundleInstance("count-test-1", "test-bundle", "1.0.0");
            var instance2 = new ProductBundleInstance("count-test-2", "test-bundle", "1.0.0");
            var instance3 = new ProductBundleInstance("count-test-3", "other-bundle", "1.0.0");

            await _storage!.CreateAsync(instance1);
            await _storage.CreateAsync(instance2);
            await _storage.CreateAsync(instance3);

            // Act
            var count = await _storage.GetCountAsync();

            // Assert
            Assert.AreEqual(3, count);
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithMatchingInstances_ReturnsCorrectCount()
        {
            // Arrange
            var instance1 = new ProductBundleInstance("bundle-count-1", "target-bundle", "1.0.0");
            var instance2 = new ProductBundleInstance("bundle-count-2", "target-bundle", "1.0.0");
            var instance3 = new ProductBundleInstance("bundle-count-3", "other-bundle", "1.0.0");

            await _storage!.CreateAsync(instance1);
            await _storage.CreateAsync(instance2);
            await _storage.CreateAsync(instance3);

            // Act
            var count = await _storage.GetCountByProductBundleIdAsync("target-bundle");

            // Assert
            Assert.AreEqual(2, count);
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithNoMatchingInstances_ReturnsZero()
        {
            // Arrange
            var instance = new ProductBundleInstance("no-match-test", "other-bundle", "1.0.0");
            await _storage!.CreateAsync(instance);

            // Act
            var count = await _storage.GetCountByProductBundleIdAsync("non-existent-bundle");

            // Assert
            Assert.AreEqual(0, count);
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithEmptyProductBundleId_ThrowsArgumentException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _storage!.GetCountByProductBundleIdAsync(""));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithMatchingInstances_ReturnsPaginatedResults()
        {
            // Arrange
            var instance1 = new ProductBundleInstance("paginated-1", "paginated-bundle", "1.0.0");
            var instance2 = new ProductBundleInstance("paginated-2", "paginated-bundle", "1.0.0");
            var instance3 = new ProductBundleInstance("paginated-3", "paginated-bundle", "1.0.0");
            var instance4 = new ProductBundleInstance("paginated-4", "other-bundle", "1.0.0");

            await _storage!.CreateAsync(instance1);
            await _storage.CreateAsync(instance2);
            await _storage.CreateAsync(instance3);
            await _storage.CreateAsync(instance4);

            var paginationRequest = new PaginationRequest(1, 2);

            // Act
            var result = await _storage.GetByProductBundleIdAsync("paginated-bundle", paginationRequest);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.PageNumber);
            Assert.AreEqual(2, result.PageSize);
            Assert.AreEqual(2, result.Items.Count());

            var items = result.Items.ToList();
            Assert.IsTrue(items.All(i => i.ProductBundleId == "paginated-bundle"));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithSecondPage_ReturnsCorrectResults()
        {
            // Arrange
            var instance1 = new ProductBundleInstance("page-test-1", "page-bundle", "1.0.0");
            var instance2 = new ProductBundleInstance("page-test-2", "page-bundle", "1.0.0");
            var instance3 = new ProductBundleInstance("page-test-3", "page-bundle", "1.0.0");

            await _storage!.CreateAsync(instance1);
            await _storage.CreateAsync(instance2);
            await _storage.CreateAsync(instance3);

            var paginationRequest = new PaginationRequest(2, 2);

            // Act
            var result = await _storage.GetByProductBundleIdAsync("page-bundle", paginationRequest);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.PageNumber);
            Assert.AreEqual(2, result.PageSize);
            Assert.AreEqual(1, result.Items.Count()); // Only 1 item on second page
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithEmptyProductBundleId_ThrowsArgumentException()
        {
            // Arrange
            var paginationRequest = new PaginationRequest(1, 10);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                () => _storage!.GetByProductBundleIdAsync("", paginationRequest));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithNullPaginationRequest_ThrowsArgumentNullException()
        {
            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(
                () => _storage!.GetByProductBundleIdAsync("test-bundle", null!));
        }

        [TestMethod]
        public async Task ComplexJsonProperties_AreStoredAndRetrievedCorrectly()
        {
            // Arrange
            var instance = new ProductBundleInstance("complex-json-test", "test-bundle", "1.0.0");
            instance.Properties["stringProperty"] = "test string";
            instance.Properties["numberProperty"] = 42;
            instance.Properties["booleanProperty"] = true;
            instance.Properties["arrayProperty"] = new[] { "item1", "item2", "item3" };
            instance.Properties["objectProperty"] = new Dictionary<string, object?>
            {
                ["nestedString"] = "nested value",
                ["nestedNumber"] = 123,
                ["nestedBoolean"] = false
            };

            // Act
            await _storage!.CreateAsync(instance);
            var retrievedInstance = await _storage.GetAsync("complex-json-test");

            // Assert
            Assert.IsNotNull(retrievedInstance);
            Assert.AreEqual("test string", ((JsonElement)retrievedInstance.Properties["stringProperty"]!).GetString());
            Assert.AreEqual(42, ((JsonElement)retrievedInstance.Properties["numberProperty"]!).GetInt32());
            Assert.AreEqual(true, ((JsonElement)retrievedInstance.Properties["booleanProperty"]!).GetBoolean());
            
            var arrayElement = (JsonElement)retrievedInstance.Properties["arrayProperty"]!;
            Assert.AreEqual(3, arrayElement.GetArrayLength());
            Assert.AreEqual("item1", arrayElement[0].GetString());
            
            var objectElement = (JsonElement)retrievedInstance.Properties["objectProperty"]!;
            Assert.AreEqual("nested value", objectElement.GetProperty("nestedString").GetString());
            Assert.AreEqual(123, objectElement.GetProperty("nestedNumber").GetInt32());
            Assert.AreEqual(false, objectElement.GetProperty("nestedBoolean").GetBoolean());
        }
    }
}
