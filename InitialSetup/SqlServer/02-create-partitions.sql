-- ProductBundles SQL Server Database Partitioning (Optional)
-- This script creates partitions for better performance with large datasets
-- Run this script after creating the base tables if you expect high volume data

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
END
