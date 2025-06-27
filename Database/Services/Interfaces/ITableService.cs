using Azure.Data.Tables;
using Database.Helpers;

namespace Database.Services;

public interface ITableService
{
    TableClient GetTable(string tableName);
    TableClient GetTable(Table table);
}
