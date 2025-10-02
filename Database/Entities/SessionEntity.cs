using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Helpers;

namespace Database.Entities;

public class SessionEntity : BaseEntity, IImageEntity
{
    public SessionEntity() : base(TableName.Sessions) { }

    public DateTime SessionDate { get; set; }

    public required string Location { get; set; }

    public required string Participants { get; set; } // JSON array as string

    public Guid ImageId { get; set; }

    public string Description { get; set; } = string.Empty;

    public string UsedSubstances { get; set; } = string.Empty; // JSON array of DevKit RowKeys

    public string DevelopedFilms { get; set; } = string.Empty; // JSON array of Film RowKeys

    protected override int RowKeyLenght() => 10;

    public SessionDto ToDTO(string accountName)
    {
        return new SessionDto()
        {
            RowKey = RowKey,
            SessionDate = DateOnly.FromDateTime(SessionDate),
            Location = Location,
            Participants = Participants,
            ImageUrl = ImageId == Guid.Empty ? string.Empty : BlobUrlHelper.GetUrlFromImageImageInfo(accountName, ContainerName.sessions.ToString(), ImageId),
            Description = Description,
            UsedSubstances = UsedSubstances,
            DevelopedFilms = DevelopedFilms
        };
    }
}
