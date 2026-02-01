# IndexSmith.EFCore.SqlServer

**Opinionated, heuristic-driven indexing for EF Core (SQL Server)**

`IndexSmith` automatically applies **safe, data-centric SQL Server indexes** to your **code-first EF Core models** during migration generation—without taking control away from developers.

> Think of it as *DBA instincts, encoded into EF Core conventions*.

---

## Why IndexSmith Exists

Entity Framework Core intentionally avoids automatically creating most indexes because:

- Indexing is workload-dependent  
- Bad indexes are worse than no indexes  
- ORMs cannot reliably infer intent  

However, many real-world systems follow **repeatable, data-centric patterns**:

- Foreign keys are almost always queried  
- Soft deletes are almost always filtered  
- Tenant + status columns are ubiquitous  
- Enum/state columns are frequently filtered  

`IndexSmith` fills this gap by:

- Using **conservative heuristics**
- Injecting indexes **at model finalization**
- Producing **explicit, reviewable migrations**
- Allowing **opt-in and opt-out overrides**

---

## Key Guarantees

- ✅ No runtime schema changes  
- ✅ No index removal  
- ✅ No query interception  
- ✅ Deterministic output  
- ✅ SQL Server–aware  
- ✅ Fully compatible with EF Core migrations  

---

## How It Works

1. EF Core builds the model  
2. `IndexSmith` runs during **model finalization**  
3. Entities and properties are analyzed  
4. Index candidates are **scored**  
5. Qualifying indexes are injected into the model  
6. Migrations generate explicit `CreateIndex` calls  

---

## Installation

```bash
dotnet add package IndexSmith.EFCore.SqlServer
```

## Requirements

- .NET 6+
- EF Core 7+
- Microsoft.EntityFrameworkCore.SqlServer

## Usage
Enable `IndexSmith` in your `DbContext`:
```csharp
protected override void OnConfiguring(DbContextOptionsBuilder options)
{
    options
        .UseSqlServer(connectionString)
        .UseIndexSmith();
}
```
Indexes will be automatically applied during migration generation.

## Configuration
Customize `IndexSmith` behavior in `OnModelCreating`:
```csharp
options
    .UseSqlServer(connectionString)
    .UseIndexSmith(o =>
    {
        o.ScoreThreshold = 50;
        o.EnableForeignKeyIndexes = true;
        o.EnableSoftDeleteIndexes = true;
        o.EnableEnumIndexes = true;
    });
```
### Configuration Options
| Option | Description | Default |
|--------|-------------|---------|
|`ScoreThreshold`| Minimum score for an index to be created | 50 |
|`EnableForeignKeyIndexes`| Enable indexing of foreign key properties | true |
|`EnableSoftDeleteIndexes`| Enable indexing of soft delete columns | true |
|`EnableEnumIndexes`| Enable indexing of enum/state columns | true |

## Automatic Indexing Rules
###  **Foreign Keys**: Indexes are created on foreign key properties to optimize joins.
```csharp
public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; }
}

```
Creates
```sql
IX_Orders_CustomerId
```
###  **Soft Deletes**: Indexes are created on soft delete columns (e.g., `IsDeleted`) to optimize filtering.
```csharp
public class Order
{
    public int Id { get; set; }
    public bool IsDeleted { get; set; }
}
```
Creates
```sql
IX_Orders_IsDeleted
```
###  Multitenant + Status Columns: Indexes are created on common multitenant and status columns.
```csharp
public class Order
{
    public int Id { get; set; }
    public Guid TenantId { get; set; }
    public bool IsDeleted { get; set; }
}
```
Creates
```sql
IX_Orders_TenantId_IsDeleted
```

###   **Enum/State Columns**: Indexes are created on enum or state columns to optimize filtering.
```csharp
public enum OrderStatus
{
    Pending,
    Paid,
    Shipped
}

public class Order
{
    public int Id { get; set; }
    public OrderStatus Status { get; set; }
}
```
Creates
```sql
IX_Orders_Status
```

## Attribute-Based Overrides: Use attributes to control indexing behavior.
### Force index creation:
```csharp
[AutoIndex]
public string Email { get; set; }
```

### Prevent index creation:
```csharp
[NoAutoIndex]
public string Notes { get; set; }
```

### Composite index example:
```csharp
[CompositeAutoIndex(nameof(TenantId), nameof(IsDeleted))]
public class Order
{
    public Guid TenantId { get; set; }
    public bool IsDeleted { get; set; }
}
```

### Migration output example:
Indexes are generated explicitly:
```csharp
migrationBuilder.CreateIndex(
    name: "IX_Orders_TenantId_IsDeleted",
    table: "Orders",
    columns: new[] { "TenantId", "IsDeleted" });
```

## What is not Indexed by Default
- nvarchar(max)
- Large strings (>256 chars)
- Audit columns (CreatedAt, UpdatedAt)
- Low-cardinality booleans (alone)
- JSON / computed columns
- All can be overridden explicitly.

## Diagnostics
-- Optional diagnostics logging:
```
[IndexSmith] Orders(TenantId, IsDeleted)
Score: 72
Rules: TenantId(+40), IsDeleted(+30)
```

## When to Use IndexSmith
- Data-centric teams
- Multi-tenant systems
- Soft-delete-heavy domains
- CRUD-heavy applications

## When Not to Use IndexSmith
- Heavily workload-tuned systems
- OLAP-only databases
- Teams unwilling to review migrations

## Roadmap
- Filtered indexes (IsDeleted = 0)
- INCLUDE columns
- CLI index report
- Multi-provider support
- Index diff analysis tool

## Philosophy
IndexSmith does not replace DBAs.
It replaces forgetting to add obvious indexes.

You control the model.
IndexSmith automates the boring parts.

## License
MIT License. See [LICENSE](LICENSE) for details.