using Azure;
using Azure.Data.Tables;
using Database.Helpers;

namespace Database.Entities;

public class UserEntity : ITableEntity
{
    public string PartitionKey { get; set; } = Table.Users.PartitionKey();
    public string RowKey { get; set; } = default!;   // we store the e-mail here
    public string Username { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;

    // housekeeping
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}