namespace AnalogAgenda.Functions.DurableFunction.PhotoUpload;

public class RecordPhotoProcessedInput
{
    public bool Success { get; set; }
    public required string FilmId { get; set; }
}

