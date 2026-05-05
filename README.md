# GenericRepositories

A shared base library that provides a generic EF Core repository, eliminating the need to copy the same data-access boilerplate across ETL projects.

## Requirements

- .NET 10
- Entity Framework Core 10

## Usage

### 1. Create a concrete repository

Subclass `GenericRepository<TEntity, TContext, TKey>`, specifying your entity, DbContext, and primary-key types:

```csharp
// Guid primary key
public class OrderRepository : GenericRepository<Order, AppDbContext, Guid>
{
    public OrderRepository(AppDbContext context, ILogger<GenericRepository<Order, AppDbContext, Guid>> logger)
        : base(context, logger) { }
}

// Integer primary key
public class ProductRepository : GenericRepository<Product, AppDbContext, int>
{
    public ProductRepository(AppDbContext context, ILogger<GenericRepository<Product, AppDbContext, int>> logger)
        : base(context, logger) { }
}
```

### 2. Register with the DI container

```csharp
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<ProductRepository>();
```

### 3. Inject and use

```csharp
public class OrderService(OrderRepository repo)
{
    public async Task ProcessAsync(CancellationToken ct)
    {
        // Paginated read — avoids loading unbounded result sets
        var batch = await repo.AllAsync(skip: 0, take: 100, ct);

        foreach (var order in batch)
        {
            order.Status = "Processed";
            repo.Update(order);
        }

        await repo.SaveChangesAsync(ct);
    }
}
```

## API reference

All mutating methods (`AddAsync`, `Update`, `Delete`) only stage changes in the EF Core change tracker. Call `SaveChangesAsync` to flush them to the database.

| Method | Description |
|---|---|
| `AddAsync(entity, ct)` | Stages entity for insertion. |
| `Update(entity)` | Stages entity for update. |
| `Delete(entity)` | Stages entity for deletion. |
| `SaveChangesAsync(ct)` | Flushes staged changes; returns the row count written. |
| `GetAsync(id, ct)` | Returns entity by primary key, or `null`. Always uses the change tracker (see note below). |
| `AllAsync(tracking, ct)` | Returns all rows. Avoid on large tables. |
| `AllAsync(skip, take, orderBy, tracking, ct)` | Returns a page of rows in the specified order. |
| `FindAsync(predicate, tracking, ct)` | Returns all rows matching the LINQ predicate. |
| `FindFirstAsync(predicate, orderBy, tracking, ct)` | Returns the first matching row in the specified order, or `null`. |

### Ordering

The `orderBy` parameter accepts a function that applies any combination of `OrderBy`, `OrderByDescending`, `ThenBy`, and `ThenByDescending`:

```csharp
// Single key, ascending
var page = await repo.AllAsync(skip: 0, take: 50, q => q.OrderBy(e => e.Name));

// Single key, descending
var latest = await repo.FindFirstAsync(e => e.IsActive, q => q.OrderByDescending(e => e.CreatedAt));

// Multi-key
var page = await repo.AllAsync(skip: 0, take: 50, q => q.OrderByDescending(e => e.Date).ThenBy(e => e.Id));
```

`orderBy` is required on `AllAsync(skip, take, ...)` and `FindFirstAsync` — pagination and "first" queries over an unordered set produce non-deterministic results.

### Change tracking

All read methods accept a `QueryTrackingBehavior` parameter (defaulting to `NoTracking`, which is the right choice for most ETL workloads):

```csharp
// Default — entities are tracked, matching EF Core's own default
var orders = await repo.AllAsync();

// Opt out of tracking for read-only or ETL workloads to reduce overhead
var orders = await repo.AllAsync(QueryTrackingBehavior.NoTracking);

// NoTrackingWithIdentityResolution — no tracking but related entities
// loaded from multiple queries are resolved to the same object instance
var orders = await repo.FindAsync(o => o.CustomerId == id, QueryTrackingBehavior.NoTrackingWithIdentityResolution);
```

> **Note on `GetAsync`:** EF Core's `FindAsync` always checks the change tracker before querying the database. This is intentional — if the entity is already loaded it avoids a round-trip — but it means the `tracking` parameter is not available on `GetAsync`. If you need a guaranteed no-tracking lookup by key, use `FindFirstAsync` with a predicate on the key property instead.

## Extending a repository

All methods are `virtual`, so concrete repositories can override any behaviour:

```csharp
public class OrderRepository : GenericRepository<Order, AppDbContext, Guid>
{
    // ...

    // Example: always eager-load related lines
    public override async Task<IEnumerable<Order>> AllAsync(CancellationToken ct = default)
    {
        return await _context.Set<Order>()
            .Include(o => o.Lines)
            .ToListAsync(ct);
    }
}
```

## Running the tests

```bash
dotnet test
```

Tests use the EF Core in-memory provider — no database required.
