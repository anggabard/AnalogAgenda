using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class FilmEntity : BaseEntity, IImageEntity
{
    public string Name { get; set; } = string.Empty;

    public required string Brand { get; set; }

    public required string Iso { get; set; }

    public EFilmType Type { get; set; }

    public int NumberOfExposures { get; set; } = 36;

    public double Cost { get; set; }

    public EUsernameType PurchasedBy { get; set; }

    public DateTime PurchasedOn { get; set; }

    public Guid ImageId { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Developed { get; set; }

    public string? DevelopedInSessionId { get; set; }

    public string? DevelopedWithDevKitId { get; set; }

    // Navigation properties
    public ICollection<PhotoEntity> Photos { get; set; } = [];
    public ICollection<ExposureDateEntity> ExposureDates { get; set; } = [];
    public SessionEntity? DevelopedInSession { get; set; }
    public DevKitEntity? DevelopedWithDevKit { get; set; }

    protected override int IdLength() => 12;

    public void Update(FilmDto dto)
    {
        Name = dto.Name ?? string.Empty;
        Brand = dto.Brand;
        Iso = dto.Iso;
        Type = dto.Type.ToEnum<EFilmType>();
        NumberOfExposures = dto.NumberOfExposures;
        Cost = dto.Cost;
        PurchasedBy = dto.PurchasedBy.ToEnum<EUsernameType>();
        PurchasedOn = dto.PurchasedOn.ToDateTime(TimeOnly.MinValue);
        Description = dto.Description;
        Developed = dto.Developed;
        DevelopedInSessionId = dto.DevelopedInSessionId;
        DevelopedWithDevKitId = dto.DevelopedWithDevKitId;
        
        if (!string.IsNullOrEmpty(dto.ImageUrl))
        {
            var extractedImageId = BlobUrlHelper.GetImageInfoFromUrl(dto.ImageUrl).ImageId;
            if (extractedImageId != Guid.Empty)
            {
                ImageId = extractedImageId;
            }
        }
    }
}
