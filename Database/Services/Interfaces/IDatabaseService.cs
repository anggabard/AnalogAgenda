using Database.DTOs;
using Database.Entities;
using System.Linq.Expressions;

namespace Database.Services.Interfaces;

public interface IDatabaseService
{
    Task<List<T>> GetAllAsync<T>() where T : BaseEntity;
    Task<List<T>> GetAllAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity;
    Task<PagedResponseDto<T>> GetPagedAsync<T>(int page = 1, int pageSize = 10, Func<IQueryable<T>, IOrderedQueryable<T>>? sortFunc = null) where T : BaseEntity;
    Task<PagedResponseDto<T>> GetPagedAsync<T>(Expression<Func<T, bool>> predicate, int page = 1, int pageSize = 10, Func<IQueryable<T>, IOrderedQueryable<T>>? sortFunc = null) where T : BaseEntity;
    Task<T?> GetByIdAsync<T>(string id) where T : BaseEntity;
    Task<bool> ExistsAsync<T>(string id) where T : BaseEntity;
    Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity;
    Task<T> AddAsync<T>(T entity) where T : BaseEntity;
    Task UpdateAsync<T>(T entity) where T : BaseEntity;
    Task DeleteAsync<T>(string id) where T : BaseEntity;
    Task DeleteAsync<T>(T entity) where T : BaseEntity;
    Task DeleteRangeAsync<T>(IEnumerable<T> entities) where T : BaseEntity;
    Task DeleteRangeAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity;
    Task<int> SaveChangesAsync();
}

