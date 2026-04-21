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
| `GetAsync(id, ct)` | Returns entity by primary key, or `null`. |
| `AllAsync(ct)` | Returns all rows. Avoid on large tables. |
| `AllAsync(skip, take, ct)` | Returns a page of rows. |
| `FindAsync(predicate, ct)` | Returns all rows matching the LINQ predicate. |
| `FindFirstAsync(predicate, ct)` | Returns the first matching row, or `null`. |

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
