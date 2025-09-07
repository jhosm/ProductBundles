using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using ProductBundles.Sdk;
using System.Text.Json;

namespace ProductBundles.Core.Storage
{
    /// <summary>
    /// SQL Server implementation of IProductBundleInstanceStorage that stores ProductBundleInstance objects as JSON documents
    /// </summary>
    public class SqlServerProductBundleInstanceStorage : IProductBundleInstanceStorage
    {
        private readonly string _connectionString;
        private readonly ILogger<SqlServerProductBundleInstanceStorage> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        /// <summary>
        /// Initializes a new instance of the SqlServerProductBundleInstanceStorage class
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string</param>
        /// <param name="logger">Optional logger for debugging and monitoring</param>
        public SqlServerProductBundleInstanceStorage(string connectionString, ILogger<SqlServerProductBundleInstanceStorage>? logger = null)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SqlServerProductBundleInstanceStorage>.Instance;
            
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Creates the database table if it doesn't exist
        /// </summary>
        private async Task InitializeDatabaseAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var createTableSql = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProductBundleInstances' AND xtype='U')
                    CREATE TABLE ProductBundleInstances (
                        Id NVARCHAR(450) PRIMARY KEY,
                        ProductBundleId NVARCHAR(450) NOT NULL,
                        ProductBundleVersion NVARCHAR(100) NOT NULL,
                        JsonDocument NVARCHAR(MAX) NOT NULL,
                        CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
                        UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
                        INDEX IX_ProductBundleInstances_ProductBundleId (ProductBundleId),
                        INDEX IX_ProductBundleInstances_CreatedAt (CreatedAt)
                    )";

                using var command = new SqlCommand(createTableSql, connection);
                await command.ExecuteNonQueryAsync();

                _logger.LogDebug("Database table ProductBundleInstances initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database table");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> CreateAsync(ProductBundleInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (string.IsNullOrEmpty(instance.Id))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(instance));

            try
            {
                var jsonDocument = JsonSerializer.Serialize(instance, _jsonOptions);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    INSERT INTO ProductBundleInstances (Id, ProductBundleId, ProductBundleVersion, JsonDocument, CreatedAt, UpdatedAt)
                    VALUES (@Id, @ProductBundleId, @ProductBundleVersion, @JsonDocument, GETUTCDATE(), GETUTCDATE())";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", instance.Id);
                command.Parameters.AddWithValue("@ProductBundleId", instance.ProductBundleId ?? string.Empty);
                command.Parameters.AddWithValue("@ProductBundleVersion", instance.ProductBundleVersion ?? string.Empty);
                command.Parameters.AddWithValue("@JsonDocument", jsonDocument);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                var success = rowsAffected > 0;

                if (success)
                {
                    _logger.LogInformation("Created ProductBundleInstance with ID: {InstanceId}", instance.Id);
                }

