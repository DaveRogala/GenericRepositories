using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GenericRepositories.Interfaces
{
    internal interface IGenericRepository<T,U> : IDisposable
    where T : class
    where U : DbContext
    {
        Task<T> AddAsync(T entity);
        T Update(T entity);
        Task<T?> GetAsync(Guid id);
        Task<IEnumerable<T>> AllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate);
        Task<int> SaveChangesAsync();
        T Delete(T entity);
    }
}
