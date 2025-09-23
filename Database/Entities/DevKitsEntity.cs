using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class DevKitEntity : BaseEntity, IImageEntity
{
    public DevKitEntity() : base(TableName.DevKits) { }

    public required string Name { get; set; }

    public required string Url { get; set; }

    public EDevKitType Type { get; set; }

    public EUsernameType PurchasedBy { get; set; }

    public DateTime PurchasedOn { get; set; }

    public DateTime MixedOn { get; set; }

    public int ValidForWeeks { get; set; }

    public int ValidForFilms { get; set; }

    public int FilmsDeveloped { get; set; }

    public Guid ImageId { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Expired { get; set; }

    protected override int RowKeyLenght() => 8;

    public DateTime GetExpirationDate() => MixedOn.AddDays(7 * ValidForWeeks);

    public DevKitDto ToDTO(string accountName)
    {
        return new DevKitDto()
        {
            RowKey = RowKey,
            Name = Name,
            Url = Url,
            Type = Type.ToString(),
            PurchasedBy = PurchasedBy.ToString(),
            PurchasedOn = DateOnly.FromDateTime(PurchasedOn),
            MixedOn = DateOnly.FromDateTime(MixedOn),
            ValidForWeeks = ValidForWeeks,
            ValidForFilms = ValidForFilms,
            FilmsDeveloped = FilmsDeveloped,
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.devkits.ToString(), ImageId),
            Description = Description,
            Expired = Expired
        };
    }
}