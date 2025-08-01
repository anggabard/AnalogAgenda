using Azure;
using Azure.Data.Tables;
using Database.DBObjects.Enums;
using Database.Helpers;

namespace Database.Entities;

public abstract class BaseEntity(TableName table) : ITableEntity
{
    protected TableName Table { get; } = table;

    public string PartitionKey { get => Table.PartitionKey(); set => Table.PartitionKey(); }
    public string RowKey { get => GetId(); set => GetId(); } 

    // housekeeping
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    protected abstract string GetId(params string[] inputs);
}
