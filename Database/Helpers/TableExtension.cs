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
            TableName.DevKits => "DEVKIT",
            TableName.Films => "FILM",
            TableName.Photos => "PHOTO",
            TableName.Sessions => "SESSION",
            TableName.UsedFilmThumbnails => "USEDFT",
            TableName.UsedDevKitThumbnails => "USEDDKT",
            _ => throw new Exception($"Partition key for {table} does not exist"),
        };
    }

    public static bool IsTable(this string tableName)
    {
        return Enum.TryParse(tableName, out TableName result) && Enum.IsDefined(typeof(TableName), result);
    }
}

