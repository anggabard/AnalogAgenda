namespace Database.Helpers;

public enum Table
{
    Users
}

public static class TableExtension
{
    public static string PartitionKey(this Table table)
    {
        return table switch
        {
            Table.Users => "USER",
            _ => throw new Exception($"Partition key for {table} does not exist"),
        };
    }

    public static bool IsTable(this string tableName)
    {
        return Enum.TryParse(tableName, out Table _);
    }
}

