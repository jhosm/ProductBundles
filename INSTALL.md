# ProductBundles Installation Guide

This guide provides step-by-step instructions for setting up the ProductBundles system.

## Prerequisites

- .NET 8.0 or later
- SQL Server (2019 or later) or MongoDB
- Visual Studio 2022 or VS Code with C# extension

## Database Setup

### SQL Server Setup

The ProductBundles system requires specific database tables to be created before the application can run. These DDL statements require elevated permissions and should be executed during the initial setup phase.

#### Step 1: Create Database
```sql
CREATE DATABASE ProductBundles;
USE ProductBundles;
```

#### Step 2: Execute Setup Scripts

Navigate to the `InitialSetup/SqlServer/` folder and execute the following scripts in order:

1. **Create Core Tables** (Required)
   ```bash
   sqlcmd -S your_server -d ProductBundles -i InitialSetup/SqlServer/01-create-tables.sql
   ```
   
   Or execute the SQL directly:
   ```sql
   -- Run the contents of InitialSetup/SqlServer/01-create-tables.sql
   ```

2. **Create Partitions** (Optional - for high-volume scenarios)
   ```bash
   sqlcmd -S your_server -d ProductBundles -i InitialSetup/SqlServer/02-create-partitions.sql
   ```

#### Required Permissions

The database user executing the setup scripts needs the following permissions:
- `CREATE TABLE`
- `CREATE INDEX`
- `CREATE PARTITION FUNCTION` (for partitioning script)
- `CREATE PARTITION SCHEME` (for partitioning script)

#### Connection String Configuration

Update your application configuration with the appropriate connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your_server;Database=ProductBundles;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

### MongoDB Setup (Alternative)

If using MongoDB instead of SQL Server:

1. **Install MongoDB** (version 4.4 or later)
2. **Create Database and Collections** (handled automatically by the application)
3. **Configure Connection String**:
   ```json
   {
     "ConnectionStrings": {
       "MongoConnection": "mongodb://localhost:27017/ProductBundles"
     }
   }
   ```

## Application Setup

### Step 1: Clone and Build

```bash
git clone <repository-url>
cd ProductBundles
dotnet restore
dotnet build
```

### Step 2: Configure Services

The application will automatically verify that required database tables exist on startup. If tables are missing, you'll see an error message directing you to run the setup scripts.

### Step 3: Run Tests

```bash
dotnet test
```

### Step 4: Start the Application

```bash
cd ProductBundles.Api
dotnet run
```

## Verification

After setup, verify the installation:

1. **Check Database Tables**: Ensure `ProductBundleInstances` and `ProductBundleInstanceVersions` tables exist
2. **Run Application**: The application should start without database-related errors
3. **Check Logs**: Look for "Database connection and table verification successful" message

## Troubleshooting

### Common Issues

1. **Missing Tables Error**
   ```
   Required database tables are missing. Please run the database setup scripts from the InitialSetup folder first.
   ```
   **Solution**: Execute the SQL scripts in the `InitialSetup/SqlServer/` folder

2. **Permission Denied**
   ```
   CREATE TABLE permission denied
   ```
   **Solution**: Ensure the database user has DDL permissions or ask a DBA to run the setup scripts

3. **Connection Issues**
   ```
   Cannot connect to SQL Server
   ```
   **Solution**: Verify connection string and SQL Server accessibility

### Getting Help

- Check the logs for detailed error messages
- Verify connection strings and permissions
- Ensure all prerequisite software is installed
- Review the `InitialSetup/README.md` for database-specific setup details

## Production Deployment

For production environments:

1. **Separate Setup Phase**: Run DDL scripts during deployment with elevated permissions
2. **Application Runtime**: Use restricted permissions for normal operation
3. **Monitoring**: Monitor the `VerifyPartitionsAsync()` method for partition status
4. **Backup Strategy**: Implement appropriate backup procedures for version history data
