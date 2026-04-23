using System.Security.Cryptography;

namespace Database.DBObjects;

public static class Constants
{
    public static readonly Guid DefaultDevKitImageId = Guid.Parse("760365AB-EEFF-455B-92A8-F24442617DDB");
    public static readonly Guid DefaultNoteImageId = Guid.Parse("93AB3C7E-82C3-426C-A13F-21CC6F419C71");
    public static readonly Guid DefaultFilmImageId = Guid.Parse("B4E1F2A3-7D6C-4B9A-8E3F-1C5D7A9B2E4F");
    public static readonly Guid DefaultSessionImageId = Guid.Parse("A7F7ACAB-6339-4B6C-A590-FC744D4C8BD9");
    public static readonly Guid DefaultCollectionImageId = Guid.Parse("E2B8C4F1-9A3D-4E7B-8F6C-1D5A0B9E2C7F");

    public static readonly DateTime AnalogAgendaGenesis = new(2025, 6, 27, 14, 57, 18, 226, 758, DateTimeKind.Utc);

    public static readonly HashAlgorithmName HashAlgorithmName = HashAlgorithmName.SHA256;

    public const int HashAlgorithmInputLength = 32;
}
