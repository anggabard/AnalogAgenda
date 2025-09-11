using Database.DBObjects.Enums;
using Database.Entities;
using Database.Helpers;

namespace Database.DTOs;

public class DevKitDto
{
    public string RowKey { get; set; } = string.Empty;

    public required string Name { get; set; }

    public required string Url { get; set; }

    public required string Type { get; set; }

    public required string PurchasedBy { get; set; }

    public DateOnly PurchasedOn { get; set; }

    public DateOnly MixedOn { get; set; }

    public ushort ValidForWeeks { get; set; }

    public ushort ValidForFilms { get; set; }

    public ushort FilmsDeveloped { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public string ImageBase64 { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool Expired { get; set; }

    public DevKitEntity ToEntity()
    {
        return new DevKitEntity
        {
            RowKey = RowKey,
            Name = Name,
            Url = Url,
            Type = Type.ToEnum<EDevKitType>(),
            PurchasedBy = PurchasedBy.ToEnum<EUsernameType>(),
            PurchasedOn = new DateTime(PurchasedOn, TimeOnly.MinValue, DateTimeKind.Utc),
            MixedOn = new DateTime(MixedOn, TimeOnly.MinValue, DateTimeKind.Utc),
            ValidForWeeks = ValidForWeeks,
            ValidForFilms = ValidForFilms,
            FilmsDeveloped = FilmsDeveloped,
            ImageId = string.IsNullOrEmpty(ImageUrl) ? Guid.Empty : BlobUrlHelper.GetImageInfoFromUrl(ImageUrl).ImageId,
            Description = Description,
            Expired = Expired
        };
    }
}
