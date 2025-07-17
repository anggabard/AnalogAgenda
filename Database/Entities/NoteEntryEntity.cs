using Azure;
using Azure.Data.Tables;
using Database.Enums;
using Database.Helpers;

namespace Database.Entities;

public class NoteEntryEntity : ITableEntity
{
    public string PartitionKey { get; set; } = Table.NotesEntries.PartitionKey();
    public string RowKey { get; set; } = default!;
    public string NoteRowKey { get; set; } = default!;
    public required TimeSpan Time { get; set; }
    public required ENoteEntryType ProcessType { get; set; }
    public required string Film { get; set; }
    public string? Details { get; set; }

    // housekeeping
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
