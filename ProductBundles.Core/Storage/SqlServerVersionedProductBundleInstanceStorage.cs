using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ProductBundles.Sdk;
using System.Text.Json;

namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// Versioned SQL Server implementation that maintains complete version history of ProductBundleInstance objects
    /// Uses a two-table strategy: current instances + version history for scalability
    /// </summary>
    public class SqlServerVersionedProductBundleInstanceStorage : IProductBundleInstanceStorage
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlServerVersionedProductBundleInstanceStorage> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public SqlServerVersionedProductBundleInstanceStorage(string connectionString, ILogger<SqlServerVersionedProductBundleInstanceStorage>? logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlServerVersionedProductBundleInstanceStorage>.Instance;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeDatabaseAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Current instances table (optimized for fast queries)
                var createCurrentTableSql = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProductBundleInstances' AND xtype='U')
                    CREATE TABLE ProductBundleInstances (
                        Id NVARCHAR(450) PRIMARY KEY,
                        ProductBundleId NVARCHAR(450) NOT NULL,
                        ProductBundleVersion NVARCHAR(100) NOT NULL,
                        JsonDocument NVARCHAR(MAX) NOT NULL,
                        VersionNumber BIGINT NOT NULL DEFAULT 1,
                        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
                        INDEX IX_ProductBundleInstances_ProductBundleId (ProductBundleId),
                        INDEX IX_ProductBundleInstances_CreatedAt (CreatedAt)
                    )";

                // Version history table (partitioned by date for scalability)
                var createVersionsTableSql = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProductBundleInstanceVersions' AND xtype='U')
                    CREATE TABLE ProductBundleInstanceVersions (
                        Id NVARCHAR(450) NOT NULL,
                        VersionNumber BIGINT NOT NULL,
                        ProductBundleId NVARCHAR(450) NOT NULL,
                        ProductBundleVersion NVARCHAR(100) NOT NULL,
                        JsonDocument NVARCHAR(MAX) NOT NULL,
                        ChangeType NVARCHAR(50) NOT NULL, -- CREATE, UPDATE, DELETE
                        ChangedBy NVARCHAR(255) NULL,
                        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                        PRIMARY KEY (Id, VersionNumber),
                        INDEX IX_ProductBundleInstanceVersions_ProductBundleId (ProductBundleId),
                        INDEX IX_ProductBundleInstanceVersions_CreatedAt (CreatedAt),
                        INDEX IX_ProductBundleInstanceVersions_ChangeType (ChangeType)
                    )";

                using var command1 = new SqlCommand(createCurrentTableSql, connection);
                await command1.ExecuteNonQueryAsync();

                using var command2 = new SqlCommand(createVersionsTableSql, connection);
                await command2.ExecuteNonQueryAsync();

                _logger.LogDebug("Versioned database tables initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize versioned database tables");
                throw;
            }
        }

        public async Task<bool> CreateAsync(ProductBundleInstance instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (string.IsNullOrEmpty(instance.Id)) throw new ArgumentException("Instance ID cannot be null or empty", nameof(instance));

            try
            {
                var jsonDocument = JsonSerializer.Serialize(instance, _jsonOptions);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Insert into current table
                    var insertCurrentSql = @"
                        INSERT INTO ProductBundleInstances (Id, ProductBundleId, ProductBundleVersion, JsonDocument, VersionNumber, CreatedAt, UpdatedAt)
                        VALUES (@Id, @ProductBundleId, @ProductBundleVersion, @JsonDocument, 1, GETUTCDATE(), GETUTCDATE())";

                    using var currentCommand = new SqlCommand(insertCurrentSql, connection, transaction);
                    currentCommand.Parameters.AddWithValue("@Id", instance.Id);
                    currentCommand.Parameters.AddWithValue("@ProductBundleId", instance.ProductBundleId ?? string.Empty);
                    currentCommand.Parameters.AddWithValue("@ProductBundleVersion", instance.ProductBundleVersion ?? string.Empty);
                    currentCommand.Parameters.AddWithValue("@JsonDocument", jsonDocument);

                    await currentCommand.ExecuteNonQueryAsync();

                    // Insert into versions table
                    await InsertVersionAsync(connection, transaction, instance.Id, 1, instance.ProductBundleId ?? string.Empty, 
                        instance.ProductBundleVersion ?? string.Empty, jsonDocument, "CREATE", null);

                    transaction.Commit();
                    _logger.LogInformation("Created versioned ProductBundleInstance with ID: {InstanceId}", instance.Id);
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                _logger.LogWarning("ProductBundleInstance with ID {InstanceId} already exists", instance.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create versioned ProductBundleInstance with ID: {InstanceId}", instance.Id);
                throw;
            }
        }

        public async Task<bool> UpdateAsync(ProductBundleInstance instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (string.IsNullOrEmpty(instance.Id)) throw new ArgumentException("Instance ID cannot be null or empty", nameof(instance));

            try
            {
                var jsonDocument = JsonSerializer.Serialize(instance, _jsonOptions);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Get current version number
                    var getVersionSql = "SELECT VersionNumber FROM ProductBundleInstances WHERE Id = @Id";
                    using var versionCommand = new SqlCommand(getVersionSql, connection, transaction);
                    versionCommand.Parameters.AddWithValue("@Id", instance.Id);
                    
                    var currentVersionObj = await versionCommand.ExecuteScalarAsync();
                    if (currentVersionObj == null)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    var newVersion = (long)currentVersionObj + 1;

                    // Update current table
                    var updateCurrentSql = @"
                        UPDATE ProductBundleInstances 
                        SET ProductBundleId = @ProductBundleId, 
                            ProductBundleVersion = @ProductBundleVersion, 
                            JsonDocument = @JsonDocument, 
                            VersionNumber = @VersionNumber,
                            UpdatedAt = GETUTCDATE()
                        WHERE Id = @Id";

                    using var updateCommand = new SqlCommand(updateCurrentSql, connection, transaction);
                    updateCommand.Parameters.AddWithValue("@Id", instance.Id);
                    updateCommand.Parameters.AddWithValue("@ProductBundleId", instance.ProductBundleId ?? string.Empty);
                    updateCommand.Parameters.AddWithValue("@ProductBundleVersion", instance.ProductBundleVersion ?? string.Empty);
                    updateCommand.Parameters.AddWithValue("@JsonDocument", jsonDocument);
                    updateCommand.Parameters.AddWithValue("@VersionNumber", newVersion);

                    var rowsAffected = await updateCommand.ExecuteNonQueryAsync();
                    if (rowsAffected == 0)
                    {
                        transaction.Rollback();
                        return false;
                    }

                    // Insert new version
                    await InsertVersionAsync(connection, transaction, instance.Id, newVersion, instance.ProductBundleId ?? string.Empty,
                        instance.ProductBundleVersion ?? string.Empty, jsonDocument, "UPDATE", null);

                    transaction.Commit();
                    _logger.LogInformation("Updated versioned ProductBundleInstance with ID: {InstanceId} to version {Version}", instance.Id, newVersion);
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update versioned ProductBundleInstance with ID: {InstanceId}", instance.Id);
                throw;
            }
        }

        private async Task InsertVersionAsync(SqlConnection connection, SqlTransaction transaction, string id, long versionNumber, 
            string productBundleId, string productBundleVersion, string jsonDocument, string changeType, string? changedBy)
        {
            var insertVersionSql = @"
                INSERT INTO ProductBundleInstanceVersions (Id, VersionNumber, ProductBundleId, ProductBundleVersion, JsonDocument, ChangeType, ChangedBy, CreatedAt)
                VALUES (@Id, @VersionNumber, @ProductBundleId, @ProductBundleVersion, @JsonDocument, @ChangeType, @ChangedBy, GETUTCDATE())";

            using var versionCommand = new SqlCommand(insertVersionSql, connection, transaction);
            versionCommand.Parameters.AddWithValue("@Id", id);
            versionCommand.Parameters.AddWithValue("@VersionNumber", versionNumber);
            versionCommand.Parameters.AddWithValue("@ProductBundleId", productBundleId);
            versionCommand.Parameters.AddWithValue("@ProductBundleVersion", productBundleVersion);
            versionCommand.Parameters.AddWithValue("@JsonDocument", jsonDocument);
            versionCommand.Parameters.AddWithValue("@ChangeType", changeType);
            versionCommand.Parameters.AddWithValue("@ChangedBy", changedBy ?? (object)DBNull.Value);

            await versionCommand.ExecuteNonQueryAsync();
        }

        // Implement remaining IProductBundleInstanceStorage methods...
        public async Task<ProductBundleInstance?> GetAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("ID cannot be null or empty", nameof(id));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT JsonDocument FROM ProductBundleInstances WHERE Id = @Id";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                var jsonDocument = await command.ExecuteScalarAsync() as string;
                if (jsonDocument == null) return null;

                return JsonSerializer.Deserialize<ProductBundleInstance>(jsonDocument, _jsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve ProductBundleInstance with ID: {InstanceId}", id);
                throw;
            }
        }

        public async Task<PaginatedResult<ProductBundleInstance>> GetByProductBundleIdAsync(string productBundleId, PaginationRequest paginationRequest)
        {
            if (string.IsNullOrEmpty(productBundleId)) throw new ArgumentException("ProductBundle ID cannot be null or empty", nameof(productBundleId));
            if (paginationRequest == null) throw new ArgumentNullException(nameof(paginationRequest));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT JsonDocument 
                    FROM ProductBundleInstances 
                    WHERE ProductBundleId = @ProductBundleId
                    ORDER BY UpdatedAt DESC
                    OFFSET @Skip ROWS
                    FETCH NEXT @PageSize ROWS ONLY";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ProductBundleId", productBundleId);
                command.Parameters.AddWithValue("@Skip", paginationRequest.Skip);
                command.Parameters.AddWithValue("@PageSize", paginationRequest.PageSize);

                var instances = new List<ProductBundleInstance>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var jsonDocument = reader.GetString(0);
                    var instance = JsonSerializer.Deserialize<ProductBundleInstance>(jsonDocument, _jsonOptions);
                    if (instance != null) instances.Add(instance);
                }

                return new PaginatedResult<ProductBundleInstance>(instances, paginationRequest.PageNumber, paginationRequest.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve ProductBundleInstances for ProductBundle: {ProductBundleId}", productBundleId);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("ID cannot be null or empty", nameof(id));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();
                using var transaction = connection.BeginTransaction();

                try
                {
                    // Get current data before deletion
                    var getCurrentSql = "SELECT JsonDocument, VersionNumber, ProductBundleId, ProductBundleVersion FROM ProductBundleInstances WHERE Id = @Id";
                    using var getCurrentCommand = new SqlCommand(getCurrentSql, connection, transaction);
                    getCurrentCommand.Parameters.AddWithValue("@Id", id);

                    using var reader = await getCurrentCommand.ExecuteReaderAsync();
                    if (!await reader.ReadAsync())
                    {
                        transaction.Rollback();
                        return false;
                    }

                    var jsonDocument = reader.GetString(0);
                    var versionNumber = reader.GetInt64(1);
                    var productBundleId = reader.GetString(2);
                    var productBundleVersion = reader.GetString(3);
                    reader.Close();

                    // Delete from current table
                    var deleteSql = "DELETE FROM ProductBundleInstances WHERE Id = @Id";
                    using var deleteCommand = new SqlCommand(deleteSql, connection, transaction);
                    deleteCommand.Parameters.AddWithValue("@Id", id);
                    await deleteCommand.ExecuteNonQueryAsync();

                    // Insert deletion record
                    await InsertVersionAsync(connection, transaction, id, versionNumber + 1, productBundleId, productBundleVersion, jsonDocument, "DELETE", null);

                    transaction.Commit();
                    _logger.LogInformation("Deleted versioned ProductBundleInstance with ID: {InstanceId}", id);
                    return true;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete versioned ProductBundleInstance with ID: {InstanceId}", id);
                throw;
            }
        }

        public async Task<bool> ExistsAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("ID cannot be null or empty", nameof(id));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT COUNT(1) FROM ProductBundleInstances WHERE Id = @Id";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                var count = await command.ExecuteScalarAsync();
                return count != null && (int)count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check existence of ProductBundleInstance with ID: {InstanceId}", id);
                throw;
            }
        }

        public async Task<int> GetCountAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT COUNT(*) FROM ProductBundleInstances";
                using var command = new SqlCommand(sql, connection);
                var count = await command.ExecuteScalarAsync();
                return count != null ? (int)count : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total ProductBundleInstance count");
                throw;
            }
        }

        public async Task<int> GetCountByProductBundleIdAsync(string productBundleId)
        {
            if (string.IsNullOrEmpty(productBundleId)) throw new ArgumentException("ProductBundle ID cannot be null or empty", nameof(productBundleId));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT COUNT(*) FROM ProductBundleInstances WHERE ProductBundleId = @ProductBundleId";
                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ProductBundleId", productBundleId);

                var count = await command.ExecuteScalarAsync();
                return count != null ? (int)count : 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get ProductBundleInstance count for ProductBundle: {ProductBundleId}", productBundleId);
                throw;
            }
        }

        /// <summary>
        /// Gets all versions of a specific ProductBundleInstance
        /// </summary>
        public async Task<List<ProductBundleInstanceVersion>> GetVersionHistoryAsync(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("ID cannot be null or empty", nameof(id));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT VersionNumber, ProductBundleId, ProductBundleVersion, JsonDocument, ChangeType, ChangedBy, CreatedAt
                    FROM ProductBundleInstanceVersions 
                    WHERE Id = @Id 
                    ORDER BY VersionNumber DESC";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                var versions = new List<ProductBundleInstanceVersion>();
                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var instance = JsonSerializer.Deserialize<ProductBundleInstance>(reader.GetString(3), _jsonOptions);
                    if (instance != null)
                    {
                        versions.Add(new ProductBundleInstanceVersion
                        {
                            Instance = instance,
                            VersionNumber = reader.GetInt64(0),
                            ChangeType = reader.GetString(4),
                            ChangedBy = reader.IsDBNull(5) ? null : reader.GetString(5),
                            CreatedAt = reader.GetDateTime(6)
                        });
                    }
                }

                return versions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get version history for ProductBundleInstance: {InstanceId}", id);
                throw;
            }
        }

        /// <summary>
        /// Archives old versions to reduce storage size and improve performance
        /// Keeps only the specified number of recent versions per instance
        /// </summary>
        public async Task<int> ArchiveOldVersionsAsync(int keepRecentVersions = 10, DateTime? olderThan = null)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var cutoffDate = olderThan ?? DateTime.UtcNow.AddMonths(-6);

                var archiveSql = @"
                    WITH VersionsToKeep AS (
                        SELECT Id, VersionNumber,
                               ROW_NUMBER() OVER (PARTITION BY Id ORDER BY VersionNumber DESC) as RowNum
                        FROM ProductBundleInstanceVersions
                        WHERE CreatedAt < @CutoffDate
                    )
                    DELETE FROM ProductBundleInstanceVersions 
                    WHERE EXISTS (
                        SELECT 1 FROM VersionsToKeep vtk 
                        WHERE vtk.Id = ProductBundleInstanceVersions.Id 
                        AND vtk.VersionNumber = ProductBundleInstanceVersions.VersionNumber
                        AND vtk.RowNum > @KeepRecentVersions
                    )";

                using var command = new SqlCommand(archiveSql, connection);
                command.Parameters.AddWithValue("@CutoffDate", cutoffDate);
                command.Parameters.AddWithValue("@KeepRecentVersions", keepRecentVersions);

                var deletedCount = await command.ExecuteNonQueryAsync();
                _logger.LogInformation("Archived {Count} old versions (keeping {KeepCount} recent versions per instance, older than {CutoffDate})", 
                    deletedCount, keepRecentVersions, cutoffDate);

                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to archive old versions");
                throw;
            }
        }

        /// <summary>
        /// Compresses version history by removing intermediate versions for instances with many changes
        /// Keeps first, last, and evenly distributed versions to maintain audit trail
        /// </summary>
        public async Task<int> CompressVersionHistoryAsync(int maxVersionsPerInstance = 50)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Find instances with too many versions
                var findInstancesSql = @"
                    SELECT Id, COUNT(*) as VersionCount
                    FROM ProductBundleInstanceVersions
                    GROUP BY Id
                    HAVING COUNT(*) > @MaxVersions";

                var instancesToCompress = new List<string>();
                using var findCommand = new SqlCommand(findInstancesSql, connection);
                findCommand.Parameters.AddWithValue("@MaxVersions", maxVersionsPerInstance);

                using var reader = await findCommand.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    instancesToCompress.Add(reader.GetString(0));
                }
                reader.Close();

                int totalDeleted = 0;
                foreach (var instanceId in instancesToCompress)
                {
                    totalDeleted += await CompressInstanceVersionsAsync(connection, instanceId, maxVersionsPerInstance);
                }

                _logger.LogInformation("Compressed version history for {InstanceCount} instances, deleted {DeletedCount} intermediate versions", 
                    instancesToCompress.Count, totalDeleted);

                return totalDeleted;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compress version history");
                throw;
            }
        }

        private async Task<int> CompressInstanceVersionsAsync(SqlConnection connection, string instanceId, int maxVersions)
        {
            // Get all versions for this instance
            var getVersionsSql = @"
                SELECT VersionNumber, ChangeType
                FROM ProductBundleInstanceVersions
                WHERE Id = @Id
                ORDER BY VersionNumber";

            var versions = new List<(long VersionNumber, string ChangeType)>();
            using var getCommand = new SqlCommand(getVersionsSql, connection);
            getCommand.Parameters.AddWithValue("@Id", instanceId);

            using var reader = await getCommand.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                versions.Add((reader.GetInt64(0), reader.GetString(1)));
            }
            reader.Close();

            if (versions.Count <= maxVersions) return 0;

            // Keep first, last, and evenly distributed versions
            var versionsToKeep = new HashSet<long>();
            
            // Always keep first and last
            versionsToKeep.Add(versions.First().VersionNumber);
            versionsToKeep.Add(versions.Last().VersionNumber);

            // Keep CREATE and DELETE operations
            foreach (var version in versions.Where(v => v.ChangeType == "CREATE" || v.ChangeType == "DELETE"))
            {
                versionsToKeep.Add(version.VersionNumber);
            }

            // Add evenly distributed versions to reach maxVersions
            var step = Math.Max(1, versions.Count / (maxVersions - versionsToKeep.Count));
            for (int i = step; i < versions.Count - 1; i += step)
            {
                if (versionsToKeep.Count >= maxVersions) break;
                versionsToKeep.Add(versions[i].VersionNumber);
            }

            // Delete versions not in the keep set
            var versionsToDelete = versions.Where(v => !versionsToKeep.Contains(v.VersionNumber)).Select(v => v.VersionNumber).ToList();
            
            if (versionsToDelete.Count == 0) return 0;

            var deleteVersionsSql = @"
                DELETE FROM ProductBundleInstanceVersions 
                WHERE Id = @Id AND VersionNumber IN (" + string.Join(",", versionsToDelete) + ")";

            using var deleteCommand = new SqlCommand(deleteVersionsSql, connection);
            deleteCommand.Parameters.AddWithValue("@Id", instanceId);

            return await deleteCommand.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Gets storage statistics for monitoring and capacity planning
        /// </summary>
        public async Task<VersionStorageStatistics> GetStorageStatisticsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var statsSql = @"
                    SELECT 
                        (SELECT COUNT(*) FROM ProductBundleInstances) as CurrentInstancesCount,
                        (SELECT COUNT(*) FROM ProductBundleInstanceVersions) as TotalVersionsCount,
                        (SELECT COUNT(DISTINCT Id) FROM ProductBundleInstanceVersions) as InstancesWithVersionsCount,
                        (SELECT AVG(CAST(VersionCount as FLOAT)) FROM (
                            SELECT COUNT(*) as VersionCount 
                            FROM ProductBundleInstanceVersions 
                            GROUP BY Id
                        ) as VersionCounts) as AverageVersionsPerInstance,
                        (SELECT MAX(VersionCount) FROM (
                            SELECT COUNT(*) as VersionCount 
                            FROM ProductBundleInstanceVersions 
                            GROUP BY Id
                        ) as VersionCounts) as MaxVersionsPerInstance,
                        (SELECT MIN(CreatedAt) FROM ProductBundleInstanceVersions) as OldestVersionDate,
                        (SELECT MAX(CreatedAt) FROM ProductBundleInstanceVersions) as NewestVersionDate";

                using var command = new SqlCommand(statsSql, connection);
                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new VersionStorageStatistics
                    {
                        CurrentInstancesCount = reader.GetInt32(0),
                        TotalVersionsCount = reader.GetInt32(1),
                        InstancesWithVersionsCount = reader.GetInt32(2),
                        AverageVersionsPerInstance = reader.IsDBNull(3) ? 0 : reader.GetDouble(3),
                        MaxVersionsPerInstance = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        OldestVersionDate = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5),
                        NewestVersionDate = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6)
                    };
                }

                return new VersionStorageStatistics();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get storage statistics");
                throw;
            }
        }

        /// <summary>
        /// Creates database partitions for better performance with large datasets
        /// </summary>
        public async Task CreatePartitionsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // Create partition function and scheme for date-based partitioning
                var createPartitionSql = @"
                    -- Create partition function if it doesn't exist
                    IF NOT EXISTS (SELECT * FROM sys.partition_functions WHERE name = 'PF_VersionsByMonth')
                    BEGIN
                        CREATE PARTITION FUNCTION PF_VersionsByMonth (DATETIME2)
                        AS RANGE RIGHT FOR VALUES (
                            '2024-01-01', '2024-02-01', '2024-03-01', '2024-04-01',
                            '2024-05-01', '2024-06-01', '2024-07-01', '2024-08-01',
                            '2024-09-01', '2024-10-01', '2024-11-01', '2024-12-01',
                            '2025-01-01'
                        )
                    END

                    -- Create partition scheme if it doesn't exist
                    IF NOT EXISTS (SELECT * FROM sys.partition_schemes WHERE name = 'PS_VersionsByMonth')
                    BEGIN
                        CREATE PARTITION SCHEME PS_VersionsByMonth
                        AS PARTITION PF_VersionsByMonth
                        ALL TO ([PRIMARY])
                    END";

                using var command = new SqlCommand(createPartitionSql, connection);
                await command.ExecuteNonQueryAsync();

                _logger.LogInformation("Database partitions created successfully for improved scalability");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create database partitions");
                throw;
            }
        }
    }

    /// <summary>
    /// Represents a version of a ProductBundleInstance with metadata
    /// </summary>
    public class ProductBundleInstanceVersion
    {
        public ProductBundleInstance Instance { get; set; } = new();
        public long VersionNumber { get; set; }
        public string ChangeType { get; set; } = string.Empty;
        public string? ChangedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Storage statistics for monitoring and capacity planning
    /// </summary>
    public class VersionStorageStatistics
    {
        public int CurrentInstancesCount { get; set; }
        public int TotalVersionsCount { get; set; }
        public int InstancesWithVersionsCount { get; set; }
        public double AverageVersionsPerInstance { get; set; }
        public int MaxVersionsPerInstance { get; set; }
        public DateTime? OldestVersionDate { get; set; }
        public DateTime? NewestVersionDate { get; set; }
    }
}
