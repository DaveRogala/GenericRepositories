using GenericRepositories.Interfaces;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace GenericRepositories
{
    public abstract class GenericRepository<T, U, TKey> : IGenericRepository<T, U, TKey>
        where T : class
        where U : DbContext
        where TKey : notnull
    {
        protected readonly U _context;
        protected readonly ILogger<GenericRepository<T, U, TKey>> _logger;
        private bool disposedValue;

        protected GenericRepository(U context, ILogger<GenericRepository<T, U, TKey>> logger)
        {
            _context = context;
            _logger = logger;
        }

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

        public virtual async Task<IEnumerable<T>> AllAsync(CancellationToken ct = default)
        {
            try
            {
                return await _context.Set<T>().ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(AllAsync));
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> AllAsync(int skip, int take, CancellationToken ct = default)
        {
            try
            {
                return await _context.Set<T>().Skip(skip).Take(take).ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(AllAsync));
                throw;
            }
        }

        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        {
            try
            {
                return await _context.Set<T>().Where(predicate).ToListAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(FindAsync));
                throw;
            }
        }

        public virtual async Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        {
            try
            {
                return await _context.Set<T>().Where(predicate).FirstOrDefaultAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {Method}", nameof(FindFirstAsync));
                throw;
            }
        }

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

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
