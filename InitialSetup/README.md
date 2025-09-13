# ProductBundles Initial Setup

This folder contains the database schema and setup scripts required for the ProductBundles system.

## Directory Structure

```
InitialSetup/
├── SqlServer/
│   ├── 01-create-tables.sql      # Core table creation script
│   └── 02-create-partitions.sql  # Optional partitioning for high-volume scenarios
└── README.md                     # This file
```

## SQL Server Setup

### Prerequisites
- SQL Server instance running
- Database created for ProductBundles
- User account with DDL permissions (CREATE TABLE, CREATE INDEX, etc.)

### Installation Steps

1. **Execute Core Tables Script**
   ```sql
   -- Run this script first to create the base tables
   -- File: SqlServer/01-create-tables.sql
   ```

2. **Execute Partitioning Script (Optional)**
   ```sql
   -- Run this script only if you expect high-volume data
   -- File: SqlServer/02-create-partitions.sql
   ```

### Tables Created

- **ProductBundleInstances**: Stores current instances with optimized indexes
- **ProductBundleInstanceVersions**: Maintains complete version history

### Permissions Required

The setup scripts require the following SQL Server permissions:
- `CREATE TABLE`
- `CREATE INDEX`
- `CREATE PARTITION FUNCTION` (for partitioning script)
- `CREATE PARTITION SCHEME` (for partitioning script)

### Notes

- All scripts use `IF NOT EXISTS` checks to prevent errors on re-execution
- The partitioning script is optional and should only be used for high-volume scenarios
- Tables are created with appropriate indexes for optimal performance
