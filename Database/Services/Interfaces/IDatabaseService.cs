using Database.DTOs;
using Database.Entities;
using System.Linq.Expressions;

namespace Database.Services.Interfaces;

public interface IDatabaseService
{
    Task<List<T>> GetAllAsync<T>() where T : BaseEntity;
    Task<List<T>> GetAllAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity;
    Task<List<T>> GetAllWithIncludesAsync<T>(params Expression<Func<T, object>>[] includes) where T : BaseEntity;
    Task<PagedResponseDto<T>> GetPagedAsync<T>(int page = 1, int pageSize = 10, Func<IQueryable<T>, IOrderedQueryable<T>>? sortFunc = null) where T : BaseEntity;
    Task<PagedResponseDto<T>> GetPagedAsync<T>(Expression<Func<T, bool>> predicate, int page = 1, int pageSize = 10, Func<IQueryable<T>, IOrderedQueryable<T>>? sortFunc = null) where T : BaseEntity;
    Task<PagedResponseDto<T>> GetPagedWithIncludesAsync<T>(int page = 1, int pageSize = 10, Func<IQueryable<T>, IOrderedQueryable<T>>? sortFunc = null, params Expression<Func<T, object>>[] includes) where T : BaseEntity;
    Task<T?> GetByIdAsync<T>(string id) where T : BaseEntity;
    Task<T?> GetByIdWithIncludesAsync<T>(string id, params Expression<Func<T, object>>[] includes) where T : BaseEntity;
    Task<bool> ExistsAsync<T>(string id) where T : BaseEntity;
    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity;
    Task<T> AddAsync<T>(T entity) where T : BaseEntity;
    Task UpdateAsync<T>(T entity) where T : BaseEntity;
    Task DeleteAsync<T>(string id) where T : BaseEntity;
    Task DeleteAsync<T>(T entity) where T : BaseEntity;
    Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : BaseEntity;
    Task DeleteRangeAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity;
    Task<int> SaveChangesAsync();

    /// <summary>Runs <paramref name="action"/> inside a single database transaction (all <see cref="SaveChangesAsync"/> calls share it).</summary>
    Task ExecuteInTransactionAsync(Func<Task> action);

    /// <summary>Query any mapped EF entity type (including junction rows that are not <see cref="BaseEntity"/>).</summary>
    Task<List<T>> GetEntitiesAsync<T>(Expression<Func<T, bool>> predicate) where T : class;

    /// <summary>Remove rows matching <paramref name="removePredicate"/> and insert <paramref name="newEntities"/> in one save.</summary>
    Task ReplaceEntitiesAsync<T>(Expression<Func<T, bool>> removePredicate, IEnumerable<T> newEntities) where T : class;

    Task AddEntitiesAsync<T>(IEnumerable<T> entities) where T : class;

    Task DeleteEntitiesAsync<T>(Expression<Func<T, bool>> predicate) where T : class;

    /// <summary>Filtered query with includes for <see cref="BaseEntity"/> types.</summary>
    Task<List<T>> GetAllWhereWithIncludesAsync<T>(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
        where T : BaseEntity;
}