                return success;
            }
            catch (SqlException ex) when (ex.Number == 2627) // Primary key violation
            {
                _logger.LogWarning("ProductBundleInstance with ID {InstanceId} already exists", instance.Id);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create ProductBundleInstance with ID: {InstanceId}", instance.Id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<ProductBundleInstance?> GetAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT JsonDocument FROM ProductBundleInstances WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                var jsonDocument = await command.ExecuteScalarAsync() as string;

                if (jsonDocument == null)
                {
                    _logger.LogDebug("ProductBundleInstance with ID {InstanceId} not found", id);
                    return null;
                }

                var instance = JsonSerializer.Deserialize<ProductBundleInstance>(jsonDocument, _jsonOptions);
                _logger.LogDebug("Retrieved ProductBundleInstance with ID: {InstanceId}", id);
                return instance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve ProductBundleInstance with ID: {InstanceId}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<PaginatedResult<ProductBundleInstance>> GetByProductBundleIdAsync(string productBundleId, PaginationRequest paginationRequest)
        {
            if (string.IsNullOrEmpty(productBundleId))
                throw new ArgumentException("ProductBundle ID cannot be null or empty", nameof(productBundleId));

            if (paginationRequest == null)
                throw new ArgumentNullException(nameof(paginationRequest));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    SELECT JsonDocument 
                    FROM ProductBundleInstances 
                    WHERE ProductBundleId = @ProductBundleId
                    ORDER BY CreatedAt DESC
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
                    var jsonDocument = reader.GetString(0); // Use column index instead of name
                    var instance = JsonSerializer.Deserialize<ProductBundleInstance>(jsonDocument, _jsonOptions);
                    if (instance != null)
                    {
                        instances.Add(instance);
                    }
                }

                _logger.LogDebug("Retrieved {Count} ProductBundleInstances for ProductBundle {ProductBundleId} (Page {PageNumber}, Size {PageSize})", 
                    instances.Count, productBundleId, paginationRequest.PageNumber, paginationRequest.PageSize);

                return new PaginatedResult<ProductBundleInstance>(instances, paginationRequest.PageNumber, paginationRequest.PageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve ProductBundleInstances for ProductBundle: {ProductBundleId}", productBundleId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UpdateAsync(ProductBundleInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            if (string.IsNullOrEmpty(instance.Id))
                throw new ArgumentException("Instance ID cannot be null or empty", nameof(instance));

            try
            {
                var jsonDocument = JsonSerializer.Serialize(instance, _jsonOptions);

                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = @"
                    UPDATE ProductBundleInstances 
                    SET ProductBundleId = @ProductBundleId, 
                        ProductBundleVersion = @ProductBundleVersion, 
                        JsonDocument = @JsonDocument, 
                        UpdatedAt = GETUTCDATE()
                    WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", instance.Id);
                command.Parameters.AddWithValue("@ProductBundleId", instance.ProductBundleId ?? string.Empty);
                command.Parameters.AddWithValue("@ProductBundleVersion", instance.ProductBundleVersion ?? string.Empty);
                command.Parameters.AddWithValue("@JsonDocument", jsonDocument);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                var success = rowsAffected > 0;

                if (success)
                {
                    _logger.LogInformation("Updated ProductBundleInstance with ID: {InstanceId}", instance.Id);
                }
                else
                {
                    _logger.LogWarning("ProductBundleInstance with ID {InstanceId} not found for update", instance.Id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update ProductBundleInstance with ID: {InstanceId}", instance.Id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "DELETE FROM ProductBundleInstances WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                var rowsAffected = await command.ExecuteNonQueryAsync();
                var success = rowsAffected > 0;

                if (success)
                {
                    _logger.LogInformation("Deleted ProductBundleInstance with ID: {InstanceId}", id);
                }
                else
                {
                    _logger.LogWarning("ProductBundleInstance with ID {InstanceId} not found for deletion", id);
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete ProductBundleInstance with ID: {InstanceId}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("ID cannot be null or empty", nameof(id));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT COUNT(1) FROM ProductBundleInstances WHERE Id = @Id";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                var count = await command.ExecuteScalarAsync();
                var result = count != null ? (int)count : 0;

                _logger.LogDebug("ProductBundleInstance with ID {InstanceId} exists: {Exists}", id, result > 0);
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check existence of ProductBundleInstance with ID: {InstanceId}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<int> GetCountAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT COUNT(*) FROM ProductBundleInstances";

                using var command = new SqlCommand(sql, connection);
                var count = await command.ExecuteScalarAsync();
                var result = count != null ? (int)count : 0;

                _logger.LogDebug("Total ProductBundleInstance count: {Count}", result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get total ProductBundleInstance count");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<int> GetCountByProductBundleIdAsync(string productBundleId)
        {
            if (string.IsNullOrEmpty(productBundleId))
                throw new ArgumentException("ProductBundle ID cannot be null or empty", nameof(productBundleId));

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var sql = "SELECT COUNT(*) FROM ProductBundleInstances WHERE ProductBundleId = @ProductBundleId";

                using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ProductBundleId", productBundleId);

                var count = await command.ExecuteScalarAsync();
                var result = count != null ? (int)count : 0;

                _logger.LogDebug("ProductBundleInstance count for ProductBundle {ProductBundleId}: {Count}", productBundleId, result);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get ProductBundleInstance count for ProductBundle: {ProductBundleId}", productBundleId);
                throw;
            }
        }
    }
}
