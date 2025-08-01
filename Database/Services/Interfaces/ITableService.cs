using Azure.Data.Tables;
using Database.DBObjects.Enums;

namespace Database.Services.Interfaces;

public interface ITableService
{
    TableClient GetTable(string tableName);
    TableClient GetTable(TableName table);
}
