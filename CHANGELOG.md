# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

---

## [10.1.0] - 2026-04-21

This release is a significant overhaul of the library. Several **breaking changes** are present — see the migration guide at the bottom of this section before upgrading.

### Breaking changes

#### Generic type signature changed — `TKey` added

`IGenericRepository<T, U>` and `GenericRepository<T, U>` now require a third type parameter `TKey` for the primary-key type. All concrete repositories must be updated.

```csharp
// Before
public class OrderRepository : GenericRepository<Order, AppDbContext> { ... }

// After
public class OrderRepository : GenericRepository<Order, AppDbContext, Guid> { ... }
public class ProductRepository : GenericRepository<Product, AppDbContext, int> { ... }
```

#### `GetAsync` key type is now `TKey` instead of `Guid`

```csharp
// Before
Task<T?> GetAsync(Guid id, CancellationToken ct = default);

// After
Task<T?> GetAsync(TKey id, CancellationToken ct = default);
```

#### All read methods now accept `QueryTrackingBehavior` instead of no tracking control

A `QueryTrackingBehavior tracking` parameter has been added to `AllAsync`, `FindAsync`, and `FindFirstAsync`. The default is `QueryTrackingBehavior.NoTracking`. Callers that relied on EF Core's default tracking behaviour (track all) must now pass `QueryTrackingBehavior.TrackAll` explicitly.

```csharp
// Before — entities were tracked by default
var orders = await repo.AllAsync();

// After — NoTracking is the default; opt in to tracking explicitly
var orders = await repo.AllAsync();                                       // no tracking
var orders = await repo.AllAsync(QueryTrackingBehavior.TrackAll);        // tracked
```

#### `AllAsync(skip, take, ...)` and `FindFirstAsync` now require an `orderBy` parameter

Pagination and "first" queries over an unordered set are non-deterministic. `orderBy` is now a required parameter on both methods. The parameter type is `Func<IQueryable<T>, IOrderedQueryable<T>>`, which supports ascending, descending, and multi-key ordering.

```csharp
// Before
var page = await repo.AllAsync(skip: 0, take: 50);
var first = await repo.FindFirstAsync(e => e.IsActive);

// After
var page = await repo.AllAsync(skip: 0, take: 50, q => q.OrderBy(e => e.CreatedAt));
var first = await repo.FindFirstAsync(e => e.IsActive, q => q.OrderBy(e => e.CreatedAt));
```

#### `CancellationToken` added to all async methods

All async methods now accept a `CancellationToken ct = default` parameter. Existing call sites without a token continue to compile unchanged, but callers should pass a token in long-running ETL contexts.

#### `IGenericRepository` and `GenericRepository` are now `public`

Both types were previously `internal`, making them inaccessible outside the assembly. They are now `public`. This is a breaking change only for code that relied on the types being hidden (e.g. reflection-based tooling scanning for internal types).

---

### Added

- **`TKey` generic parameter** — supports any non-nullable primary-key type (`int`, `Guid`, `long`, `string`, etc.)
- **`QueryTrackingBehavior` parameter on all read methods** — defaults to `NoTracking`, which is appropriate for ETL read workloads. Pass `NoTrackingWithIdentityResolution` when related entities are loaded across multiple queries and must resolve to the same in-memory instance.
- **`orderBy` parameter on `AllAsync(skip, take)` and `FindFirstAsync`** — accepts `Func<IQueryable<T>, IOrderedQueryable<T>>` to support single-key, multi-key, ascending, and descending ordering.
- **Paginated `AllAsync(int skip, int take, ...)`** overload.
- **`CancellationToken` on all async methods.**
- **xUnit test project** (`GenericRepositories.Tests`) covering all repository methods using the EF Core in-memory provider. No database required to run tests.
- **XML documentation comments** on all public members.
- **README** with usage examples, API reference, and migration guidance.

### Fixed

- `FindFirstAsync` previously called `FindAsync` and applied `FirstOrDefault` in memory, loading every matching row. It now issues a single `FirstOrDefaultAsync` query with a `LIMIT 1`.
- `LogError` calls previously passed `ex.Message` as the message template, which would throw `FormatException` if the message contained curly braces. Now uses a static template: `"Error in {Method}"`.
- `_context.AddAsync` in `AddAsync` replaced with `_context.Set<T>().AddAsync` for consistency with all other methods.
- Removed redundant `.AsQueryable()` calls on `Set<T>()`, which already returns `IQueryable<T>`.
- Removed auto-generated `// TODO` scaffolding comments from the `Dispose` pattern.

### Changed

- `_context` and `_logger` fields are now `readonly`.

---

### Migration guide

1. **Add `TKey` to all subclasses:**
   ```csharp
   // Guid key
   class OrderRepo : GenericRepository<Order, AppDbContext, Guid>
   // Int key
   class ProductRepo : GenericRepository<Product, AppDbContext, int>
   ```

2. **Update DI registrations** if they reference the interface type directly:
   ```csharp
   services.AddScoped<IGenericRepository<Order, AppDbContext, Guid>, OrderRepository>();
   ```

3. **Add `orderBy` to paginated calls and `FindFirstAsync`:**
   ```csharp
   await repo.AllAsync(0, 100, q => q.OrderBy(e => e.Id));
   await repo.FindFirstAsync(predicate, q => q.OrderBy(e => e.Id));
   ```

4. **Pass `QueryTrackingBehavior.TrackAll`** anywhere you were relying on tracked entities being returned:
   ```csharp
   var entity = await repo.FindFirstAsync(predicate, q => q.OrderBy(e => e.Id), QueryTrackingBehavior.TrackAll);
   ```

---

## [10.0.6] - initial release

Initial implementation of `GenericRepository<T, U>` and `IGenericRepository<T, U>`.
