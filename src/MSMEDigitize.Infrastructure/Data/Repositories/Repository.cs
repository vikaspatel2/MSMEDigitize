using MSMEDigitize.Core.DTOs;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MSMEDigitize.Core.Common;
using MSMEDigitize.Core.Interfaces;

namespace MSMEDigitize.Infrastructure.Data.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.FindAsync(new object[] { id }, ct);

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default)
        => await _dbSet.ToListAsync(ct);

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, ct);

    public virtual IQueryable<T> Query() => _dbSet.AsQueryable();

    public virtual async Task<PagedResult<T>> GetPagedAsync(int pageNumber, int pageSize,
        Expression<Func<T, bool>>? filter = null, CancellationToken ct = default)
    {
        var query = filter != null ? _dbSet.Where(filter) : _dbSet.AsQueryable();
        var total = await query.CountAsync(ct);
        var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedResult<T> { Items = items, TotalCount = total, PageNumber = pageNumber, PageSize = pageSize };
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        return entity;
    }

    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        var list = entities.ToList();
        await _dbSet.AddRangeAsync(list, ct);
        return list;
    }

    public virtual Task<T> UpdateAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }

    public virtual Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = _dbSet.Find(id);
        if (entity != null) _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
        => await _dbSet.AnyAsync(e => e.Id == id, ct);

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default)
        => filter == null ? await _dbSet.CountAsync(ct) : await _dbSet.CountAsync(filter, ct);
}