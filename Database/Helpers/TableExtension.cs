namespace Database.Helpers;

public enum Table
{
    Users,
    Notes,
    NotesEntries
}

public static class TableExtension
{
    public static string PartitionKey(this Table table)
    {
        return table switch
        {
            Table.Users => "USER",
            Table.Notes => "NOTE",
            Table.NotesEntries => "NOTEENTRY",
            _ => throw new Exception($"Partition key for {table} does not exist"),
        };
    }

    public static bool IsTable(this string tableName)
    {
        return Enum.TryParse(tableName, out Table _);
    }
}

