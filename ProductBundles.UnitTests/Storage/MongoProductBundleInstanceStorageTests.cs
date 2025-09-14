using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using ProductBundles.Core.Storage;
using ProductBundles.Sdk;
using System.ComponentModel.DataAnnotations;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class MongoProductBundleInstanceStorageTests
    {
        private const string TestConnectionString = "mongodb://localhost:27017";
        private const string TestDatabaseName = "productbundles_test";
        private const string TestCollectionName = "instances_test";
        private ILogger<MongoProductBundleInstanceStorage>? _logger;

        [TestInitialize]
        public async Task Setup()
        {
            var loggerFactory = LoggerFactory.Create(builder => { });
            _logger = loggerFactory.CreateLogger<MongoProductBundleInstanceStorage>();
            
            // Clean up any existing test data before each test
            await CleanupTestData();
        }

        [TestCleanup]
        public async Task TestCleanup()
        {
            // Clean up test data after each test
            await CleanupTestData();
        }

        private async Task CleanupTestData()
        {
            try
            {
                var client = new MongoClient(TestConnectionString);
                var database = client.GetDatabase(TestDatabaseName);
                var collection = database.GetCollection<ProductBundleInstance>(TestCollectionName);
                
                // Clear all documents from the test collection
                await collection.DeleteManyAsync(Builders<ProductBundleInstance>.Filter.Empty);
            }
            catch (Exception ex)
            {
                // Log but don't fail tests due to cleanup issues
                Console.WriteLine($"Test cleanup failed: {ex.Message}");
            }
        }

        [TestMethod]
        public void Constructor_WithValidParameters_ShouldInitializeSuccessfully()
        {
            // Act & Assert - Should not throw
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // If we reach here, the constructor succeeded
            Assert.IsNotNull(storage);
        }

        [TestMethod]
        public void Constructor_WithValidParametersAndDefaultCollection_ShouldInitializeSuccessfully()
        {
            // Act & Assert - Should not throw
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                logger: _logger);

            // If we reach here, the constructor succeeded
            Assert.IsNotNull(storage);
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ShouldInitializeSuccessfully()
        {
            // Act & Assert - Should not throw
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName);

            // If we reach here, the constructor succeeded
            Assert.IsNotNull(storage);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithNullConnectionString_ShouldThrowArgumentException()
        {
            // Act
            new MongoProductBundleInstanceStorage(
                null!,
                TestDatabaseName,
                TestCollectionName,
                _logger);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithEmptyConnectionString_ShouldThrowArgumentException()
        {
            // Act
            new MongoProductBundleInstanceStorage(
                "",
                TestDatabaseName,
                TestCollectionName,
                _logger);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithWhitespaceConnectionString_ShouldThrowArgumentException()
        {
            // Act
            new MongoProductBundleInstanceStorage(
                "   ",
                TestDatabaseName,
                TestCollectionName,
                _logger);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithNullDatabaseName_ShouldThrowArgumentException()
        {
            // Act
            new MongoProductBundleInstanceStorage(
                TestConnectionString,
                null!,
                TestCollectionName,
                _logger);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithEmptyDatabaseName_ShouldThrowArgumentException()
        {
            // Act
            new MongoProductBundleInstanceStorage(
                TestConnectionString,
                "",
                TestCollectionName,
                _logger);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithWhitespaceDatabaseName_ShouldThrowArgumentException()
        {
            // Act
            new MongoProductBundleInstanceStorage(
                TestConnectionString,
                "   ",
                TestCollectionName,
                _logger);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithNullCollectionName_ShouldThrowArgumentException()
        {
            // Act
            new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                null!,
                _logger);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithEmptyCollectionName_ShouldThrowArgumentException()
        {
            // Act
            new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                "",
                _logger);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void Constructor_WithWhitespaceCollectionName_ShouldThrowArgumentException()
        {
            // Act
            new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                "   ",
                _logger);
        }

        #region CreateAsync Tests

        [TestMethod]
        public async Task CreateAsync_WithValidInstance_ShouldReturnTrue()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            var instance = new ProductBundleInstance(
                "test-id-" + Guid.NewGuid(),
                "test-plugin",
                "1.0.0");
            instance.Properties["testProperty"] = "testValue";

            try
            {
                // Act
                var result = await storage.CreateAsync(instance);

                // Assert
                Assert.IsTrue(result, "CreateAsync should return true for new instance");
            }
            finally
            {
                // Cleanup: try to delete the test instance
                try
                {
                    await storage.DeleteAsync(instance.Id);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public async Task CreateAsync_WithDuplicateId_ShouldReturnFalse()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            var instanceId = "duplicate-test-id-" + Guid.NewGuid();
            var instance1 = new ProductBundleInstance(instanceId, "test-plugin", "1.0.0");
            var instance2 = new ProductBundleInstance(instanceId, "test-plugin", "1.0.0");

            try
            {
                // Act
                var result1 = await storage.CreateAsync(instance1);
                var result2 = await storage.CreateAsync(instance2);

                // Assert
                Assert.IsTrue(result1, "First CreateAsync should return true");
                Assert.IsFalse(result2, "Second CreateAsync with same ID should return false");
            }
            finally
            {
                // Cleanup: try to delete the test instance
                try
                {
                    await storage.DeleteAsync(instanceId);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task CreateAsync_WithNullInstance_ShouldThrowArgumentNullException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.CreateAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task CreateAsync_WithNullId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            var instance = new ProductBundleInstance(null!, "test-plugin", "1.0.0");

            // Act
            await storage.CreateAsync(instance);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task CreateAsync_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            var instance = new ProductBundleInstance("", "test-plugin", "1.0.0");

            // Act
            await storage.CreateAsync(instance);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task CreateAsync_WithWhitespaceId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            var instance = new ProductBundleInstance("   ", "test-plugin", "1.0.0");

            // Act
            await storage.CreateAsync(instance);
        }

        #endregion

        #region GetAsync Tests

        [TestMethod]
        public async Task GetAsync_WithExistingId_ShouldReturnInstance()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            var instance = new ProductBundleInstance(
                "get-test-id-" + Guid.NewGuid(),
                "test-plugin",
                "1.0.0");
            instance.Properties["testProperty"] = "testValue";
            instance.Properties["numericProperty"] = 42;

            try
            {
                // Create the instance first
                await storage.CreateAsync(instance);

                // Act
                var retrievedInstance = await storage.GetAsync(instance.Id);

                // Assert
                Assert.IsNotNull(retrievedInstance, "GetAsync should return the instance");
                Assert.AreEqual(instance.Id, retrievedInstance.Id);
                Assert.AreEqual(instance.ProductBundleId, retrievedInstance.ProductBundleId);
                Assert.AreEqual(instance.ProductBundleVersion, retrievedInstance.ProductBundleVersion);
                Assert.AreEqual("testValue", retrievedInstance.Properties["testProperty"]);
                Assert.AreEqual(42, retrievedInstance.Properties["numericProperty"]);
            }
            finally
            {
                // Cleanup: try to delete the test instance
                try
                {
                    await storage.DeleteAsync(instance.Id);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public async Task GetAsync_WithNonExistentId_ShouldReturnNull()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            var nonExistentId = "non-existent-id-" + Guid.NewGuid();

            // Act
            var result = await storage.GetAsync(nonExistentId);

            // Assert
            Assert.IsNull(result, "GetAsync should return null for non-existent ID");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetAsync_WithNullId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.GetAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetAsync_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.GetAsync("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task GetAsync_WithWhitespaceId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.GetAsync("   ");
        }

        #endregion



        #region GetByProductBundleIdAsync Tests - All Non-Paginated Tests Removed











        #endregion

        #region GetByProductBundleIdAsync Pagination Tests

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            // Create 15 instances for the same ProductBundle
            for (int i = 1; i <= 15; i++)
            {
                var instance = new ProductBundleInstance($"id{i}", "test-bundle", "1.0.0");
                await storage.CreateAsync(instance);
            }
            
            var paginationRequest = new PaginationRequest(2, 5); // Page 2, 5 items per page

            // Act
            var result = await storage.GetByProductBundleIdAsync("test-bundle", paginationRequest);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.PageNumber);
            Assert.AreEqual(5, result.PageSize);
            Assert.AreEqual(5, result.Items.Count());
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationFirstPage_ReturnsCorrectMetadata()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            // Create 12 instances for the same ProductBundle
            for (int i = 1; i <= 12; i++)
            {
                var instance = new ProductBundleInstance($"id{i}", "test-bundle", "1.0.0");
                await storage.CreateAsync(instance);
            }
            
            var paginationRequest = new PaginationRequest(1, 10);

            // Act
            var result = await storage.GetByProductBundleIdAsync("test-bundle", paginationRequest);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.PageNumber);
            Assert.AreEqual(10, result.PageSize);
            Assert.AreEqual(10, result.Items.Count());
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationLastPage_ReturnsCorrectMetadata()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            // Create 12 instances for the same ProductBundle
            for (int i = 1; i <= 12; i++)
            {
                var instance = new ProductBundleInstance($"id{i}", "test-bundle", "1.0.0");
                await storage.CreateAsync(instance);
            }
            
            var paginationRequest = new PaginationRequest(2, 10); // Last page

            // Act
            var result = await storage.GetByProductBundleIdAsync("test-bundle", paginationRequest);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.PageNumber);
            Assert.AreEqual(10, result.PageSize);
            Assert.AreEqual(2, result.Items.Count()); // Only 2 items on last page
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationNoMatches_ReturnsEmptyResult()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            // Create instances for different ProductBundle
            var instance = new ProductBundleInstance("id1", "different-bundle", "1.0.0");
            await storage.CreateAsync(instance);
            
            var paginationRequest = new PaginationRequest(1, 10);

            // Act
            var result = await storage.GetByProductBundleIdAsync("test-bundle", paginationRequest);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.PageNumber);
            Assert.AreEqual(10, result.PageSize);
            Assert.AreEqual(0, result.Items.Count());
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationNullProductBundleId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            var paginationRequest = new PaginationRequest(1, 10);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetByProductBundleIdAsync(null!, paginationRequest));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationEmptyProductBundleId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            var paginationRequest = new PaginationRequest(1, 10);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetByProductBundleIdAsync("", paginationRequest));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationWhitespaceProductBundleId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            var paginationRequest = new PaginationRequest(1, 10);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetByProductBundleIdAsync("   ", paginationRequest));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithNullPaginationRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                storage.GetByProductBundleIdAsync("test-bundle", null!));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationMixedProductBundles_FiltersCorrectly()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            // Create instances for different ProductBundles
            for (int i = 1; i <= 10; i++)
            {
                var instance1 = new ProductBundleInstance($"bundle-a-{i}", "bundle-a", "1.0.0");
                var instance2 = new ProductBundleInstance($"bundle-b-{i}", "bundle-b", "1.0.0");
                await storage.CreateAsync(instance1);
                await storage.CreateAsync(instance2);
            }
            
            var paginationRequest = new PaginationRequest(1, 5);

            // Act
            var result = await storage.GetByProductBundleIdAsync("bundle-a", paginationRequest);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.Items.Count()); // Should only return items for current page
            
            // Verify all returned items are for bundle-a
            foreach (var item in result.Items)
            {
                Assert.AreEqual("bundle-a", item.ProductBundleId);
            }
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationLargePageSize_ReturnsAllAvailableItems()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            // Create 5 instances for the same ProductBundle
            for (int i = 1; i <= 5; i++)
            {
                var instance = new ProductBundleInstance($"id{i}", "test-bundle", "1.0.0");
                await storage.CreateAsync(instance);
            }
            
            var paginationRequest = new PaginationRequest(1, 100); // Request more than available

            // Act
            var result = await storage.GetByProductBundleIdAsync("test-bundle", paginationRequest);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.PageNumber);
            Assert.AreEqual(100, result.PageSize);
            Assert.AreEqual(5, result.Items.Count()); // Should return all available items
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationOrderConsistency_ReturnsConsistentResults()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            // Create 10 instances for the same ProductBundle with predictable IDs
            var createdInstances = new List<ProductBundleInstance>();
            for (int i = 1; i <= 10; i++)
            {
                var instance = new ProductBundleInstance($"id{i:00}", "test-bundle", "1.0.0");
                createdInstances.Add(instance);
                await storage.CreateAsync(instance);
            }
            
            var firstPageRequest = new PaginationRequest(1, 5);
            var secondPageRequest = new PaginationRequest(2, 5);

            // Act
            var firstPageResult = await storage.GetByProductBundleIdAsync("test-bundle", firstPageRequest);
            var secondPageResult = await storage.GetByProductBundleIdAsync("test-bundle", secondPageRequest);

            // Assert
            Assert.IsNotNull(firstPageResult);
            Assert.IsNotNull(secondPageResult);
            Assert.AreEqual(5, firstPageResult.Items.Count());
            Assert.AreEqual(5, secondPageResult.Items.Count());
            
            // Verify no overlap between pages
            var firstPageIds = firstPageResult.Items.Select(i => i.Id).ToHashSet();
            var secondPageIds = secondPageResult.Items.Select(i => i.Id).ToHashSet();
            Assert.IsFalse(firstPageIds.Overlaps(secondPageIds), "Pages should not have overlapping items");
            
            // Verify all items are accounted for
            var allReturnedIds = firstPageIds.Union(secondPageIds);
            Assert.AreEqual(10, allReturnedIds.Count());
        }

        #endregion

        #region UpdateAsync Tests

        [TestMethod]
        public async Task UpdateAsync_WithExistingInstance_ShouldReturnTrueAndUpdateInstance()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_update_existing_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var originalInstance = new ProductBundleInstance(
                "update-test-" + Guid.NewGuid(),
                "test-plugin",
                "1.0.0");
            originalInstance.Properties["originalProp"] = "originalValue";
            originalInstance.Properties["numericProp"] = 100;

            try
            {
                // Create the original instance
                await storage.CreateAsync(originalInstance);

                // Modify the instance
                var updatedInstance = new ProductBundleInstance(
                    originalInstance.Id, // Same ID
                    "updated-plugin", // Changed ProductBundleId
                    "2.0.0"); // Changed version
                updatedInstance.Properties["originalProp"] = "updatedValue"; // Updated existing property
                updatedInstance.Properties["newProp"] = "newValue"; // Added new property
                updatedInstance.Properties["numericProp"] = 200; // Updated numeric property
                // Note: originalInstance had "numericProp" but updatedInstance doesn't include it in the replacement

                // Act
                var result = await storage.UpdateAsync(updatedInstance);

                // Assert
                Assert.IsTrue(result, "UpdateAsync should return true for existing instance");

                // Verify the instance was actually updated
                var retrievedInstance = await storage.GetAsync(originalInstance.Id);
                Assert.IsNotNull(retrievedInstance, "Updated instance should still exist");
                Assert.AreEqual(originalInstance.Id, retrievedInstance.Id, "ID should remain the same");
                Assert.AreEqual("updated-plugin", retrievedInstance.ProductBundleId, "ProductBundleId should be updated");
                Assert.AreEqual("2.0.0", retrievedInstance.ProductBundleVersion, "ProductBundleVersion should be updated");
                Assert.AreEqual("updatedValue", retrievedInstance.Properties["originalProp"], "Existing property should be updated");
                Assert.AreEqual("newValue", retrievedInstance.Properties["newProp"], "New property should be added");
                Assert.AreEqual(200, retrievedInstance.Properties["numericProp"], "Numeric property should be updated");
            }
            finally
            {
                // Cleanup: try to delete the test instance
                try
                {
                    await storage.DeleteAsync(originalInstance.Id);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        public async Task UpdateAsync_WithNonExistentInstance_ShouldReturnFalse()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_update_nonexistent_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var nonExistentInstance = new ProductBundleInstance(
                "non-existent-" + Guid.NewGuid(),
                "test-plugin",
                "1.0.0");
            nonExistentInstance.Properties["testProp"] = "testValue";

            // Act
            var result = await storage.UpdateAsync(nonExistentInstance);

            // Assert
            Assert.IsFalse(result, "UpdateAsync should return false for non-existent instance");
        }

        [TestMethod]
        public async Task UpdateAsync_WithPropertyChanges_ShouldPreserveAllChanges()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_update_properties_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var originalInstance = new ProductBundleInstance(
                "prop-update-test-" + Guid.NewGuid(),
                "test-plugin",
                "1.0.0");
            originalInstance.Properties["stringProp"] = "original";
            originalInstance.Properties["intProp"] = 42;
            originalInstance.Properties["boolProp"] = true;
            originalInstance.Properties["objectProp"] = new { Name = "Original", Value = 100 };

            try
            {
                // Create the original instance
                await storage.CreateAsync(originalInstance);

                // Update with completely new properties
                var updatedInstance = new ProductBundleInstance(
                    originalInstance.Id,
                    originalInstance.ProductBundleId,
                    "1.1.0"); // Only version changed
                updatedInstance.Properties["stringProp"] = "updated";
                updatedInstance.Properties["intProp"] = 84;
                updatedInstance.Properties["boolProp"] = false;
                updatedInstance.Properties["newStringProp"] = "brand new";
                // Note: objectProp is removed in the update

                // Act
                var result = await storage.UpdateAsync(updatedInstance);

                // Assert
                Assert.IsTrue(result, "UpdateAsync should return true");

                // Verify all changes
                var retrievedInstance = await storage.GetAsync(originalInstance.Id);
                Assert.IsNotNull(retrievedInstance);
                Assert.AreEqual("1.1.0", retrievedInstance.ProductBundleVersion);
                Assert.AreEqual("updated", retrievedInstance.Properties["stringProp"]);
                Assert.AreEqual(84, retrievedInstance.Properties["intProp"]);
                Assert.AreEqual(false, retrievedInstance.Properties["boolProp"]);
                Assert.AreEqual("brand new", retrievedInstance.Properties["newStringProp"]);
                Assert.IsFalse(retrievedInstance.Properties.ContainsKey("objectProp"), "Removed property should not exist");
            }
            finally
            {
                // Cleanup
                try
                {
                    await storage.DeleteAsync(originalInstance.Id);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task UpdateAsync_WithNullInstance_ShouldThrowArgumentNullException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.UpdateAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateAsync_WithNullId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            var instance = new ProductBundleInstance(
                null!, // Null ID
                "test-plugin",
                "1.0.0");

            // Act
            await storage.UpdateAsync(instance);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateAsync_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            var instance = new ProductBundleInstance(
                "", // Empty ID
                "test-plugin",
                "1.0.0");

            // Act
            await storage.UpdateAsync(instance);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task UpdateAsync_WithWhitespaceId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);
            
            var instance = new ProductBundleInstance(
                "   ", // Whitespace ID
                "test-plugin",
                "1.0.0");

            // Act
            await storage.UpdateAsync(instance);
        }

        #endregion

        #region DeleteAsync Tests

        [TestMethod]
        public async Task DeleteAsync_WithExistingInstance_ShouldReturnTrueAndDeleteInstance()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_delete_existing_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var instance = new ProductBundleInstance(
                "delete-test-" + Guid.NewGuid(),
                "test-bundle",
                "1.0.0")
            {
                Properties = new Dictionary<string, object?>
                {
                    ["TestProperty"] = "TestValue",
                    ["NumberProperty"] = 42
                }
            };

            await storage.CreateAsync(instance);

            // Act
            var result = await storage.DeleteAsync(instance.Id);

            // Assert
            Assert.IsTrue(result, "DeleteAsync should return true for existing instance");

            // Verify instance is actually deleted
            var deletedInstance = await storage.GetAsync(instance.Id);
            Assert.IsNull(deletedInstance, "Instance should be null after deletion");
        }

        [TestMethod]
        public async Task DeleteAsync_WithNonExistentInstance_ShouldReturnFalse()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_delete_nonexistent_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var nonExistentId = "non-existent-delete-test-" + Guid.NewGuid();

            // Act
            var result = await storage.DeleteAsync(nonExistentId);

            // Assert
            Assert.IsFalse(result, "DeleteAsync should return false for non-existent instance");
        }

        [TestMethod]
        public async Task DeleteAsync_AfterSuccessfulDeletion_ShouldNotAffectOtherInstances()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_delete_isolation_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var instance1 = new ProductBundleInstance(
                "delete-test-keep-1-" + Guid.NewGuid(),
                "test-bundle",
                "1.0.0")
            {
                Properties = new Dictionary<string, object?> { ["Keep"] = "Instance1" }
            };
            var instance2 = new ProductBundleInstance(
                "delete-test-remove-2-" + Guid.NewGuid(),
                "test-bundle",
                "1.0.0")
            {
                Properties = new Dictionary<string, object?> { ["Remove"] = "Instance2" }
            };
            var instance3 = new ProductBundleInstance(
                "delete-test-keep-3-" + Guid.NewGuid(),
                "test-bundle",
                "1.0.0")
            {
                Properties = new Dictionary<string, object?> { ["Keep"] = "Instance3" }
            };

            await storage.CreateAsync(instance1);
            await storage.CreateAsync(instance2);
            await storage.CreateAsync(instance3);

            // Act
            var result = await storage.DeleteAsync(instance2.Id);

            // Assert
            Assert.IsTrue(result, "DeleteAsync should return true for existing instance");

            // Verify correct instance is deleted
            var deletedInstance = await storage.GetAsync(instance2.Id);
            Assert.IsNull(deletedInstance, "Deleted instance should be null");

            // Verify other instances are still present
            var remainingInstance1 = await storage.GetAsync(instance1.Id);
            var remainingInstance3 = await storage.GetAsync(instance3.Id);

            Assert.IsNotNull(remainingInstance1, "Other instances should remain after deletion");
            Assert.IsNotNull(remainingInstance3, "Other instances should remain after deletion");
            Assert.AreEqual(instance1.Id, remainingInstance1.Id);
            Assert.AreEqual(instance3.Id, remainingInstance3.Id);
            Assert.AreEqual("Instance1", remainingInstance1.Properties["Keep"]);
            Assert.AreEqual("Instance3", remainingInstance3.Properties["Keep"]);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteAsync_WithNullId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.DeleteAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteAsync_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.DeleteAsync(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeleteAsync_WithWhitespaceId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.DeleteAsync("   ");
        }

        #endregion

        #region ExistsAsync Tests

        [TestMethod]
        public async Task ExistsAsync_WithExistingInstance_ShouldReturnTrue()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_exists_existing_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var instance = new ProductBundleInstance(
                "exists-test-" + Guid.NewGuid(),
                "test-bundle",
                "1.0.0")
            {
                Properties = new Dictionary<string, object?>
                {
                    ["TestProperty"] = "TestValue"
                }
            };

            await storage.CreateAsync(instance);

            // Act
            var exists = await storage.ExistsAsync(instance.Id);

            // Assert
            Assert.IsTrue(exists, "ExistsAsync should return true for existing instance");
        }

        [TestMethod]
        public async Task ExistsAsync_WithNonExistentInstance_ShouldReturnFalse()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_exists_nonexistent_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var nonExistentId = "non-existent-exists-test-" + Guid.NewGuid();

            // Act
            var exists = await storage.ExistsAsync(nonExistentId);

            // Assert
            Assert.IsFalse(exists, "ExistsAsync should return false for non-existent instance");
        }

        [TestMethod]
        public async Task ExistsAsync_AfterDeletion_ShouldReturnFalse()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_exists_after_delete_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var instance = new ProductBundleInstance(
                "exists-delete-test-" + Guid.NewGuid(),
                "test-bundle",
                "1.0.0");

            await storage.CreateAsync(instance);

            // Verify instance exists before deletion
            var existsBeforeDeletion = await storage.ExistsAsync(instance.Id);
            Assert.IsTrue(existsBeforeDeletion, "Instance should exist before deletion");

            // Delete the instance
            await storage.DeleteAsync(instance.Id);

            // Act
            var existsAfterDeletion = await storage.ExistsAsync(instance.Id);

            // Assert
            Assert.IsFalse(existsAfterDeletion, "ExistsAsync should return false after instance deletion");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ExistsAsync_WithNullId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.ExistsAsync(null!);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ExistsAsync_WithEmptyId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.ExistsAsync(string.Empty);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task ExistsAsync_WithWhitespaceId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName,
                _logger);

            // Act
            await storage.ExistsAsync("   ");
        }

        #endregion

        #region GetCountAsync Tests

        [TestMethod]
        public async Task GetCountAsync_WithEmptyCollection_ShouldReturnZero()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_empty_" + Guid.NewGuid().ToString("N")[..8],
                _logger);

            // Act
            var count = await storage.GetCountAsync();

            // Assert
            Assert.AreEqual(0, count, "GetCountAsync should return 0 for empty collection");
        }

        [TestMethod]
        public async Task GetCountAsync_WithSingleInstance_ShouldReturnOne()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_single_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var instance = new ProductBundleInstance(
                "count-test-single-" + Guid.NewGuid(),
                "test-bundle",
                "1.0.0");

            await storage.CreateAsync(instance);

            // Act
            var count = await storage.GetCountAsync();

            // Assert
            Assert.AreEqual(1, count, "GetCountAsync should return 1 for collection with one instance");
        }

        [TestMethod]
        public async Task GetCountAsync_WithMultipleInstances_ShouldReturnCorrectCount()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_multiple_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var instances = new List<ProductBundleInstance>();
            for (int i = 0; i < 5; i++)
            {
                var instance = new ProductBundleInstance(
                    $"count-test-multiple-{i}-" + Guid.NewGuid(),
                    "test-bundle",
                    "1.0.0")
                {
                    Properties = new Dictionary<string, object?> { ["Index"] = i }
                };
                instances.Add(instance);
                await storage.CreateAsync(instance);
            }

            // Act
            var count = await storage.GetCountAsync();

            // Assert
            Assert.AreEqual(5, count, "GetCountAsync should return 5 for collection with five instances");
        }

        [TestMethod]
        public async Task GetCountAsync_AfterDeletion_ShouldReturnUpdatedCount()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_after_delete_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            // Create 3 instances
            var instances = new List<ProductBundleInstance>();
            for (int i = 0; i < 3; i++)
            {
                var instance = new ProductBundleInstance(
                    $"count-delete-test-{i}-" + Guid.NewGuid(),
                    "test-bundle",
                    "1.0.0");
                instances.Add(instance);
                await storage.CreateAsync(instance);
            }

            // Verify initial count
            var initialCount = await storage.GetCountAsync();
            Assert.AreEqual(3, initialCount, "Initial count should be 3");

            // Delete one instance
            await storage.DeleteAsync(instances[1].Id);

            // Act
            var finalCount = await storage.GetCountAsync();

            // Assert
            Assert.AreEqual(2, finalCount, "GetCountAsync should return 2 after deleting one instance");
        }

        [TestMethod]
        public async Task GetCountAsync_WithMixedProductBundleIds_ShouldReturnTotalCount()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_mixed_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            // Create instances with different ProductBundle IDs
            var instance1 = new ProductBundleInstance(
                "count-mixed-1-" + Guid.NewGuid(),
                "bundle-a",
                "1.0.0");
            var instance2 = new ProductBundleInstance(
                "count-mixed-2-" + Guid.NewGuid(),
                "bundle-b",
                "1.0.0");
            var instance3 = new ProductBundleInstance(
                "count-mixed-3-" + Guid.NewGuid(),
                "bundle-a",
                "2.0.0");

            await storage.CreateAsync(instance1);
            await storage.CreateAsync(instance2);
            await storage.CreateAsync(instance3);

            // Act
            var count = await storage.GetCountAsync();

            // Assert
            Assert.AreEqual(3, count, "GetCountAsync should return total count regardless of ProductBundle ID");
        }

        #endregion

        #region GetCountByProductBundleIdAsync Tests

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithNonExistentProductBundleId_ShouldReturnZero()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_by_id_empty_" + Guid.NewGuid().ToString("N")[..8],
                _logger);

            // Act
            var count = await storage.GetCountByProductBundleIdAsync("non-existent-bundle");

            // Assert
            Assert.AreEqual(0, count, "GetCountByProductBundleIdAsync should return 0 for non-existent ProductBundle ID");
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithSingleMatchingInstance_ShouldReturnOne()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_by_id_single_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var productBundleId = "test-bundle-" + Guid.NewGuid();
            var instance = new ProductBundleInstance(
                "count-by-id-test-single-" + Guid.NewGuid(),
                productBundleId,
                "1.0.0");

            await storage.CreateAsync(instance);

            // Act
            var count = await storage.GetCountByProductBundleIdAsync(productBundleId);

            // Assert
            Assert.AreEqual(1, count, "GetCountByProductBundleIdAsync should return 1 for ProductBundle ID with one instance");
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithMultipleMatchingInstances_ShouldReturnCorrectCount()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_by_id_multiple_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var productBundleId = "test-bundle-" + Guid.NewGuid();
            var instances = new List<ProductBundleInstance>();
            
            // Create 4 instances with the same ProductBundle ID
            for (int i = 0; i < 4; i++)
            {
                var instance = new ProductBundleInstance(
                    $"count-by-id-multiple-{i}-" + Guid.NewGuid(),
                    productBundleId,
                    "1.0.0")
                {
                    Properties = new Dictionary<string, object?> { ["Index"] = i }
                };
                instances.Add(instance);
                await storage.CreateAsync(instance);
            }

            // Act
            var count = await storage.GetCountByProductBundleIdAsync(productBundleId);

            // Assert
            Assert.AreEqual(4, count, "GetCountByProductBundleIdAsync should return 4 for ProductBundle ID with four instances");
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithMixedProductBundleIds_ShouldReturnCorrectCount()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_by_id_mixed_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var targetBundleId = "target-bundle-" + Guid.NewGuid();
            var otherBundleId = "other-bundle-" + Guid.NewGuid();
            
            // Create 3 instances with target ProductBundle ID
            for (int i = 0; i < 3; i++)
            {
                var instance = new ProductBundleInstance(
                    $"count-mixed-target-{i}-" + Guid.NewGuid(),
                    targetBundleId,
                    "1.0.0");
                await storage.CreateAsync(instance);
            }
            
            // Create 2 instances with different ProductBundle ID
            for (int i = 0; i < 2; i++)
            {
                var instance = new ProductBundleInstance(
                    $"count-mixed-other-{i}-" + Guid.NewGuid(),
                    otherBundleId,
                    "1.0.0");
                await storage.CreateAsync(instance);
            }

            // Act
            var targetCount = await storage.GetCountByProductBundleIdAsync(targetBundleId);
            var otherCount = await storage.GetCountByProductBundleIdAsync(otherBundleId);

            // Assert
            Assert.AreEqual(3, targetCount, "GetCountByProductBundleIdAsync should return 3 for target ProductBundle ID");
            Assert.AreEqual(2, otherCount, "GetCountByProductBundleIdAsync should return 2 for other ProductBundle ID");
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_AfterDeletion_ShouldReturnUpdatedCount()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_by_id_after_delete_" + Guid.NewGuid().ToString("N")[..8],
                _logger);
            
            var productBundleId = "test-bundle-" + Guid.NewGuid();
            var instances = new List<ProductBundleInstance>();
            
            // Create 3 instances with the same ProductBundle ID
            for (int i = 0; i < 3; i++)
            {
                var instance = new ProductBundleInstance(
                    $"count-by-id-delete-{i}-" + Guid.NewGuid(),
                    productBundleId,
                    "1.0.0");
                instances.Add(instance);
                await storage.CreateAsync(instance);
            }

            // Verify initial count
            var initialCount = await storage.GetCountByProductBundleIdAsync(productBundleId);
            Assert.AreEqual(3, initialCount, "Initial count should be 3");

            // Delete one instance
            await storage.DeleteAsync(instances[1].Id);

            // Act
            var finalCount = await storage.GetCountByProductBundleIdAsync(productBundleId);

            // Assert
            Assert.AreEqual(2, finalCount, "GetCountByProductBundleIdAsync should return 2 after deleting one instance");
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithNullProductBundleId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_by_id_null_" + Guid.NewGuid().ToString("N")[..8],
                _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await storage.GetCountByProductBundleIdAsync(null!),
                "GetCountByProductBundleIdAsync should throw ArgumentException for null ProductBundle ID");
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithEmptyProductBundleId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_by_id_empty_" + Guid.NewGuid().ToString("N")[..8],
                _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await storage.GetCountByProductBundleIdAsync(string.Empty),
                "GetCountByProductBundleIdAsync should throw ArgumentException for empty ProductBundle ID");
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithWhitespaceProductBundleId_ShouldThrowArgumentException()
        {
            // Arrange
            var storage = new MongoProductBundleInstanceStorage(
                TestConnectionString,
                TestDatabaseName,
                TestCollectionName + "_count_by_id_whitespace_" + Guid.NewGuid().ToString("N")[..8],
                _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(
                async () => await storage.GetCountByProductBundleIdAsync("   "),
                "GetCountByProductBundleIdAsync should throw ArgumentException for whitespace ProductBundle ID");
        }

        #endregion
    }
}
