using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class DevKitEntity : BaseEntity, IImageEntity
{
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

    // Navigation properties
    public ICollection<SessionEntity> UsedInSessions { get; set; } = new List<SessionEntity>();
    public ICollection<FilmEntity> DevelopedFilms { get; set; } = new List<FilmEntity>();

    protected override int IdLength() => 8;

    public DateTime GetExpirationDate() => MixedOn.AddDays(7 * ValidForWeeks);

    public void Update(DevKitDto dto)
    {
        Name = dto.Name;
        Url = dto.Url;
        Type = dto.Type.ToEnum<EDevKitType>();
        PurchasedBy = dto.PurchasedBy.ToEnum<EUsernameType>();
        PurchasedOn = dto.PurchasedOn.ToDateTime(TimeOnly.MinValue);
        MixedOn = dto.MixedOn.ToDateTime(TimeOnly.MinValue);
        ValidForWeeks = dto.ValidForWeeks;
        ValidForFilms = dto.ValidForFilms;
        FilmsDeveloped = dto.FilmsDeveloped;
        Description = dto.Description;
        Expired = dto.Expired;
        
        // Update ImageId if provided in the URL (extracted from ImageUrl)
        if (!string.IsNullOrEmpty(dto.ImageUrl))
        {
            var extractedImageId = BlobUrlHelper.GetImageInfoFromUrl(dto.ImageUrl).ImageId;
            if (extractedImageId != Guid.Empty)
            {
                ImageId = extractedImageId;
            }
        }
    }

    public DevKitDto ToDTO(string accountName)
    {
        return new DevKitDto()
        {
            Id = Id,
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