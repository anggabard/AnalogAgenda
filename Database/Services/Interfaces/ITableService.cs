using Azure.Data.Tables;
using Database.DBObjects.Enums;
using Database.Entities;

namespace Database.Services.Interfaces;

public interface ITableService
{
    TableClient GetTable(string tableName);
    TableClient GetTable(TableName table);
    Task<List<T>> GetTableEntries<T>() where T : BaseEntity;
    Task<T?> GetTableEntryIfExists<T>(string partitionKey, string rowKey) where T : BaseEntity;
    Task<bool> EntryExists<T>(string partitionKey, string rowKey) where T : BaseEntity;
}
