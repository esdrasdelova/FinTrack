using System.Linq.Expressions;
using FinTrack.Data;
using FinTrack.Models;
using FinTrack.Services;
using Microsoft.EntityFrameworkCore;

namespace FinTrack.Repositories;

public class Repository<T> : IRepository<T> where T : class, IUserOwnedEntity
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public Repository(ApplicationDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    /// <summary>Toda consulta passa obrigatoriamente pelo filtro de proprietário.</summary>
    public IQueryable<T> Query(bool asNoTracking = true)
    {
        var query = _context.Set<T>().Where(x => x.UserId == _currentUser.UserId);
        return asNoTracking ? query.AsNoTracking() : query;
    }

    public Task<T?> GetByIdAsync(int id, bool asNoTracking = false) =>
        (asNoTracking ? Query(true) : Query(false)).FirstOrDefaultAsync(x => x.Id == id);

    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true) =>
        (asNoTracking ? Query(true) : Query(false)).FirstOrDefaultAsync(predicate);

    public Task<IReadOnlyList<T>> GetAllAsync() => ToListAsync(Query());

    public Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        ToListAsync(Query().Where(predicate));

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate) => Query().AnyAsync(predicate);

    public Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null) =>
        predicate is null ? Query().CountAsync() : Query().CountAsync(predicate);

    public async Task<T> AddAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.UserId))
            entity.UserId = _currentUser.UserId;

        await _context.Set<T>().AddAsync(entity);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
    {
        foreach (var entity in entities)
        {
            if (string.IsNullOrWhiteSpace(entity.UserId))
                entity.UserId = _currentUser.UserId;
        }
        await _context.Set<T>().AddRangeAsync(entities);
    }

    public void Update(T entity)
    {
        entity.UserId = _currentUser.UserId;
        _context.Set<T>().Update(entity);
    }

    public void Remove(T entity) => _context.Set<T>().Remove(entity);

    public void RemoveRange(IEnumerable<T> entities) => _context.Set<T>().RemoveRange(entities);

    private static async Task<IReadOnlyList<T>> ToListAsync(IQueryable<T> query) =>
        await query.ToListAsync();
}
