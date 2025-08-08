using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProductBundles.Core.Serialization;
using ProductBundles.Core.Storage;
using ProductBundles.Sdk;

namespace ProductBundles.UnitTests
{
    [TestClass]
    public class FileSystemProductBundleInstanceStorageTests
    {
        private string _tempDirectory = null!;
        private IProductBundleInstanceSerializer _serializer = null!;
        private ILogger<FileSystemProductBundleInstanceStorage> _logger = null!;

        [TestInitialize]
        public void Setup()
        {
            // Create a temporary directory for each test
            _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            _serializer = new JsonProductBundleInstanceSerializer();
            _logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<FileSystemProductBundleInstanceStorage>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up temporary directory after each test
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, true);
            }
        }

        [TestMethod]
        public void Constructor_ValidParameters_InitializesCorrectly()
        {
            // Act
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Assert
            Assert.IsNotNull(storage);
            Assert.IsTrue(Directory.Exists(_tempDirectory), "Storage directory should be created");
        }

        [TestMethod]
        public void Constructor_NullStorageDirectory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new FileSystemProductBundleInstanceStorage(null!, _serializer, _logger));
        }

        [TestMethod]
        public void Constructor_NullSerializer_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                new FileSystemProductBundleInstanceStorage(_tempDirectory, null!, _logger));
        }

        [TestMethod]
        public void Constructor_NullLogger_UsesNullLogger()
        {
            // Act
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, null);

            // Assert
            Assert.IsNotNull(storage);
            Assert.IsTrue(Directory.Exists(_tempDirectory), "Storage directory should be created");
        }

        [TestMethod]
        public async Task CreateAsync_ValidInstance_ReturnsTrue()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance = new ProductBundleInstance("test-id", "bundle-id", "1.0.0");

            // Act
            var result = await storage.CreateAsync(instance);

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(await storage.ExistsAsync("test-id"));
        }

        [TestMethod]
        public async Task CreateAsync_NullInstance_ThrowsArgumentNullException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                storage.CreateAsync(null!));
        }

        [TestMethod]
        public async Task CreateAsync_EmptyInstanceId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance = new ProductBundleInstance("", "bundle-id", "1.0.0");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.CreateAsync(instance));
        }

        [TestMethod]
        public async Task CreateAsync_DuplicateInstance_ReturnsFalse()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance = new ProductBundleInstance("test-id", "bundle-id", "1.0.0");

            // Act
            var firstResult = await storage.CreateAsync(instance);
            var secondResult = await storage.CreateAsync(instance);

            // Assert
            Assert.IsTrue(firstResult);
            Assert.IsFalse(secondResult);
        }

        [TestMethod]
        public async Task GetAsync_ExistingInstance_ReturnsInstance()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var originalInstance = new ProductBundleInstance("test-id", "bundle-id", "1.0.0");
            originalInstance.Properties["testProperty"] = "testValue";
            await storage.CreateAsync(originalInstance);

            // Act
            var retrievedInstance = await storage.GetAsync("test-id");

            // Assert
            Assert.IsNotNull(retrievedInstance);
            Assert.AreEqual("test-id", retrievedInstance!.Id);
            Assert.AreEqual("bundle-id", retrievedInstance.ProductBundleId);
            Assert.AreEqual("1.0.0", retrievedInstance.ProductBundleVersion);
            Assert.AreEqual("testValue", retrievedInstance.Properties["testProperty"]);
        }

        [TestMethod]
        public async Task GetAsync_NonExistentInstance_ReturnsNull()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act
            var result = await storage.GetAsync("non-existent-id");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task GetAsync_EmptyId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetAsync(""));
        }

        [TestMethod]
        public async Task GetAsync_NullId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetAsync(null!));
        }

        [TestMethod]
        public async Task UpdateAsync_ExistingInstance_ReturnsTrue()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var originalInstance = new ProductBundleInstance("test-id", "bundle-id", "1.0.0");
            originalInstance.Properties["originalProperty"] = "originalValue";
            await storage.CreateAsync(originalInstance);

            var updatedInstance = new ProductBundleInstance("test-id", "bundle-id", "2.0.0");
            updatedInstance.Properties["updatedProperty"] = "updatedValue";

            // Act
            var result = await storage.UpdateAsync(updatedInstance);

            // Assert
            Assert.IsTrue(result);
            
            var retrievedInstance = await storage.GetAsync("test-id");
            Assert.IsNotNull(retrievedInstance);
            Assert.AreEqual("2.0.0", retrievedInstance!.ProductBundleVersion);
            Assert.AreEqual("updatedValue", retrievedInstance.Properties["updatedProperty"]);
            Assert.IsFalse(retrievedInstance.Properties.ContainsKey("originalProperty"));
        }

        [TestMethod]
        public async Task UpdateAsync_NonExistentInstance_ReturnsFalse()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance = new ProductBundleInstance("non-existent-id", "bundle-id", "1.0.0");

            // Act
            var result = await storage.UpdateAsync(instance);

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateAsync_NullInstance_ThrowsArgumentNullException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                storage.UpdateAsync(null!));
        }

        [TestMethod]
        public async Task UpdateAsync_EmptyInstanceId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance = new ProductBundleInstance("", "bundle-id", "1.0.0");

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.UpdateAsync(instance));
        }

        [TestMethod]
        public async Task DeleteAsync_ExistingInstance_ReturnsTrue()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance = new ProductBundleInstance("test-id", "bundle-id", "1.0.0");
            await storage.CreateAsync(instance);

            // Act
            var result = await storage.DeleteAsync("test-id");

            // Assert
            Assert.IsTrue(result);
            Assert.IsFalse(await storage.ExistsAsync("test-id"));
        }

        [TestMethod]
        public async Task DeleteAsync_NonExistentInstance_ReturnsFalse()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act
            var result = await storage.DeleteAsync("non-existent-id");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task DeleteAsync_EmptyId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.DeleteAsync(""));
        }

        [TestMethod]
        public async Task DeleteAsync_NullId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.DeleteAsync(null!));
        }

        [TestMethod]
        public async Task ExistsAsync_ExistingInstance_ReturnsTrue()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance = new ProductBundleInstance("test-id", "bundle-id", "1.0.0");
            await storage.CreateAsync(instance);

            // Act
            var result = await storage.ExistsAsync("test-id");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task ExistsAsync_NonExistentInstance_ReturnsFalse()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act
            var result = await storage.ExistsAsync("non-existent-id");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task ExistsAsync_EmptyId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.ExistsAsync(""));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithMatchingInstances_ReturnsFilteredInstances()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance1 = new ProductBundleInstance("id1", "bundle-a", "1.0.0");
            var instance2 = new ProductBundleInstance("id2", "bundle-b", "2.0.0");
            var instance3 = new ProductBundleInstance("id3", "bundle-a", "1.5.0");
            
            await storage.CreateAsync(instance1);
            await storage.CreateAsync(instance2);
            await storage.CreateAsync(instance3);

            // Act
            var result = await storage.GetByProductBundleIdAsync("bundle-a");

            // Assert
            Assert.IsNotNull(result);
            var instances = result.ToList();
            Assert.AreEqual(2, instances.Count);
            
            foreach (var instance in instances)
            {
                Assert.AreEqual("bundle-a", instance.ProductBundleId);
            }
            
            var ids = instances.Select(i => i.Id).OrderBy(id => id).ToList();
            CollectionAssert.AreEquivalent(new[] { "id1", "id3" }, ids);
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithNoMatchingInstances_ReturnsEmptyCollection()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance1 = new ProductBundleInstance("id1", "bundle-a", "1.0.0");
            await storage.CreateAsync(instance1);

            // Act
            var result = await storage.GetByProductBundleIdAsync("non-existent-bundle");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_EmptyProductBundleId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetByProductBundleIdAsync(""));
        }

        [TestMethod]
        public async Task GetCountAsync_EmptyStorage_ReturnsZero()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act
            var result = await storage.GetCountAsync();

            // Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public async Task GetCountAsync_WithInstances_ReturnsCorrectCount()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance1 = new ProductBundleInstance("id1", "bundle-a", "1.0.0");
            var instance2 = new ProductBundleInstance("id2", "bundle-b", "2.0.0");
            var instance3 = new ProductBundleInstance("id3", "bundle-a", "1.5.0");
            
            await storage.CreateAsync(instance1);
            await storage.CreateAsync(instance2);
            await storage.CreateAsync(instance3);

            // Act
            var result = await storage.GetCountAsync();

            // Assert
            Assert.AreEqual(3, result);
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithMatchingInstances_ReturnsCorrectCount()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance1 = new ProductBundleInstance("id1", "bundle-a", "1.0.0");
            var instance2 = new ProductBundleInstance("id2", "bundle-b", "2.0.0");
            var instance3 = new ProductBundleInstance("id3", "bundle-a", "1.5.0");
            
            await storage.CreateAsync(instance1);
            await storage.CreateAsync(instance2);
            await storage.CreateAsync(instance3);

            // Act
            var result = await storage.GetCountByProductBundleIdAsync("bundle-a");

            // Assert
            Assert.AreEqual(2, result);
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_WithNoMatchingInstances_ReturnsZero()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance1 = new ProductBundleInstance("id1", "bundle-a", "1.0.0");
            await storage.CreateAsync(instance1);

            // Act
            var result = await storage.GetCountByProductBundleIdAsync("non-existent-bundle");

            // Assert
            Assert.AreEqual(0, result);
        }

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_EmptyProductBundleId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetCountByProductBundleIdAsync(""));
        }

        [TestMethod]
        public async Task CreateAsync_InstanceWithComplexProperties_PersistsCorrectly()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance = new ProductBundleInstance("complex-id", "bundle-id", "1.0.0");
            instance.Properties["stringValue"] = "test string";
            instance.Properties["intValue"] = 42;
            instance.Properties["boolValue"] = true;
            instance.Properties["nullValue"] = null;
            instance.Properties["arrayValue"] = new[] { "item1", "item2" };
            instance.Properties["objectValue"] = new { Name = "test", Value = 123 };

            // Act
            var created = await storage.CreateAsync(instance);
            var retrieved = await storage.GetAsync("complex-id");

            // Assert
            Assert.IsTrue(created);
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("complex-id", retrieved.Id);
            Assert.AreEqual("bundle-id", retrieved.ProductBundleId);
            Assert.AreEqual("1.0.0", retrieved.ProductBundleVersion);
            Assert.AreEqual(6, retrieved.Properties.Count);
        }

        [TestMethod]
        public async Task CreateAsync_InstanceWithSpecialCharactersInId_SanitizesFileName()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instance = new ProductBundleInstance("special/id:with*chars?", "bundle-id", "1.0.0");
            instance.Properties["data"] = "test data";

            // Act
            var created = await storage.CreateAsync(instance);
            var retrieved = await storage.GetAsync("special/id:with*chars?");
            var exists = await storage.ExistsAsync("special/id:with*chars?");

            // Assert
            Assert.IsTrue(created);
            Assert.IsNotNull(retrieved);
            Assert.IsTrue(exists);
            Assert.AreEqual("special/id:with*chars?", retrieved.Id);
            Assert.AreEqual("test data", retrieved.Properties["data"]);
        }

        [TestMethod]
        public async Task FullWorkflow_CreateUpdateDeleteSequence_WorksCorrectly()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var originalInstance = new ProductBundleInstance("workflow-id", "bundle-id", "1.0.0");
            originalInstance.Properties["originalValue"] = "original";

            // Act & Assert - Create
            var created = await storage.CreateAsync(originalInstance);
            Assert.IsTrue(created);
            Assert.IsTrue(await storage.ExistsAsync("workflow-id"));
            Assert.AreEqual(1, await storage.GetCountAsync());

            // Act & Assert - Read
            var retrieved = await storage.GetAsync("workflow-id");
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("original", retrieved.Properties["originalValue"]);

            // Act & Assert - Update
            retrieved.Properties["originalValue"] = "updated";
            retrieved.Properties["newValue"] = "new";
            var updated = await storage.UpdateAsync(retrieved);
            Assert.IsTrue(updated);

            var retrievedAfterUpdate = await storage.GetAsync("workflow-id");
            Assert.IsNotNull(retrievedAfterUpdate);
            Assert.AreEqual("updated", retrievedAfterUpdate.Properties["originalValue"]);
            Assert.AreEqual("new", retrievedAfterUpdate.Properties["newValue"]);

            // Act & Assert - Delete
            var deleted = await storage.DeleteAsync("workflow-id");
            Assert.IsTrue(deleted);
            Assert.IsFalse(await storage.ExistsAsync("workflow-id"));
            Assert.AreEqual(0, await storage.GetCountAsync());
            Assert.IsNull(await storage.GetAsync("workflow-id"));
        }

        [TestMethod]
        public async Task MultipleInstances_FilteringAndCounting_WorksCorrectly()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var instances = new[]
            {
                new ProductBundleInstance("plugin1-inst1", "plugin1", "1.0.0"),
                new ProductBundleInstance("plugin1-inst2", "plugin1", "1.1.0"),
                new ProductBundleInstance("plugin1-inst3", "plugin1", "2.0.0"),
                new ProductBundleInstance("plugin2-inst1", "plugin2", "1.0.0"),
                new ProductBundleInstance("plugin2-inst2", "plugin2", "1.0.0"),
                new ProductBundleInstance("plugin3-inst1", "plugin3", "3.0.0")
            };

            // Create all instances
            foreach (var instance in instances)
            {
                await storage.CreateAsync(instance);
            }

            // Act & Assert - Total counts
            Assert.AreEqual(6, await storage.GetCountAsync());

            // Act & Assert - Plugin1 filtering
            var plugin1Instances = await storage.GetByProductBundleIdAsync("plugin1");
            Assert.AreEqual(3, plugin1Instances.Count());
            Assert.AreEqual(3, await storage.GetCountByProductBundleIdAsync("plugin1"));
            Assert.IsTrue(plugin1Instances.All(i => i.ProductBundleId == "plugin1"));

            // Act & Assert - Plugin2 filtering
            var plugin2Instances = await storage.GetByProductBundleIdAsync("plugin2");
            Assert.AreEqual(2, plugin2Instances.Count());
            Assert.AreEqual(2, await storage.GetCountByProductBundleIdAsync("plugin2"));
            Assert.IsTrue(plugin2Instances.All(i => i.ProductBundleId == "plugin2"));

            // Act & Assert - Plugin3 filtering
            var plugin3Instances = await storage.GetByProductBundleIdAsync("plugin3");
            Assert.AreEqual(1, plugin3Instances.Count());
            Assert.AreEqual(1, await storage.GetCountByProductBundleIdAsync("plugin3"));
            Assert.IsTrue(plugin3Instances.All(i => i.ProductBundleId == "plugin3"));

            // Act & Assert - Non-existent plugin filtering
            var nonExistentInstances = await storage.GetByProductBundleIdAsync("non-existent");
            Assert.AreEqual(0, nonExistentInstances.Count());
            Assert.AreEqual(0, await storage.GetCountByProductBundleIdAsync("non-existent"));
        }

        [TestMethod]
        public async Task UpdateAsync_PreservesInstanceIdentity_UpdatesOnlyProperties()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var originalInstance = new ProductBundleInstance("identity-test", "original-bundle", "1.0.0");
            originalInstance.Properties["key1"] = "value1";
            originalInstance.Properties["key2"] = "value2";
            await storage.CreateAsync(originalInstance);

            // Act - Attempt to update with different ID, ProductBundleId, and Version
            var updateInstance = new ProductBundleInstance("identity-test", "different-bundle", "2.0.0");
            updateInstance.Properties["key1"] = "updated-value1";
            updateInstance.Properties["key3"] = "new-value3";
            var updated = await storage.UpdateAsync(updateInstance);

            // Assert
            Assert.IsTrue(updated);
            var retrieved = await storage.GetAsync("identity-test");
            Assert.IsNotNull(retrieved);
            
            // ID should remain the same (it's the key)
            Assert.AreEqual("identity-test", retrieved.Id);
            
            // ProductBundleId and Version should be updated
            Assert.AreEqual("different-bundle", retrieved.ProductBundleId);
            Assert.AreEqual("2.0.0", retrieved.ProductBundleVersion);
            
            // Properties should be completely replaced
            Assert.AreEqual(2, retrieved.Properties.Count);
            Assert.AreEqual("updated-value1", retrieved.Properties["key1"]);
            Assert.AreEqual("new-value3", retrieved.Properties["key3"]);
            Assert.IsFalse(retrieved.Properties.ContainsKey("key2"));
        }

        [TestMethod]
        public async Task Storage_WithEmptyInstances_HandlesGracefully()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var emptyInstance = new ProductBundleInstance("empty-id", "", "");
            // Leave Properties as empty dictionary

            // Act & Assert
            var created = await storage.CreateAsync(emptyInstance);
            Assert.IsTrue(created);
            
            var retrieved = await storage.GetAsync("empty-id");
            Assert.IsNotNull(retrieved);
            Assert.AreEqual("empty-id", retrieved.Id);
            Assert.AreEqual("", retrieved.ProductBundleId);
            Assert.AreEqual("", retrieved.ProductBundleVersion);
            Assert.IsNotNull(retrieved.Properties);
            Assert.AreEqual(0, retrieved.Properties.Count);
        }

        [TestMethod]
        public async Task ExistsAsync_NullId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.ExistsAsync(null!));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_NullProductBundleId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetByProductBundleIdAsync(null!));
        }

        #region GetByProductBundleIdAsync Pagination Tests

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPagination_ReturnsCorrectPage()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            
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
            Assert.IsTrue(result.HasPreviousPage);
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationFirstPage_ReturnsCorrectMetadata()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            
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
            Assert.IsFalse(result.HasPreviousPage);
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationLastPage_ReturnsCorrectMetadata()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            
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
            Assert.IsTrue(result.HasPreviousPage);
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationNoMatches_ReturnsEmptyResult()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            
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
            Assert.IsFalse(result.HasPreviousPage);
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationNullProductBundleId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var paginationRequest = new PaginationRequest(1, 10);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetByProductBundleIdAsync(null!, paginationRequest));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationEmptyProductBundleId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            var paginationRequest = new PaginationRequest(1, 10);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetByProductBundleIdAsync("", paginationRequest));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithNullPaginationRequest_ThrowsArgumentNullException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(() =>
                storage.GetByProductBundleIdAsync("test-bundle", null!));
        }

        [TestMethod]
        public async Task GetByProductBundleIdAsync_WithPaginationMixedProductBundles_FiltersCorrectly()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);
            
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

        #endregion

        [TestMethod]
        public async Task GetCountByProductBundleIdAsync_NullProductBundleId_ThrowsArgumentException()
        {
            // Arrange
            var storage = new FileSystemProductBundleInstanceStorage(_tempDirectory, _serializer, _logger);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
                storage.GetCountByProductBundleIdAsync(null!));
        }
    }
}
