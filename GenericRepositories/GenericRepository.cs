using GenericRepositories.Interfaces;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace GenericRepositories
{
    /// <summary>
    /// Abstract base class that implements <see cref="IGenericRepository{T,U,TKey}"/> using EF Core.
    /// Subclass once per entity and inject the concrete <typeparamref name="U"/> context.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <typeparam name="U">The <see cref="DbContext"/> type that owns the entity set.</typeparam>
    /// <typeparam name="TKey">
    /// The primary-key type (e.g. <see cref="int"/> or <see cref="Guid"/>).
    /// Must be non-nullable.
    /// </typeparam>
    /// <example>
    /// <code>
    /// public class OrderRepository : GenericRepository&lt;Order, AppDbContext, int&gt;
    /// {
    ///     public OrderRepository(AppDbContext context, ILogger&lt;GenericRepository&lt;Order, AppDbContext, int&gt;&gt; logger)
    ///         : base(context, logger) { }
    /// }
    /// </code>
    /// </example>
    public abstract class GenericRepository<T, U, TKey> : IGenericRepository<T, U, TKey>
        where T : class
        where U : DbContext
        where TKey : notnull
    {
        /// <summary>The EF Core context used by this repository.</summary>
        protected readonly U _context;

        /// <summary>Logger scoped to this repository type.</summary>
        protected readonly ILogger<GenericRepository<T, U, TKey>> _logger;

        private bool disposedValue;

        /// <param name="context">The EF Core context to use for all data access.</param>
        /// <param name="logger">Logger provided by the DI container.</param>
        protected GenericRepository(U context, ILogger<GenericRepository<T, U, TKey>> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <inheritdoc/>
        public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
        {
            try
            {
                var addedEntity = await _context.Set<T>().AddAsync(entity, ct);
                return addedEntity.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(AddAsync));
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<T>> AllAsync(QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTracking, CancellationToken ct = default)
        {
            try
            {
                return await ApplyTracking(_context.Set<T>(), tracking).ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(AllAsync));
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<T>> AllAsync(int skip, int take, QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTracking, CancellationToken ct = default)
        {
            try
            {
                return await ApplyTracking(_context.Set<T>(), tracking).Skip(skip).Take(take).ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(AllAsync));
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTracking, CancellationToken ct = default)
        {
            try
            {
                return await ApplyTracking(_context.Set<T>(), tracking).Where(predicate).ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(FindAsync));
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate, QueryTrackingBehavior tracking = QueryTrackingBehavior.NoTracking, CancellationToken ct = default)
        {
            try
            {
                return await ApplyTracking(_context.Set<T>(), tracking).Where(predicate).FirstOrDefaultAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(FindFirstAsync));
                throw;
            }
        }

        private static IQueryable<T> ApplyTracking(IQueryable<T> query, QueryTrackingBehavior tracking) => tracking switch
        {
            QueryTrackingBehavior.NoTracking                    => query.AsNoTracking(),
            QueryTrackingBehavior.NoTrackingWithIdentityResolution => query.AsNoTrackingWithIdentityResolution(),
            _                                                   => query.AsTracking()
        };

        /// <inheritdoc/>
        public virtual async Task<T?> GetAsync(TKey id, CancellationToken ct = default)
        {
            try
            {
                return await _context.FindAsync<T>(new object[] { id }, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(GetAsync));
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                return await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(SaveChangesAsync));
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual T Update(T entity)
        {
            try
            {
                return _context.Update(entity).Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(Update));
                throw;
            }
        }

        /// <inheritdoc/>
        public virtual T Delete(T entity)
        {
            try
            {
                return _context.Remove(entity).Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(Delete));
                throw;
            }
        }

        /// <summary>
        /// Disposes the underlying <see cref="DbContext"/> when <paramref name="disposing"/> is <see langword="true"/>.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _context.Dispose();
                }
                disposedValue = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
