#!/bin/bash

# Test SQL Server connection and create ProductBundles database
echo "Testing SQL Server connection and setup..."

# Test basic connection
echo "1. Testing basic connection..."
docker exec productbundles-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'ProductBundles123!' -C -Q 'SELECT @@VERSION'

if [ $? -ne 0 ]; then
    echo "❌ Failed to connect to SQL Server"
    exit 1
fi

# Create ProductBundles database
echo ""
echo "2. Creating ProductBundles database..."
docker exec productbundles-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'ProductBundles123!' -C -Q 'CREATE DATABASE ProductBundles'

# Verify database was created
echo ""
echo "3. Verifying ProductBundles database exists..."
docker exec productbundles-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'ProductBundles123!' -C -Q 'SELECT name FROM sys.databases WHERE name = '\''ProductBundles'\'''

echo ""
echo "✅ SQL Server is ready for ProductBundles!"
echo ""
echo "Connection Details:"
echo "  Server: localhost,1433"
echo "  Database: ProductBundles"
echo "  Username: sa"
echo "  Password: ProductBundles123!"
echo "  Connection String: Server=localhost,1433;Database=ProductBundles;User Id=sa;Password=ProductBundles123!;TrustServerCertificate=true;"
echo ""
echo "You can now run your ProductBundles unit tests with SQL Server!"
