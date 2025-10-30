using Database.DBObjects.Enums;

namespace Database.Helpers;

public static class TableExtension
{
    public static string PartitionKey(this TableName table)
    {
        return table switch
        {
            TableName.Users => "USER",
            TableName.Notes => "NOTE",
            TableName.NotesEntries => "NOTEENTRY",
            TableName.NotesEntryRules => "NOTERULE",
            TableName.NotesEntryOverrides => "NOTEOVERRIDE",
            TableName.DevKits => "DEVKIT",
            TableName.UsedDevKitThumbnails => "USEDDKT",
            TableName.Films => "FILM",
            TableName.UsedFilmThumbnails => "USEDFT",
            TableName.Photos => "PHOTO",
            TableName.Sessions => "SESSION",
            _ => throw new Exception($"Partition key for {table} does not exist"),
        };
    }

    public static bool IsTable(this string tableName)
    {
        return Enum.TryParse(tableName, out TableName result) && Enum.IsDefined(typeof(TableName), result);
    }
}

