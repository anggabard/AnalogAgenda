namespace AnalogAgenda.Functions.DurableFunction.PhotoUpload;

public class PhotoUploadBatchEntity
{
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string? FilmId { get; set; }

    public void RecordPhotoProcessed(RecordPhotoProcessedInput input)
    {
        ProcessedCount++;
        if (input.Success)
        {
            SuccessCount++;
        }
        else
        {
            FailureCount++;
        }
        FilmId = input.FilmId;
    }

    public BatchStatus GetStatus()
    {
        return new BatchStatus
        {
            ProcessedCount = ProcessedCount,
            SuccessCount = SuccessCount,
            FailureCount = FailureCount,
            FilmId = FilmId
        };
    }
}

