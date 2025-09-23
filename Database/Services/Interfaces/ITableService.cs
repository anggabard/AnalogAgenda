using Azure.Data.Tables;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using System.Linq.Expressions;

namespace Database.Services.Interfaces;

public interface ITableService
{
    TableClient GetTable(string tableName);
    TableClient GetTable(TableName table);
    Task<List<T>> GetTableEntriesAsync<T>() where T : BaseEntity;
    Task<List<T>> GetTableEntriesAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity;
    Task<PagedResponseDto<T>> GetTableEntriesPagedAsync<T>(int page = 1, int pageSize = 10) where T : BaseEntity;
    Task<PagedResponseDto<T>> GetTableEntriesPagedAsync<T>(Expression<Func<T, bool>> predicate, int page = 1, int pageSize = 10) where T : BaseEntity;
    Task<T?> GetTableEntryIfExistsAsync<T>(string rowKey) where T : BaseEntity;
    Task<bool> EntryExistsAsync(BaseEntity entity);
    Task DeleteTableEntryAsync<T>(string rowKey) where T : BaseEntity;
    Task DeleteTableEntriesAsync<T>(IEnumerable<T> entities) where T : BaseEntity;
    Task DeleteTableEntriesAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity;
}
