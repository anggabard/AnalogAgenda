using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;

namespace Database.Services;

public class EntityConvertor()
{
    public PhotoEntity ToEntity(PhotoDto dto) => new()
    {
        Id = dto.Id,
        FilmId = dto.FilmId,
        Index = dto.Index,
        ImageId = Guid.Empty // ImageId should be set by the controller, not derived from URL
    };

    public FilmEntity ToEntity(FilmDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Iso = dto.Iso,
        Type = dto.Type.ToEnum<EFilmType>(),
        NumberOfExposures = dto.NumberOfExposures,
        Cost = dto.Cost,
        PurchasedBy = dto.PurchasedBy.ToEnum<EUsernameType>(),
        PurchasedOn = new DateTime(dto.PurchasedOn, TimeOnly.MinValue, DateTimeKind.Utc),
        ImageId = string.IsNullOrEmpty(dto.ImageUrl) ? Guid.Empty : BlobUrlHelper.GetImageInfoFromUrl(dto.ImageUrl).ImageId,
        Description = dto.Description,
        Developed = dto.Developed,
        DevelopedInSessionId = dto.DevelopedInSessionId,
        DevelopedWithDevKitId = dto.DevelopedWithDevKitId
    };

    public DevKitEntity ToEntity(DevKitDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        Url = dto.Url,
        Type = dto.Type.ToEnum<EDevKitType>(),
        PurchasedBy = dto.PurchasedBy.ToEnum<EUsernameType>(),
        PurchasedOn = new DateTime(dto.PurchasedOn, TimeOnly.MinValue, DateTimeKind.Utc),
        MixedOn = dto.MixedOn.HasValue ? new DateTime(dto.MixedOn.Value, TimeOnly.MinValue, DateTimeKind.Utc) : null,
        ValidForWeeks = dto.ValidForWeeks,
        ValidForFilms = dto.ValidForFilms,
        FilmsDeveloped = dto.FilmsDeveloped,
        ImageId = string.IsNullOrEmpty(dto.ImageUrl) ? Guid.Empty : BlobUrlHelper.GetImageInfoFromUrl(dto.ImageUrl).ImageId,
        Description = dto.Description,
        Expired = dto.Expired
    };

    public SessionEntity ToEntity(SessionDto dto) => new()
    {
        Id = dto.Id,
        SessionDate = dto.SessionDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
        Location = dto.Location,
        Participants = dto.Participants,
        ImageId = string.IsNullOrEmpty(dto.ImageUrl) ? Guid.Empty : BlobUrlHelper.GetImageInfoFromUrl(dto.ImageUrl).ImageId,
        Description = dto.Description
    };

    public NoteEntity ToNoteEntity(NoteDto dto) => new()
    {
        Id = dto.Id,
        Name = dto.Name,
        SideNote = dto.SideNote,
        ImageId = string.IsNullOrEmpty(dto.ImageUrl) ? Guid.Empty : BlobUrlHelper.GetImageInfoFromUrl(dto.ImageUrl).ImageId
    };

    public List<NoteEntryEntity> ToNoteEntryEntities(NoteDto dto, string noteId) => 
        dto.Entries.Select(entry => ToEntity(entry, noteId)).ToList();

    public UsedFilmThumbnailEntity ToEntity(UsedFilmThumbnailDto dto) => new()
    {
        Id = dto.Id,
        FilmName = dto.FilmName,
        ImageId = string.IsNullOrEmpty(dto.ImageId) ? Guid.Empty : Guid.Parse(dto.ImageId)
    };

    public UsedDevKitThumbnailEntity ToEntity(UsedDevKitThumbnailDto dto) => new()
    {
        Id = dto.Id,
        DevKitName = dto.DevKitName,
        ImageId = string.IsNullOrEmpty(dto.ImageId) ? Guid.Empty : Guid.Parse(dto.ImageId)
    };

    public NoteEntryEntity ToEntity(NoteEntryDto dto) => new()
    {
        Id = dto.Id,
        NoteId = dto.NoteId,
        Time = dto.Time,
        Step = dto.Step,
        Details = dto.Details,
        Index = dto.Index,
        TemperatureMin = dto.TemperatureMin,
        TemperatureMax = dto.TemperatureMax
    };

    public NoteEntryEntity ToEntity(NoteEntryDto dto, string noteId) => new()
    {
        Id = dto.Id,
        NoteId = noteId,
        Time = dto.Time,
        Step = dto.Step,
        Details = dto.Details,
        Index = dto.Index,
        TemperatureMin = dto.TemperatureMin,
        TemperatureMax = dto.TemperatureMax
    };

    public NoteEntryRuleEntity ToEntity(NoteEntryRuleDto dto) => new()
    {
        Id = dto.Id,
        NoteEntryId = dto.NoteEntryId,
        FilmInterval = dto.FilmInterval,
        TimeIncrement = dto.TimeIncrement
    };

    public NoteEntryRuleEntity ToEntity(NoteEntryRuleDto dto, string noteEntryId) => new()
    {
        Id = dto.Id,
        NoteEntryId = noteEntryId,
        FilmInterval = dto.FilmInterval,
        TimeIncrement = dto.TimeIncrement
    };

    public NoteEntryOverrideEntity ToEntity(NoteEntryOverrideDto dto) => new()
    {
        Id = dto.Id,
        NoteEntryId = dto.NoteEntryId,
        FilmCountMin = dto.FilmCountMin,
        FilmCountMax = dto.FilmCountMax,
        Time = dto.Time,
        Step = dto.Step,
        Details = dto.Details,
        TemperatureMin = dto.TemperatureMin,
        TemperatureMax = dto.TemperatureMax
    };

    public NoteEntryOverrideEntity ToEntity(NoteEntryOverrideDto dto, string noteEntryId) => new()
    {
        Id = dto.Id,
        NoteEntryId = noteEntryId,
        FilmCountMin = dto.FilmCountMin,
        FilmCountMax = dto.FilmCountMax,
        Time = dto.Time,
        Step = dto.Step,
        Details = dto.Details,
        TemperatureMin = dto.TemperatureMin,
        TemperatureMax = dto.TemperatureMax
    };

    public UserSettingsEntity ToEntity(UserSettingsDto dto) => new()
    {
        Id = string.IsNullOrEmpty(dto.UserId) ? string.Empty : dto.UserId, // Will be generated if empty
        UserId = dto.UserId,
        IsSubscribed = dto.IsSubscribed,
        CurrentFilmId = dto.CurrentFilmId,
        TableView = dto.TableView,
        EntitiesPerPage = dto.EntitiesPerPage
    };

    public IdeaEntity ToEntity(IdeaDto dto) => new()
    {
        Id = string.IsNullOrEmpty(dto.Id) ? string.Empty : dto.Id,
        Title = dto.Title,
        Description = dto.Description ?? string.Empty
    };
}

