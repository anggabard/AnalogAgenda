using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class FilmEntity : BaseEntity, IImageEntity
{
    public FilmEntity() : base(TableName.Films) { }

    public required string Name { get; set; }

    public required string Iso { get; set; }

    public EFilmType Type { get; set; }

    public int NumberOfExposures { get; set; } = 36;

    public double Cost { get; set; }

    public EUsernameType PurchasedBy { get; set; }

    public DateTime PurchasedOn { get; set; }

    public Guid ImageId { get; set; }

    public string Description { get; set; } = string.Empty;

    public bool Developed { get; set; }

    public string? DevelopedInSessionRowKey { get; set; }

    public string? DevelopedWithDevKitRowKey { get; set; }

    protected override int RowKeyLenght() => 12;

    public FilmDto ToDTO(string accountName)
    {
        return new FilmDto()
        {
            RowKey = RowKey,
            Name = Name,
            Iso = Iso,
            Type = Type.ToDisplayString(),
            NumberOfExposures = NumberOfExposures,
            Cost = Cost,
            PurchasedBy = PurchasedBy.ToString(),
            PurchasedOn = DateOnly.FromDateTime(PurchasedOn),
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.films.ToString(), ImageId),
            Description = Description,
            Developed = Developed,
            DevelopedInSessionRowKey = DevelopedInSessionRowKey,
            DevelopedWithDevKitRowKey = DevelopedWithDevKitRowKey
        };
    }
}
