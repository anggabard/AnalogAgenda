namespace Database.DTOs;

public class PhotoBatchUploadDto
{
    public required string Key { get; set; }
    public required string KeyId { get; set; }
    public required string FilmId { get; set; }
    public required PhotoCreateDto[] Photos { get; set; }
}

