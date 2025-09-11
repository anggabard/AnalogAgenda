using Azure;
using Azure.Data.Tables;
using Database.DBObjects.Enums;
using Database.Helpers;

namespace Database.Entities;

public abstract class BaseEntity(TableName table) : ITableEntity
{
    protected TableName Table { get; } = table;
    public TableName GetTable() => Table;


    public string PartitionKey { get => Table.PartitionKey(); set => Table.PartitionKey(); }
    public string RowKey { get => GetId(); set => GetId(); }
    protected abstract int RowKeyLenght();

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;


    // housekeeping
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    protected virtual string GetId()
    {
        return IdGenerator.Get(RowKeyLenght(), PartitionKey, CreatedDate.Ticks.ToString());
    }

}
