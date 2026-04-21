using GenericRepositories.Interfaces;
using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace GenericRepositories
{
    abstract class GenericRepository<T,U> : IGenericRepository<T,U>
        where T : class
        where U : DbContext
    {
        protected U _context;
        protected ILogger<GenericRepository<T,U>> _logger;
        private bool disposedValue;

        protected GenericRepository(U context, ILogger<GenericRepository<T,U>> logger)
        {
            _context = context;
            _logger = logger;
        }
        public virtual async Task<T> AddAsync(T entity)
        {
            try
            {
                EntityEntry<T> addedEntity = await _context.AddAsync(entity);
                return addedEntity.Entity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }

        }
        public virtual async Task<IEnumerable<T>> AllAsync()
        {
            try
            {
                IQueryable<T> query = _context.Set<T>().AsQueryable();
                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }

        }
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                IQueryable<T> query = _context.Set<T>().AsQueryable();
                query = query.Where(predicate);

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }

        }
        public virtual async Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate)
        {

            return (await FindAsync(predicate)).FirstOrDefault();
        }
        public virtual async Task<T?> GetAsync(Guid id)
        {
            try
            {
                return await _context.FindAsync<T>(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }
        public virtual async Task<int> SaveChangesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
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
                _logger.LogError(ex, ex.Message);
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
                _logger.LogError(ex, ex.Message);
                throw;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    _context.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GenericRepository()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
