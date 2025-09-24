namespace Database.DTOs;

public class PhotoBulkUploadDto
{
    public required string FilmRowId { get; set; }
    public required List<PhotoUploadDto> Photos { get; set; }
}

public class PhotoUploadDto
{
    public required string ImageBase64 { get; set; }
}

public class PhotoCreateDto
{
    public required string FilmRowId { get; set; }
    public required string ImageBase64 { get; set; }
}
