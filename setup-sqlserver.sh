#!/bin/bash

# Setup SQL Server Docker container for ProductBundles
echo "Setting up SQL Server Docker container for ProductBundles..."

# Start the SQL Server container using Docker Compose
docker-compose -f docker-compose.sqlserver.yml up -d

# Wait for SQL Server to be ready
echo "Waiting for SQL Server to start..."
sleep 30

# Check if SQL Server is ready
echo "Checking SQL Server connection..."
docker exec productbundles-sqlserver /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P 'ProductBundles123!' -C -Q 'SELECT @@VERSION'

if [ $? -eq 0 ]; then
    echo "✅ SQL Server is ready!"
    echo ""
    echo "Connection Details:"
    echo "  Server: localhost,1433"
    echo "  Username: sa"
    echo "  Password: ProductBundles123!"
    echo "  Connection String: Server=localhost,1433;Database=ProductBundles;User Id=sa;Password=ProductBundles123!;TrustServerCertificate=true;"
    echo ""
    echo "To stop the container: docker-compose -f docker-compose.sqlserver.yml down"
    echo "To view logs: docker logs productbundles-sqlserver"
else
    echo "❌ SQL Server failed to start properly"
    echo "Check logs with: docker logs productbundles-sqlserver"
fi
