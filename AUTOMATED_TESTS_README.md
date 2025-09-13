# Automated Tests Setup and Execution Guide

This document provides comprehensive instructions for setting up and running all automated tests in the ProductBundles solution, including tests that require external dependencies like SQL Server and MongoDB.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Database Dependencies](#database-dependencies)
  - [SQL Server Setup](#sql-server-setup)
  - [MongoDB Setup](#mongodb-setup)
- [Test Execution](#test-execution)
- [Test Categories](#test-categories)
- [Troubleshooting](#troubleshooting)
- [CI/CD Considerations](#cicd-considerations)

## Prerequisites

### Required Software
- **.NET 8.0 SDK** or later
- **Docker Desktop** (for database containers)
- **Git** (for cloning and version control)

### Verify Prerequisites
```bash
# Check .NET version
dotnet --version

# Check Docker installation
docker --version
docker-compose --version
```

## Quick Start

For developers who want to run tests immediately:

```bash
# 1. Clone and navigate to the project
git clone <repository-url>
cd windsurf-project

# 2. Setup databases (required for storage tests)
./setup-sqlserver.sh
./setup-mongodb.sh  # If MongoDB setup script exists

# 3. Build plugins
./build-plugins.sh

# 4. Run all tests
dotnet test --logger "console;verbosity=normal"
```

## Database Dependencies

### SQL Server Setup

The solution includes SQL Server storage tests that require a running SQL Server instance.

#### Option 1: Docker Container (Recommended)

```bash
# Start SQL Server container
docker-compose -f docker-compose.sqlserver.yml up -d

# Verify connection and create databases
./test-sqlserver-connection.sh
```

**Connection Details:**
- **Server**: `localhost,1433`
- **Username**: `sa`
- **Password**: `ProductBundles123!`
- **Test Database**: `ProductBundlesTest`
- **Main Database**: `ProductBundles`

#### Option 2: Local SQL Server Installation

If you prefer a local SQL Server installation:

1. Install SQL Server Developer Edition
2. Update connection string in test files:
   ```csharp
   private const string TestConnectionString = "Server=localhost;Database=ProductBundlesTest;Integrated Security=true;";
   ```

#### SQL Server Test Configuration

The SQL Server tests are located in:
- `ProductBundles.UnitTests/SqlServerProductBundleInstanceStorageTests.cs` (tests SqlServerVersionedProductBundleInstanceStorage)
- `ProductBundles.UnitTests/ServiceCollectionExtensionsSqlServerTests.cs`

**Test Database Schema:**
The tests automatically create the required `ProductBundleInstances` table with the following structure:
```sql
CREATE TABLE ProductBundleInstances (
    Id NVARCHAR(255) PRIMARY KEY,
    ProductBundleId NVARCHAR(255) NOT NULL,
    ProductBundleVersion NVARCHAR(50) NOT NULL,
    JsonDocument NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
```

### MongoDB Setup

The solution includes MongoDB storage tests that require a running MongoDB instance.

#### Docker Container Setup

```bash
# Start MongoDB container
docker run -d --name productbundles-mongodb \
  -p 27017:27017 \
  -e MONGO_INITDB_ROOT_USERNAME=admin \
  -e MONGO_INITDB_ROOT_PASSWORD=ProductBundles123! \
  mongo:latest

# Verify MongoDB connection
docker exec productbundles-mongodb mongosh --eval "db.adminCommand('ping')"
```

**Connection Details:**
- **Server**: `localhost:27017`
- **Username**: `admin`
- **Password**: `ProductBundles123!`
- **Test Database**: `ProductBundlesTest`
- **Collection**: `ProductBundleInstances`

#### MongoDB Test Configuration

The MongoDB tests are located in:
- `ProductBundles.UnitTests/MongoProductBundleInstanceStorageTests.cs`
- `ProductBundles.UnitTests/ServiceCollectionExtensionsStorageTests.cs`

## Test Execution

### Run All Tests
```bash
# Run all tests with detailed output
dotnet test --logger "console;verbosity=normal"

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage" --logger "console;verbosity=normal"
```

### Run Specific Test Categories

#### Storage Tests Only
```bash
# SQL Server versioned storage tests
dotnet test --filter "ClassName~SqlServerProductBundleInstanceStorageTests"

# MongoDB storage tests  
dotnet test --filter "ClassName~MongoProductBundleInstanceStorageTests"

# File system storage tests
dotnet test --filter "ClassName~FileSystemProductBundleInstanceStorageTests"
```

#### Plugin System Tests
```bash
# Plugin loading and management tests
dotnet test --filter "ClassName~ProductBundlesLoaderTests"

# Plugin execution tests
dotnet test --filter "ClassName~ProductBundleBackgroundServiceTests"
```

#### Integration Tests
```bash
# Entity source integration tests
dotnet test --filter "ClassName~EntitySourceIntegrationTests"

# End-to-end workflow tests
dotnet test --filter "ClassName~IntegrationTests"
```

### Run Tests by Project
```bash
# Run only unit tests
dotnet test ProductBundles.UnitTests/

# Run with specific configuration
dotnet test ProductBundles.UnitTests/ --configuration Release
```

## Test Categories

### Unit Tests (189 total)

#### Core Functionality Tests
- **ProductBundlesLoaderTests** (16 tests) - Plugin loading and discovery
- **PluginManagerTests** (8 tests) - Plugin lifecycle management
- **CronScheduleTests** (12 tests) - Cron expression parsing and validation
- **PluginSchedulerTests** (10 tests) - Scheduled plugin execution

#### Storage Implementation Tests
- **FileSystemProductBundleInstanceStorageTests** (26 tests) - File-based storage
- **SqlServerProductBundleInstanceStorageTests** (26 tests) - SQL Server versioned storage
- **MongoProductBundleInstanceStorageTests** (72 tests) - MongoDB storage
- **JsonProductBundleInstanceSerializerTests** (15 tests) - JSON serialization

#### Background Service Tests
- **ProductBundleBackgroundServiceTests** (8 tests) - Async plugin execution
- **ProductBundleBackgroundServiceTimeoutTests** (4 tests) - Timeout handling

#### Entity Source Tests
- **EntitySourceManagerTests** (6 tests) - Entity event management
- **CustomerEventSourceTests** (4 tests) - Sample entity source
- **EntitySourceIntegrationTests** (6 tests) - End-to-end entity processing

#### Dependency Injection Tests
- **ServiceCollectionExtensionsTests** (10 tests) - DI registration
- **ServiceCollectionExtensionsSqlServerTests** (10 tests) - SQL Server DI
- **ServiceCollectionExtensionsStorageTests** (10 tests) - MongoDB DI

### Integration Tests (6 total)
- **EntitySourceIntegrationTests** - Complete workflow from entity events to plugin execution

## Troubleshooting

### Common Issues

#### SQL Server Connection Issues
```bash
# Check if container is running
docker ps | grep productbundles-sqlserver

# Check container logs
docker logs productbundles-sqlserver

# Restart container
docker-compose -f docker-compose.sqlserver.yml restart
```

**Error**: `SQL Server not available for testing`
**Solution**: Ensure SQL Server container is running and accessible on port 1433.

#### MongoDB Connection Issues
```bash
# Check if container is running
docker ps | grep productbundles-mongodb

# Test connection
docker exec productbundles-mongodb mongosh --eval "db.adminCommand('ping')"
```

**Error**: `MongoDB connection failed`
**Solution**: Verify MongoDB container is running on port 27017.

#### Plugin Loading Issues
```bash
# Rebuild plugins
./build-plugins.sh

# Check plugin directory
ls -la ProductBundles.UnitTests/plugins/
```

**Error**: `No plugins found in directory`
**Solution**: Run `./build-plugins.sh` to copy plugin DLLs to test directories.

#### Test Discovery Issues
```bash
# Clean and rebuild
dotnet clean
dotnet build

# Restore packages
dotnet restore
```

### Test Skipping Behavior

Some tests may be skipped if dependencies are not available:

- **SQL Server tests** skip if connection fails with `Assert.Inconclusive`
- **MongoDB tests** skip if MongoDB is not accessible
- **Plugin tests** skip if plugin files are missing

### Performance Considerations

- **Database tests** may take longer due to I/O operations
- **Integration tests** include setup/teardown overhead
- **Parallel execution** is enabled by default but may cause resource contention

## CI/CD Considerations

### GitHub Actions / Azure DevOps

```yaml
# Example CI pipeline steps
- name: Setup .NET
  uses: actions/setup-dotnet@v3
  with:
    dotnet-version: '8.0.x'

- name: Start SQL Server
  run: docker-compose -f docker-compose.sqlserver.yml up -d

- name: Start MongoDB  
  run: docker run -d --name mongodb -p 27017:27017 mongo:latest

- name: Build plugins
  run: ./build-plugins.sh

- name: Run tests
  run: dotnet test --logger trx --collect:"XPlat Code Coverage"
```

### Docker Compose for CI

Create `docker-compose.ci.yml` for CI environments:

```yaml
version: '3.8'
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      - ACCEPT_EULA=Y
      - MSSQL_SA_PASSWORD=ProductBundles123!
    ports:
      - "1433:1433"
    
  mongodb:
    image: mongo:latest
    environment:
      - MONGO_INITDB_ROOT_USERNAME=admin
      - MONGO_INITDB_ROOT_PASSWORD=ProductBundles123!
    ports:
      - "27017:27017"
```

### Test Execution Order

For reliable CI execution:

1. **Start database containers**
2. **Wait for health checks**
3. **Build solution**
4. **Build plugins**
5. **Run tests**
6. **Collect coverage**
7. **Stop containers**

### Environment Variables

Set these environment variables for CI:

```bash
export PRODUCTBUNDLES_SQLSERVER_CONNECTION="Server=localhost,1433;Database=ProductBundlesTest;User Id=sa;Password=ProductBundles123!;TrustServerCertificate=true;"
export PRODUCTBUNDLES_MONGODB_CONNECTION="mongodb://admin:ProductBundles123!@localhost:27017"
```

## Code Coverage

Generate detailed code coverage reports:

```bash
# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults

# Generate HTML report (requires reportgenerator tool)
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"./TestResults/*/coverage.cobertura.xml" -targetdir:"./TestResults/html" -reporttypes:Html
```

**Current Coverage Metrics:**
- **Line Coverage**: ~86%
- **Branch Coverage**: ~85%
- **Method Coverage**: ~89%

## Test Data Management

### Database Cleanup

Tests automatically clean up test data:
- **SQL Server**: `DELETE FROM ProductBundleInstances` in `TestCleanup`
- **MongoDB**: Collection drop/recreate between tests
- **File System**: Temporary directories with automatic cleanup

### Test Isolation

Each test method:
- Uses unique identifiers for test data
- Cleans up after execution
- Runs independently of other tests
- Uses separate database/collection namespaces when possible

---

## Support

For issues with test execution:

1. Check this README for common solutions
2. Verify all prerequisites are installed
3. Ensure database containers are running
4. Check test output for specific error messages
5. Review container logs for database connectivity issues

**Test Statistics:**
- **Total Tests**: 189
- **Test Projects**: 1 (ProductBundles.UnitTests)
- **Test Categories**: Unit Tests, Integration Tests, Storage Tests
- **External Dependencies**: SQL Server, MongoDB, File System
- **Average Execution Time**: ~2-3 minutes (with databases)
