namespace AnalogAgenda.Functions.DurableFunction.PhotoUpload;

public class PhotoUploadActivityInput
{
    public required string FilmId { get; set; }
    public required string ImageBase64 { get; set; }
    public required int Index { get; set; }
    public required string StorageAccountName { get; set; }
}

