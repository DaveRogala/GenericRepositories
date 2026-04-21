using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace GenericRepositories.Interfaces
{
    public interface IGenericRepository<T, U, TKey> : IDisposable
    where T : class
    where U : DbContext
    where TKey : notnull
    {
        Task<T> AddAsync(T entity, CancellationToken ct = default);
        T Update(T entity);
        Task<T?> GetAsync(TKey id, CancellationToken ct = default);
        Task<IEnumerable<T>> AllAsync(CancellationToken ct = default);
        Task<IEnumerable<T>> AllAsync(int skip, int take, CancellationToken ct = default);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
        Task<int> SaveChangesAsync(CancellationToken ct = default);
        T Delete(T entity);
    }
}
