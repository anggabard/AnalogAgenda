using Database.DBObjects.Enums;
using Database.Entities;
using Database.Helpers;
using Database.DTOs.Subclasses;
using System.Text.Json;

namespace Database.DTOs;

public class FilmDto
{
    public string Id { get; set; } = string.Empty;

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

    public string? DevelopedInSessionId { get; set; }

    public string? DevelopedWithDevKitId { get; set; }

    public string ExposureDates { get; set; } = string.Empty;

    public List<ExposureDateEntry> ExposureDatesList
    {
        get => string.IsNullOrEmpty(ExposureDates) ? [] : JsonSerializer.Deserialize<List<ExposureDateEntry>>(ExposureDates) ?? [];
        set => ExposureDates = value == null || value.Count == 0 ? string.Empty : JsonSerializer.Serialize(value);
    }

    public FilmEntity ToEntity()
    {
        return new FilmEntity
        {
            Id = Id,
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
            DevelopedInSessionId = DevelopedInSessionId,
            DevelopedWithDevKitId = DevelopedWithDevKitId,
            ExposureDates = ExposureDates
        };
    }
}
