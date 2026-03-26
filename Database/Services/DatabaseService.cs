using Database.Data;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Database.Services;

public class DatabaseService(AnalogAgendaDbContext context) : IDatabaseService
{
    private readonly AnalogAgendaDbContext _context = context;

    public async Task<List<T>> GetAllAsync<T>() where T : BaseEntity
    {
        return await _context.Set<T>().ToListAsync();
    }

    public async Task<List<T>> GetAllAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
    {
        return await _context.Set<T>().Where(predicate).ToListAsync();
    }

    public async Task<List<T>> GetAllWithIncludesAsync<T>(params Expression<Func<T, object>>[] includes) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>();
        
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        
        return await query.ToListAsync();
    }

    public async Task<PagedResponseDto<T>> GetPagedAsync<T>(
        int page = 1, 
        int pageSize = 10, 
        Func<IQueryable<T>, IOrderedQueryable<T>>? sortFunc = null) where T : BaseEntity
    {
        var query = _context.Set<T>().AsQueryable();
        return await GetPagedInternalAsync(query, page, pageSize, sortFunc);
    }

    public async Task<PagedResponseDto<T>> GetPagedAsync<T>(
        Expression<Func<T, bool>> predicate, 
        int page = 1, 
        int pageSize = 10, 
        Func<IQueryable<T>, IOrderedQueryable<T>>? sortFunc = null) where T : BaseEntity
    {
        var query = _context.Set<T>().Where(predicate);
        return await GetPagedInternalAsync(query, page, pageSize, sortFunc);
    }

    public async Task<PagedResponseDto<T>> GetPagedWithIncludesAsync<T>(
        int page = 1, 
        int pageSize = 10, 
        Func<IQueryable<T>, IOrderedQueryable<T>>? sortFunc = null, 
        params Expression<Func<T, object>>[] includes) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>();
        
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        
        return await GetPagedInternalAsync(query, page, pageSize, sortFunc);
    }

    private async Task<PagedResponseDto<T>> GetPagedInternalAsync<T>(
        IQueryable<T> query, 
        int page, 
        int pageSize, 
        Func<IQueryable<T>, IOrderedQueryable<T>>? sortFunc) where T : BaseEntity
    {
        // Get total count
        var totalCount = await query.CountAsync();

        // Apply sorting - default to UpdatedDate descending if no sort function provided
        IQueryable<T> sortedQuery = sortFunc != null
            ? sortFunc(query)
            : query.OrderByDescending(e => e.UpdatedDate);

        // Apply pagination
        var data = await sortedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponseDto<T>
        {
            Data = data,
            TotalCount = totalCount,
            PageSize = pageSize,
            CurrentPage = page
        };
    }

    public async Task<T?> GetByIdAsync<T>(string id) where T : BaseEntity
    {
        return await _context.Set<T>().FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<T?> GetByIdWithIncludesAsync<T>(string id, params Expression<Func<T, object>>[] includes) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>();
        
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        
        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<bool> ExistsAsync<T>(string id) where T : BaseEntity
    {
        return await _context.Set<T>().AnyAsync(e => e.Id == id);
    }

    public async Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
    {
        return await _context.Set<T>().AnyAsync(predicate);
    }

    public async Task<int> GetNextSessionIndexAsync()
    {
        if (!await _context.Set<SessionEntity>().AnyAsync())
            return 1;
        return await _context.Set<SessionEntity>().MaxAsync(s => s.Index) + 1;
    }

    public async Task<T> AddAsync<T>(T entity) where T : BaseEntity
    {
        if (string.IsNullOrEmpty(entity.Id))
            entity.Id = entity.GetId();

        await _context.Set<T>().AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public async Task UpdateAsync<T>(T entity) where T : BaseEntity
    {
        entity.UpdatedDate = DateTime.UtcNow;
        _context.Set<T>().Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync<T>(string id) where T : BaseEntity
    {
        var entity = await GetByIdAsync<T>(id);
        if (entity != null)
        {
            _context.Set<T>().Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync<T>(T entity) where T : BaseEntity
    {
        _context.Set<T>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : BaseEntity
    {
        _context.Set<T>().RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteRangeAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
    {
        var entities = await _context.Set<T>().Where(predicate).ToListAsync();
        _context.Set<T>().RemoveRange(entities);
        await _context.SaveChangesAsync();
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task ExecuteInTransactionAsync(Func<Task> action)
    {
        // In-memory provider does not support transactions; still run the action (tests use this provider).
        if (string.Equals(
                _context.Database.ProviderName,
                "Microsoft.EntityFrameworkCore.InMemory",
                StringComparison.Ordinal))
        {
            await action();
            return;
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await action();
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<List<T>> GetEntitiesAsync<T>(Expression<Func<T, bool>> predicate) where T : class =>
        await _context.Set<T>().Where(predicate).ToListAsync();

    public async Task ReplaceEntitiesAsync<T>(Expression<Func<T, bool>> removePredicate, IEnumerable<T> newEntities)
        where T : class
    {
        var old = await _context.Set<T>().Where(removePredicate).ToListAsync();
        if (old.Count > 0)
            _context.Set<T>().RemoveRange(old);
        var list = newEntities.ToList();
        if (list.Count > 0)
            await _context.Set<T>().AddRangeAsync(list);
        await _context.SaveChangesAsync();
    }

    public async Task AddEntitiesAsync<T>(IEnumerable<T> entities) where T : class
    {
        var list = entities.ToList();
        if (list.Count == 0)
            return;
        await _context.Set<T>().AddRangeAsync(list);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteEntitiesAsync<T>(Expression<Func<T, bool>> predicate) where T : class
    {
        var rows = await _context.Set<T>().Where(predicate).ToListAsync();
        if (rows.Count == 0)
            return;
        _context.Set<T>().RemoveRange(rows);
        await _context.SaveChangesAsync();
    }

    public async Task<List<T>> GetAllWhereWithIncludesAsync<T>(
        Expression<Func<T, bool>> predicate,
        params Expression<Func<T, object>>[] includes) where T : BaseEntity
    {
        IQueryable<T> query = _context.Set<T>().Where(predicate);
        foreach (var include in includes)
            query = query.Include(include);
        return await query.ToListAsync();
    }

    public async Task<List<IdeaEntity>> GetAllIdeasWithSessionLinksAsync() =>
        await _context.Set<IdeaEntity>()
            .Include(i => i.IdeaSessions)
            .ThenInclude(j => j.Session)
            .ToListAsync();

    public async Task<IdeaEntity?> GetIdeaByIdWithSessionLinksAsync(string id) =>
        await _context.Set<IdeaEntity>()
            .Include(i => i.IdeaSessions)
            .ThenInclude(j => j.Session)
            .FirstOrDefaultAsync(e => e.Id == id);

    public async Task<SessionEntity?> GetSessionByIdWithFullIncludesAsync(string id) =>
        await _context.Set<SessionEntity>()
            .AsSplitQuery()
            .Include(s => s.UsedDevKits)
            .Include(s => s.DevelopedFilms)
            .Include(s => s.IdeaSessions)
            .ThenInclude(j => j.Idea)
            .FirstOrDefaultAsync(s => s.Id == id);
}

