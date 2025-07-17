using Azure;
using Azure.Data.Tables;
using Database.Helpers;

namespace Database.Entities;

public class NoteEntity : ITableEntity
{
    public string PartitionKey { get; set; } = Table.Notes.PartitionKey();
    public string RowKey { get; set; } = default!;
    public required string Name { get; set; }
    public DateTime CreatedDate { get; set; }

    // housekeeping
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
