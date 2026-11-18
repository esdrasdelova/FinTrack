using System.Linq.Expressions;
using FinTrack.Models;

namespace FinTrack.Repositories;

/// <summary>Repositório genérico com escopo obrigatório por usuário autenticado.</summary>
public interface IRepository<T> where T : class, IUserOwnedEntity
{
    IQueryable<T> Query(bool asNoTracking = true);
    Task<T?> GetByIdAsync(int id, bool asNoTracking = false);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true);
    Task<IReadOnlyList<T>> GetAllAsync();
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
    Task<T> AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
}
