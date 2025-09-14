using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using ProductBundles.Core.Extensions;
using ProductBundles.Sdk;

namespace ProductBundles.UnitTests.Extensions
{
    /// <summary>
    /// Unit tests for ServiceCollectionExtensions MongoDB infrastructure methods
    /// </summary>
    [TestClass]
    public class ServiceCollectionExtensionsMongoInfrastructureTests
    {
        private IServiceCollection _services = null!;

        [TestInitialize]
        public void Setup()
        {
            _services = new ServiceCollection();
        }

        #region AddMongoClient Tests

        [TestMethod]
        public void AddMongoClient_WithValidConnectionString_RegistersMongoClient()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";

            // Act
            var result = _services.AddMongoClient(connectionString);

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var mongoClient = serviceProvider.GetService<IMongoClient>();
            
            Assert.IsNotNull(mongoClient, "IMongoClient should be registered");
            Assert.IsInstanceOfType(mongoClient, typeof(MongoClient), "Should register MongoClient implementation");
        }

        [TestMethod]
        public void AddMongoClient_WithNullConnectionString_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddMongoClient(null!));

            Assert.AreEqual("connectionString", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Connection string cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddMongoClient_WithEmptyConnectionString_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddMongoClient(string.Empty));

            Assert.AreEqual("connectionString", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Connection string cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddMongoClient_WithWhitespaceConnectionString_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddMongoClient("   "));

            Assert.AreEqual("connectionString", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Connection string cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddMongoClient_CalledMultipleTimes_RegistersOnlyOnce()
        {
            // Arrange
            const string connectionString1 = "mongodb://localhost:27017";
            const string connectionString2 = "mongodb://localhost:27018";

            // Act
            _services.AddMongoClient(connectionString1);
            _services.AddMongoClient(connectionString2);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var mongoClients = serviceProvider.GetServices<IMongoClient>().ToList();
            
            Assert.AreEqual(1, mongoClients.Count, "Should register MongoClient only once due to TryAddSingleton");
        }

        [TestMethod]
        public void AddMongoClient_RegisteredAsSingleton_ReturnsSameInstance()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            _services.AddMongoClient(connectionString);
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var client1 = serviceProvider.GetService<IMongoClient>();
            var client2 = serviceProvider.GetService<IMongoClient>();

            // Assert
            Assert.AreSame(client1, client2, "MongoClient should be registered as singleton");
        }

        #endregion

        #region AddMongoDatabase Tests

        [TestMethod]
        public void AddMongoDatabase_WithValidDatabaseName_RegistersMongoDatabase()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            const string databaseName = "testdb";
            _services.AddMongoClient(connectionString);

            // Act
            var result = _services.AddMongoDatabase(databaseName);

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var mongoDatabase = serviceProvider.GetService<IMongoDatabase>();
            
            Assert.IsNotNull(mongoDatabase, "IMongoDatabase should be registered");
            Assert.AreEqual(databaseName, mongoDatabase.DatabaseNamespace.DatabaseName, "Database name should match");
        }

        [TestMethod]
        public void AddMongoDatabase_WithNullDatabaseName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddMongoDatabase(null!));

            Assert.AreEqual("databaseName", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Database name cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddMongoDatabase_WithEmptyDatabaseName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddMongoDatabase(string.Empty));

            Assert.AreEqual("databaseName", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Database name cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddMongoDatabase_WithWhitespaceDatabaseName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddMongoDatabase("   "));

            Assert.AreEqual("databaseName", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Database name cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddMongoDatabase_CalledMultipleTimes_RegistersOnlyOnce()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            const string databaseName1 = "testdb1";
            const string databaseName2 = "testdb2";
            _services.AddMongoClient(connectionString);

            // Act
            _services.AddMongoDatabase(databaseName1);
            _services.AddMongoDatabase(databaseName2);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var mongoDatabases = serviceProvider.GetServices<IMongoDatabase>().ToList();
            
            Assert.AreEqual(1, mongoDatabases.Count, "Should register MongoDatabase only once due to TryAddSingleton");
            Assert.AreEqual(databaseName1, mongoDatabases[0].DatabaseNamespace.DatabaseName, "Should keep first registered database");
        }

        [TestMethod]
        public void AddMongoDatabase_RegisteredAsSingleton_ReturnsSameInstance()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            const string databaseName = "testdb";
            _services.AddMongoClient(connectionString);
            _services.AddMongoDatabase(databaseName);
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var database1 = serviceProvider.GetService<IMongoDatabase>();
            var database2 = serviceProvider.GetService<IMongoDatabase>();

            // Assert
            Assert.AreSame(database1, database2, "MongoDatabase should be registered as singleton");
        }

        #endregion

        #region AddMongoCollection Tests

        [TestMethod]
        public void AddMongoCollection_WithValidCollectionName_RegistersMongoCollection()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            const string databaseName = "testdb";
            const string collectionName = "testcollection";
            _services.AddMongoClient(connectionString);
            _services.AddMongoDatabase(databaseName);

            // Act
            var result = _services.AddMongoCollection(collectionName);

            // Assert
            Assert.AreSame(_services, result, "Method should return the same service collection for chaining");
            
            var serviceProvider = _services.BuildServiceProvider();
            var mongoCollection = serviceProvider.GetService<IMongoCollection<ProductBundleInstance>>();
            
            Assert.IsNotNull(mongoCollection, "IMongoCollection<ProductBundleInstance> should be registered");
            Assert.AreEqual(collectionName, mongoCollection.CollectionNamespace.CollectionName, "Collection name should match");
        }

        [TestMethod]
        public void AddMongoCollection_WithNullCollectionName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddMongoCollection(null!));

            Assert.AreEqual("collectionName", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Collection name cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddMongoCollection_WithEmptyCollectionName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddMongoCollection(string.Empty));

            Assert.AreEqual("collectionName", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Collection name cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddMongoCollection_WithWhitespaceCollectionName_ThrowsArgumentException()
        {
            // Act & Assert
            var exception = Assert.ThrowsException<ArgumentException>(() =>
                _services.AddMongoCollection("   "));

            Assert.AreEqual("collectionName", exception.ParamName, "Should specify correct parameter name");
            StringAssert.Contains(exception.Message, "Collection name cannot be null or empty", "Should have descriptive error message");
        }

        [TestMethod]
        public void AddMongoCollection_CalledMultipleTimes_RegistersOnlyOnce()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            const string databaseName = "testdb";
            const string collectionName1 = "testcollection1";
            const string collectionName2 = "testcollection2";
            _services.AddMongoClient(connectionString);
            _services.AddMongoDatabase(databaseName);

            // Act
            _services.AddMongoCollection(collectionName1);
            _services.AddMongoCollection(collectionName2);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            var mongoCollections = serviceProvider.GetServices<IMongoCollection<ProductBundleInstance>>().ToList();
            
            Assert.AreEqual(1, mongoCollections.Count, "Should register MongoCollection only once due to TryAddSingleton");
            Assert.AreEqual(collectionName1, mongoCollections[0].CollectionNamespace.CollectionName, "Should keep first registered collection");
        }

        [TestMethod]
        public void AddMongoCollection_RegisteredAsSingleton_ReturnsSameInstance()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            const string databaseName = "testdb";
            const string collectionName = "testcollection";
            _services.AddMongoClient(connectionString);
            _services.AddMongoDatabase(databaseName);
            _services.AddMongoCollection(collectionName);
            var serviceProvider = _services.BuildServiceProvider();

            // Act
            var collection1 = serviceProvider.GetService<IMongoCollection<ProductBundleInstance>>();
            var collection2 = serviceProvider.GetService<IMongoCollection<ProductBundleInstance>>();

            // Assert
            Assert.AreSame(collection1, collection2, "MongoCollection should be registered as singleton");
        }

        #endregion

        #region Integration Tests

        [TestMethod]
        public void MongoInfrastructureServices_WorkTogether_RegistersAllServices()
        {
            // Arrange
            const string connectionString = "mongodb://localhost:27017";
            const string databaseName = "testdb";
            const string collectionName = "testcollection";

            // Act
            _services
                .AddMongoClient(connectionString)
                .AddMongoDatabase(databaseName)
                .AddMongoCollection(collectionName);

            // Assert
            var serviceProvider = _services.BuildServiceProvider();
            
            var mongoClient = serviceProvider.GetService<IMongoClient>();
            var mongoDatabase = serviceProvider.GetService<IMongoDatabase>();
            var mongoCollection = serviceProvider.GetService<IMongoCollection<ProductBundleInstance>>();
            
            Assert.IsNotNull(mongoClient, "IMongoClient should be registered");
            Assert.IsNotNull(mongoDatabase, "IMongoDatabase should be registered");
            Assert.IsNotNull(mongoCollection, "IMongoCollection should be registered");
            
            Assert.AreEqual(databaseName, mongoDatabase.DatabaseNamespace.DatabaseName, "Database name should match");
            Assert.AreEqual(collectionName, mongoCollection.CollectionNamespace.CollectionName, "Collection name should match");
        }

        #endregion
    }
}
