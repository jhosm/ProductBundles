-- ProductBundles SQL Server Database Schema
-- This script creates the required tables for the ProductBundles system
-- Run this script with elevated permissions (DDL permissions) during initial setup

-- Current instances table (optimized for fast queries)
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
);

-- Version history table (partitioned by date for scalability)
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
);
