using Database.DTOs.Subclasses;

namespace Database.DTOs;

public class FilmSearchDto : PaginationParams
{
    public string? Name { get; set; }
    public string? Brand { get; set; }
    public string? Id { get; set; }
    public string? Iso { get; set; }
    public string? Type { get; set; }
    public string? Description { get; set; }
    public DateOnly? ExposureDateFrom { get; set; }
    public DateOnly? ExposureDateTo { get; set; }
    public string? PurchasedBy { get; set; }
    public string? DevelopedWithDevKitId { get; set; }
    public string? DevelopedInSessionId { get; set; }
}
