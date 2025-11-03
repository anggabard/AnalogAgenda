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

    public async Task<bool> ExistsAsync<T>(string id) where T : BaseEntity
    {
        return await _context.Set<T>().AnyAsync(e => e.Id == id);
    }

    public async Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
    {
        return await _context.Set<T>().AnyAsync(predicate);
    }

    public async Task<T> AddAsync<T>(T entity) where T : BaseEntity
    {
        entity.CreatedDate = DateTime.UtcNow;
        entity.UpdatedDate = DateTime.UtcNow;
        
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
}

