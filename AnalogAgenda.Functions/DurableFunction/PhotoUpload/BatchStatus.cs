namespace AnalogAgenda.Functions.DurableFunction.PhotoUpload;

public class BatchStatus
{
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public string? FilmId { get; set; }
}

