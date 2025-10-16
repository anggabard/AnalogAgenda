using Database.DBObjects.Enums;
using Database.Entities;
using Database.Helpers;

namespace Database.DTOs;

public class FilmDto
{
    public string RowKey { get; set; } = string.Empty;

    public required string Name { get; set; }

    public required string Iso { get; set; }

    public required string Type { get; set; }

    public int NumberOfExposures { get; set; } = 36;

    public double Cost { get; set; }

    public required string PurchasedBy { get; set; }

    public DateOnly PurchasedOn { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Developed { get; set; }

    public string? DevelopedInSessionRowKey { get; set; }

    public string? DevelopedWithDevKitRowKey { get; set; }

    public FilmEntity ToEntity()
    {
        return new FilmEntity
        {
            RowKey = RowKey,
            Name = Name,
            Iso = Iso,
            Type = Type.ToEnum<EFilmType>(),
            NumberOfExposures = NumberOfExposures,
            Cost = Cost,
            PurchasedBy = PurchasedBy.ToEnum<EUsernameType>(),
            PurchasedOn = new DateTime(PurchasedOn, TimeOnly.MinValue, DateTimeKind.Utc),
            ImageId = string.IsNullOrEmpty(ImageUrl) ? Guid.Empty : BlobUrlHelper.GetImageInfoFromUrl(ImageUrl).ImageId,
            Description = Description,
            Developed = Developed,
            DevelopedInSessionRowKey = DevelopedInSessionRowKey,
            DevelopedWithDevKitRowKey = DevelopedWithDevKitRowKey
        };
    }
}
