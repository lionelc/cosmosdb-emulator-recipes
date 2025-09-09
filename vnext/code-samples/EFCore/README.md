# EF Core CosmosDB Test Project

This project demonstrates how to use Entity Framework Core with Azure CosmosDB, replicating the test scenarios from the direct SDK implementation but using EF Core's ORM capabilities.

## Overview

This test suite mirrors the functionality of the direct CosmosDB SDK test (`vnext/code-samples/dotnet`) but uses Entity Framework Core as the data access layer. It demonstrates:

- **Database and Container Creation**: Using EF Core migrations and `EnsureCreatedAsync()`
- **Hierarchical Partition Keys**: Configuring composite partition keys with EF Core
- **CRUD Operations**: Create, Read, Update, Delete operations using EF Core
- **Complex Document Types**: Working with nested objects and collections
- **Change Tracking**: Leveraging EF Core's change tracking capabilities
- **Querying**: Using LINQ queries translated to CosmosDB SQL
- **System Properties**: Handling CosmosDB system properties like `_etag`

## Key Features Tested

### 1. Document Models
- **TestDocument**: Basic document with hierarchical partition key
- **MessageDocument**: Complex document with nested collections (similar to the original SDK test)
- **SimpleTestDocument**: Document for testing system properties like `_etag`

### 2. EF Core Configuration
- **DbContext Configuration**: Connection to local CosmosDB emulator
- **Model Configuration**: Partition key setup, JSON property mapping
- **Container Mapping**: Multiple document types to different containers

### 3. Test Scenarios
- **Basic CRUD Operations**: Create, read, update, delete documents
- **Hierarchical Partition Keys**: Working with composite partition keys
- **Complex Queries**: Filtering, ordering, and projection
- **Change Tracking**: EF Core's automatic change detection
- **Concurrent Access**: Simulating multiple contexts accessing the same data
- **System Properties**: Testing ETag handling and other CosmosDB metadata

## Getting Started

### Prerequisites
- .NET 9.0 SDK
- CosmosDB Emulator running locally on `http://localhost:8081` (when it's https, need to change the endpoint setting correspondingly): see the [guide](https://learn.microsoft.com/en-us/azure/cosmos-db/emulator-linux)

### Running the Tests

```bash
cd vnext/code-samples/EFCore
dotnet run
```

### Project Structure

```
EFCore/
├── Program.cs                          # Main test runner
├── EFCore.CosmosDB.Test.csproj        # Project file with EF Core dependencies
├── Models/
│   └── Models.cs                       # Entity models (TestDocument, MessageDocument, etc.)
└── Data/
    └── CosmosDbContext.cs             # EF Core DbContext configuration
```

## Key Differences from Direct SDK

### Advantages of EF Core Approach:
1. **Automatic Change Tracking**: EF Core automatically detects changes to entities
2. **LINQ Support**: Type-safe queries using LINQ instead of SQL strings
3. **Model Configuration**: Centralized entity configuration and validation
4. **Navigation Properties**: Better support for related data and complex types
5. **Migration Support**: Schema evolution capabilities

### SDK-Specific Features Not Available:
1. **Change Feed**: Direct change feed access (requires SDK)
2. **Bulk Operations**: Bulk insert/update operations
3. **Custom Serialization**: Full control over JSON serialization
4. **Advanced Query Options**: Some advanced CosmosDB query features

## Configuration Notes

### Connection Configuration
The project uses the same connection settings as the direct SDK test:
- **Endpoint**: `http://localhost:8081` (CosmosDB Emulator)
- **Key**: Default emulator key
- **SSL Validation**: Disabled for local emulator

### Partition Key Configuration
EF Core supports hierarchical partition keys through composite key configuration:
```csharp
entity.HasPartitionKey(e => new { e.PartitionKey, e.QueryField });
```

### JSON Property Mapping
Properties are mapped to specific JSON property names:
```csharp
entity.Property(e => e.Id).ToJsonProperty("id");
entity.Property(e => e.PartitionKey).ToJsonProperty("pk");
```

## Testing Strategy

The test suite follows the same pattern as the direct SDK test:

1. **Database Setup**: Create unique database for each test run
2. **Document Creation**: Create test documents with various partition keys
3. **Read Operations**: Query documents with different filters
4. **Update Operations**: Modify documents and verify changes
5. **Delete Operations**: Remove documents and verify deletion
6. **Complex Scenarios**: Test message documents with nested data
7. **System Properties**: Verify ETag and other metadata handling
8. **Cleanup**: Remove test database

## Expected Output

The test will output detailed information about each operation, including:
- Entity creation and configuration
- Query results and document content
- Change tracking state transitions
- Error handling and exception details
- Performance and behavior differences from direct SDK

This allows for direct comparison between EF Core and direct SDK approaches to identify any differences in behavior or capabilities.
