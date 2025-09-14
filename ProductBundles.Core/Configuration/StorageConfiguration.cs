namespace ProductBundles.Core.Configuration
{
    /// <summary>
    /// Configuration for ProductBundle storage options
    /// </summary>
    public class StorageConfiguration
    {
        /// <summary>
        /// The type of storage to use (FileSystem, MongoDB, SqlServer)
        /// </summary>
        public string Provider { get; set; } = "FileSystem";

        /// <summary>
        /// File system storage configuration
        /// </summary>
        public FileSystemStorageOptions? FileSystem { get; set; }

        /// <summary>
        /// MongoDB storage configuration
        /// </summary>
        public MongoStorageOptions? MongoDB { get; set; }

        /// <summary>
        /// SQL Server storage configuration
        /// </summary>
        public SqlServerStorageOptions? SqlServer { get; set; }

        /// <summary>
        /// Validates the storage configuration based on the selected provider
        /// </summary>
        /// <returns>Validation result with error messages if any</returns>
        public StorageConfigurationValidationResult Validate()
        {
            var result = new StorageConfigurationValidationResult();

            switch (Provider?.ToLowerInvariant())
            {
                case "filesystem":
                    if (FileSystem == null)
                    {
                        result.AddError("FileSystem configuration is required when Provider is 'FileSystem'");
                    }
                    else if (string.IsNullOrWhiteSpace(FileSystem.StorageDirectory))
                    {
                        result.AddError("FileSystem.StorageDirectory is required");
                    }
                    break;

                case "mongodb":
                    if (MongoDB == null)
                    {
                        result.AddError("MongoDB configuration is required when Provider is 'MongoDB'");
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(MongoDB.ConnectionString))
                            result.AddError("MongoDB.ConnectionString is required");
                        if (string.IsNullOrWhiteSpace(MongoDB.DatabaseName))
                            result.AddError("MongoDB.DatabaseName is required");
                    }
                    break;

                case "sqlserver":
                    if (SqlServer == null)
                    {
                        result.AddError("SqlServer configuration is required when Provider is 'SqlServer'");
                    }
                    else if (string.IsNullOrWhiteSpace(SqlServer.ConnectionString))
                    {
                        result.AddError("SqlServer.ConnectionString is required");
                    }
                    break;

                default:
                    result.AddError($"Unknown storage provider '{Provider}'. Supported providers are: FileSystem, MongoDB, SqlServer");
                    break;
            }

            return result;
        }
    }

    /// <summary>
    /// File system storage configuration options
    /// </summary>
    public class FileSystemStorageOptions
    {
        /// <summary>
        /// Directory path where ProductBundle instances will be stored
        /// </summary>
        public string StorageDirectory { get; set; } = string.Empty;
    }

    /// <summary>
    /// MongoDB storage configuration options
    /// </summary>
    public class MongoStorageOptions
    {
        /// <summary>
        /// MongoDB connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// MongoDB database name
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// MongoDB collection name (defaults to "ProductBundleInstances")
        /// </summary>
        public string CollectionName { get; set; } = "ProductBundleInstances";
    }

    /// <summary>
    /// SQL Server storage configuration options
    /// </summary>
    public class SqlServerStorageOptions
    {
        /// <summary>
        /// SQL Server connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result of storage configuration validation
    /// </summary>
    public class StorageConfigurationValidationResult
    {
        private readonly List<string> _errors = new();

        /// <summary>
        /// Gets whether the configuration is valid
        /// </summary>
        public bool IsValid => _errors.Count == 0;

        /// <summary>
        /// Gets the validation error messages
        /// </summary>
        public IReadOnlyList<string> Errors => _errors.AsReadOnly();

        /// <summary>
        /// Adds an error message to the validation result
        /// </summary>
        /// <param name="error">The error message</param>
        internal void AddError(string error)
        {
            _errors.Add(error);
        }

        /// <summary>
        /// Gets a formatted error message containing all validation errors
        /// </summary>
        /// <returns>Formatted error message</returns>
        public string GetFormattedErrors()
        {
            if (IsValid)
                return string.Empty;

            return $"Storage configuration validation failed:\n{string.Join("\n", _errors.Select(e => $"- {e}"))}";
        }
    }
}
