using Database.DTOs;

namespace AnalogAgenda.Functions.DurableFunction.PhotoUpload;

public class PhotoUploadActivityResult
{
    public bool Success { get; set; }
    public PhotoDto? Photo { get; set; }
    public string? ErrorMessage { get; set; }
}

