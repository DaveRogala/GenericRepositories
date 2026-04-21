using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GenericRepositories.Interfaces
{
    /// <summary>
    /// Defines the standard data-access contract for an EF Core repository.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="U">The <see cref="DbContext"/> type that owns the entity set.</typeparam>
    /// <typeparam name="TKey">
    /// The primary-key type (e.g. <see cref="int"/> or <see cref="Guid"/>).
    /// Must be non-nullable.
    /// </typeparam>
    public interface IGenericRepository<T, U, TKey> : IDisposable
    where T : class
    where U : DbContext
    where TKey : notnull
    {
        /// <summary>Stages <paramref name="entity"/> for insertion. Call <see cref="SaveChangesAsync"/> to persist.</summary>
        /// <returns>The tracked entity as returned by EF Core.</returns>
        Task<T> AddAsync(T entity, CancellationToken ct = default);

        /// <summary>Stages <paramref name="entity"/> for update. Call <see cref="SaveChangesAsync"/> to persist.</summary>
        /// <returns>The tracked entity as returned by EF Core.</returns>
        T Update(T entity);

        /// <summary>Stages <paramref name="entity"/> for deletion. Call <see cref="SaveChangesAsync"/> to persist.</summary>
        /// <returns>The tracked entity as returned by EF Core.</returns>
        T Delete(T entity);

        /// <summary>Returns the entity with the given primary key, or <see langword="null"/> if not found.</summary>
        Task<T?> GetAsync(TKey id, CancellationToken ct = default);

        /// <summary>Returns every row in the entity's table. Avoid on large tables — prefer the paginated overload.</summary>
        Task<IEnumerable<T>> AllAsync(CancellationToken ct = default);

        /// <summary>Returns a page of rows. <paramref name="skip"/> rows are bypassed before <paramref name="take"/> are returned.</summary>
        Task<IEnumerable<T>> AllAsync(int skip, int take, CancellationToken ct = default);

        /// <summary>Returns all entities that satisfy <paramref name="predicate"/>.</summary>
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

        /// <summary>Returns the first entity that satisfies <paramref name="predicate"/>, or <see langword="null"/> if none match.</summary>
        Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

        /// <summary>Flushes all pending changes to the database.</summary>
        /// <returns>The number of rows written.</returns>
        Task<int> SaveChangesAsync(CancellationToken ct = default);
    }
}
