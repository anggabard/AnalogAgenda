using Azure.Data.Tables;
using Database.DBObjects.Enums;
using Database.Entities;

namespace Database.Services.Interfaces;

public interface ITableService
{
    TableClient GetTable(string tableName);
    TableClient GetTable(TableName table);
    Task<List<T>> GetTableEntries<T>() where T : BaseEntity;
}
