using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Microsoft.Extensions.Configuration;

namespace Database.Services;

public class DtoConvertor(Configuration.Sections.System systemCfg, Storage storageCfg, IConfiguration configuration)
{
    private readonly Configuration.Sections.System systemCfg = systemCfg;
    private readonly Storage storageCfg = storageCfg;
    private readonly IConfiguration configuration = configuration;

    private string BuildImageUrl(ContainerName containerName, Guid imageId)
    {
        if (imageId == Guid.Empty)
            return string.Empty;

        var containerNameString = containerName.ToString();

        if (systemCfg.IsDev)
        {
            // Development: Use BlobEndpoint from Storage config (already populated from appsettings)
            return $"{storageCfg.BlobEndpoint}/{containerNameString}/{imageId}";
        }

        // Production: Use Azure Storage format
        return $"https://{storageCfg.AccountName}.blob.core.windows.net/{containerNameString}/{imageId}";
    }

    public PhotoDto ToDTO(PhotoEntity entity) => new PhotoDto()
    {
        Id = entity.Id,
        FilmId = entity.FilmId,
        Index = entity.Index,
        ImageUrl = BuildImageUrl(ContainerName.photos, entity.ImageId)
    };

    public FilmDto ToDTO(FilmEntity entity)
    {
        // Get dates from ExposureDates navigation property (if loaded)
        var exposureDates = entity.ExposureDates?.Select(e => e.Date).ToList() ?? [];
        var fallbackDate = DateOnly.FromDateTime(entity.PurchasedOn);
        var formattedDate = DateFormattingHelper.FormatExposureDateRange(exposureDates, fallbackDate);

        return new FilmDto()
        {
            Id = entity.Id,
            Name = entity.Name,
            Iso = entity.Iso,
            Type = entity.Type.ToDisplayString(),
            NumberOfExposures = entity.NumberOfExposures,
            Cost = entity.Cost,
            PurchasedBy = entity.PurchasedBy.ToString(),
            PurchasedOn = DateOnly.FromDateTime(entity.PurchasedOn),
            ImageUrl = BuildImageUrl(ContainerName.films, entity.ImageId),
            Description = entity.Description,
            Developed = entity.Developed,
            DevelopedInSessionId = entity.DevelopedInSessionId,
            DevelopedWithDevKitId = entity.DevelopedWithDevKitId,
            FormattedExposureDate = formattedDate,
            PhotoCount = entity.Photos?.Count ?? 0
        };
    }

    public DevKitDto ToDTO(DevKitEntity entity) => new DevKitDto()
    {
        Id = entity.Id,
        Name = entity.Name,
        Url = entity.Url,
        Type = entity.Type.ToString(),
        PurchasedBy = entity.PurchasedBy.ToString(),
        PurchasedOn = DateOnly.FromDateTime(entity.PurchasedOn),
        MixedOn = DateOnly.FromDateTime(entity.MixedOn),
        ValidForWeeks = entity.ValidForWeeks,
        ValidForFilms = entity.ValidForFilms,
        FilmsDeveloped = entity.FilmsDeveloped,
        ImageUrl = BuildImageUrl(ContainerName.devkits, entity.ImageId),
        Description = entity.Description,
        Expired = entity.Expired
    };

    public SessionDto ToDTO(SessionEntity entity) => new SessionDto()
    {
        Id = entity.Id,
        SessionDate = DateOnly.FromDateTime(entity.SessionDate),
        Location = entity.Location,
        Participants = entity.Participants,
        ImageUrl = BuildImageUrl(ContainerName.sessions, entity.ImageId),
        Description = entity.Description,
        UsedSubstances = string.Join(",", entity.UsedDevKits.Select(d => d.Id)),
        DevelopedFilms = string.Join(",", entity.DevelopedFilms.Select(f => f.Id))
    };

    public NoteDto ToDTO(NoteEntity entity) => new NoteDto()
    {
        Id = entity.Id,
        Name = entity.Name,
        SideNote = entity.SideNote,
        ImageUrl = BuildImageUrl(ContainerName.notes, entity.ImageId)
    };

    public NoteDto ToDTO(NoteEntity entity, List<NoteEntryEntity> noteEntries) => new NoteDto()
    {
        Id = entity.Id,
        Name = entity.Name,
        SideNote = entity.SideNote,
        ImageUrl = BuildImageUrl(ContainerName.notes, entity.ImageId),
        Entries = noteEntries.Select(ToDTO).ToList()
    };

    public NoteDto ToDTO(NoteEntity entity, List<NoteEntryDto> noteEntries) => new NoteDto()
    {
        Id = entity.Id,
        Name = entity.Name,
        SideNote = entity.SideNote,
        ImageUrl = BuildImageUrl(ContainerName.notes, entity.ImageId),
        Entries = noteEntries
    };

    public UsedFilmThumbnailDto ToDTO(UsedFilmThumbnailEntity entity) => new UsedFilmThumbnailDto()
    {
        Id = entity.Id,
        FilmName = entity.FilmName,
        ImageId = entity.ImageId.ToString(),
        ImageUrl = BuildImageUrl(ContainerName.films, entity.ImageId)
    };

    public UsedDevKitThumbnailDto ToDTO(UsedDevKitThumbnailEntity entity) => new UsedDevKitThumbnailDto()
    {
        Id = entity.Id,
        DevKitName = entity.DevKitName,
        ImageId = entity.ImageId.ToString(),
        ImageUrl = BuildImageUrl(ContainerName.devkits, entity.ImageId)
    };

    public NoteEntryDto ToDTO(NoteEntryEntity entity) => new NoteEntryDto
    {
        Id = entity.Id,
        NoteId = entity.NoteId,
        Time = entity.Time,
        Step = entity.Step,
        Details = entity.Details,
        Index = entity.Index,
        TemperatureMin = entity.TemperatureMin,
        TemperatureMax = entity.TemperatureMax,
        Rules = entity.Rules.Select(ToDTO).ToList(),
        Overrides = entity.Overrides.Select(ToDTO).ToList()
    };

    public NoteEntryRuleDto ToDTO(NoteEntryRuleEntity entity) => new NoteEntryRuleDto
    {
        Id = entity.Id,
        NoteEntryId = entity.NoteEntryId,
        FilmInterval = entity.FilmInterval,
        TimeIncrement = entity.TimeIncrement
    };

    public NoteEntryOverrideDto ToDTO(NoteEntryOverrideEntity entity) => new NoteEntryOverrideDto
    {
        Id = entity.Id,
        NoteEntryId = entity.NoteEntryId,
        FilmCountMin = entity.FilmCountMin,
        FilmCountMax = entity.FilmCountMax,
        Time = entity.Time,
        Step = entity.Step,
        Details = entity.Details,
        TemperatureMin = entity.TemperatureMin,
        TemperatureMax = entity.TemperatureMax
    };
}

