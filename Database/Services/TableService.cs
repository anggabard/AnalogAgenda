using Azure.Data.Tables;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using System.Linq.Expressions;

namespace Database.Services
{
    public sealed class TableService(AzureAd azureAdCfg, Storage storageCfg) : BaseAzureService<TableClient>(azureAdCfg, storageCfg, "table"), ITableService
    {
        public TableClient GetTable(string tableName) => GetValidatedClient(tableName);

        public TableClient GetTable(TableName table) => GetTable(table.ToString());

        protected override void ValidateResourceName(string resourceName)
        {
            if (!resourceName.IsTable())
                throw new ArgumentException($"Error: '{resourceName}' is not a valid Table.");
        }

        protected override TableClient CreateClient(string resourceName) =>
            new(AccountUri, resourceName, Credential);

        public static TableName GetTableName<T>() where T : BaseEntity
        {
            var method = typeof(T).GetMethod("GetTable") ?? throw new InvalidOperationException($"Method 'GetTable' not found on type {typeof(T).Name}");

            if (Activator.CreateInstance(typeof(T)) is not BaseEntity instance)
                throw new InvalidOperationException($"Could not create instance of type {typeof(T).Name}");

            var tableObj = method.Invoke(instance, null);
            if (tableObj is not TableName tableName)
                throw new InvalidOperationException($"Returned value from 'GetTable' is not of type TableName");

            return tableName;
        }

        public async Task<List<T>> GetTableEntriesAsync<T>() where T : BaseEntity
        {
            TableName tableName = GetTableName<T>();

            var entities = new List<T>();
            await foreach (var entity in GetTable(tableName).QueryAsync<T>())
            {
                entities.Add(entity);
            }

            return entities;
        }

        public async Task<List<T>> GetTableEntriesAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
        {
            TableName tableName = GetTableName<T>();

            var entities = new List<T>();
            await foreach (T entity in GetTable(tableName).QueryAsync(predicate))
            {
                entities.Add(entity);
            }

            return entities;
        }

        public async Task<T?> GetTableEntryIfExistsAsync<T>(string rowKey) where T : BaseEntity
        {
            var tableName = GetTableName<T>();
            var table = GetTable(tableName);

            var entity = await table.GetEntityIfExistsAsync<T>(tableName.PartitionKey(), rowKey);

            return entity.HasValue ? entity.Value : null;

        }

        public async Task<PagedResponseDto<T>> GetTableEntriesPagedAsync<T>(int page = 1, int pageSize = 10, Func<IEnumerable<T>, IOrderedEnumerable<T>>? sortFunc = null) where T : BaseEntity
        {
            var allEntities = await GetTableEntriesAsync<T>();
            return GetTableEntriesPagedInternal(allEntities, page, pageSize, sortFunc);
        }

        public async Task<PagedResponseDto<T>> GetTableEntriesPagedAsync<T>(Expression<Func<T, bool>> predicate, int page = 1, int pageSize = 10, Func<IEnumerable<T>, IOrderedEnumerable<T>>? sortFunc = null) where T : BaseEntity
        {
            var allEntities = await GetTableEntriesAsync(predicate);
            return GetTableEntriesPagedInternal(allEntities, page, pageSize, sortFunc);
        }

        private PagedResponseDto<T> GetTableEntriesPagedInternal<T>(List<T> allEntities, int page, int pageSize, Func<IEnumerable<T>, IOrderedEnumerable<T>>? sortFunc = null) where T : BaseEntity
        {
            // Apply sorting - default to UpdatedDate descending if no sort function provided
            IEnumerable<T> sortedEntities = sortFunc != null
                ? sortFunc(allEntities)
                : allEntities.OrderByDescending(e => e.UpdatedDate);

            int skip = (page - 1) * pageSize;
            var pagedData = sortedEntities.Skip(skip).Take(pageSize).ToList();

            return new PagedResponseDto<T>
            {
                Data = pagedData,
                TotalCount = allEntities.Count,
                PageSize = pageSize,
                CurrentPage = page
            };
        }

        public async Task<bool> EntryExistsAsync(BaseEntity entity)
        {
            var table = GetTable(entity.GetTable());

            var entry = await table.GetEntityIfExistsAsync<BaseEntity>(entity.PartitionKey, entity.RowKey);

            return entry.HasValue;
        }

        public async Task<bool> EntryExistsAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
        {
            var tableName = GetTableName<T>();

            await foreach (var _ in GetTable(tableName).QueryAsync(predicate))
            {
                return true;
            }

            return false;
        }

        public async Task DeleteTableEntryAsync<T>(string rowKey) where T : BaseEntity
        {
            var tableName = GetTableName<T>();
            var table = GetTable(tableName);

            await table.DeleteEntityAsync(tableName.PartitionKey(), rowKey);
        }

        public async Task DeleteTableEntriesAsync<T>(IEnumerable<T> entities) where T : BaseEntity
        {
            if (!entities.Any()) return;

            var tableName = GetTableName<T>();
            var table = GetTable(tableName);

            foreach (var entity in entities)
            {
                await table.DeleteEntityAsync(tableName.PartitionKey(), entity.RowKey);
            }
        }

        public async Task DeleteTableEntriesAsync<T>(Expression<Func<T, bool>> predicate) where T : BaseEntity
        {
            TableName tableName = GetTableName<T>();
            var table = GetTable(tableName);

            await foreach (T entity in GetTable(tableName).QueryAsync(predicate))
            {
                await table.DeleteEntityAsync(tableName.PartitionKey(), entity.RowKey);
            }
        }
    }
}
