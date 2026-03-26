using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;

namespace Database.Services;

public class DtoConvertor(Configuration.Sections.System systemCfg, Storage storageCfg)
{
    private readonly Configuration.Sections.System systemCfg = systemCfg;
    private readonly Storage storageCfg = storageCfg;

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

    public PhotoDto ToDTO(PhotoEntity entity) => new()
    {
        Id = entity.Id,
        FilmId = entity.FilmId,
        Index = entity.Index,
        ImageUrl = BuildImageUrl(ContainerName.photos, entity.ImageId),
        Restricted = entity.Restricted
    };

    public FilmDto ToDTO(FilmEntity entity)
    {
        // Get dates from ExposureDates navigation property (if loaded). No fallback: empty when no exposure dates.
        var exposureDates = entity.ExposureDates?.Select(e => e.Date).ToList() ?? [];
        var formattedDate = exposureDates.Count > 0
            ? DateFormattingHelper.FormatExposureDateRange(exposureDates, null)
            : string.Empty;

        return new FilmDto()
        {
            Id = entity.Id,
            Name = entity.Name ?? string.Empty,
            Brand = entity.Brand,
            Iso = entity.Iso,
            Type = entity.Type.ToDisplayString(),
            NumberOfExposures = entity.NumberOfExposures,
            Cost = entity.Cost,
            CostCurrency = entity.CostCurrency.ToString(),
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

    public DevKitDto ToDTO(DevKitEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Url = entity.Url,
        Type = entity.Type.ToString(),
        PurchasedBy = entity.PurchasedBy.ToString(),
        PurchasedOn = DateOnly.FromDateTime(entity.PurchasedOn),
        MixedOn = entity.MixedOn.HasValue ? DateOnly.FromDateTime(entity.MixedOn.Value) : null,
        ValidForWeeks = entity.ValidForWeeks,
        ValidForFilms = entity.ValidForFilms,
        FilmsDeveloped = entity.FilmsDeveloped,
        ImageUrl = BuildImageUrl(ContainerName.devkits, entity.ImageId),
        Description = entity.Description,
        Expired = entity.Expired
    };

    public SessionDto ToDTO(SessionEntity entity)
    {
        var dto = new SessionDto
        {
            Id = entity.Id,
            Index = entity.Index,
            Name = entity.Name,
            SessionDate = DateOnly.FromDateTime(entity.SessionDate),
            Location = entity.Location,
            Participants = entity.Participants,
            ImageUrl = BuildImageUrl(ContainerName.sessions, entity.ImageId),
            Description = entity.Description,
            UsedSubstances = string.Join(",", entity.UsedDevKits.Select(d => d.Id)),
            DevelopedFilms = string.Join(",", entity.DevelopedFilms.Select(f => f.Id))
        };

        if (entity.IdeaSessions is { Count: > 0 })
        {
            var summaries = entity.IdeaSessions
                .Where(x => x.Idea != null)
                .OrderBy(x => x.Idea!.Title)
                .Select(x => new SessionLinkedIdeaSummaryDto { Id = x.IdeaId, Title = x.Idea!.Title })
                .ToList();
            dto.ConnectedIdeas = summaries;
            dto.ConnectedIdeaIds = summaries.Select(x => x.Id).ToList();
        }

        return dto;
    }

    public NoteDto ToDTO(NoteEntity entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        SideNote = entity.SideNote,
        ImageUrl = BuildImageUrl(ContainerName.notes, entity.ImageId)
    };

    public NoteDto ToDTO(NoteEntity entity, List<NoteEntryEntity> noteEntries) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        SideNote = entity.SideNote,
        ImageUrl = BuildImageUrl(ContainerName.notes, entity.ImageId),
        Entries = noteEntries.Select(ToDTO).ToList()
    };

    public NoteDto ToDTO(NoteEntity entity, List<NoteEntryDto> noteEntries) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        SideNote = entity.SideNote,
        ImageUrl = BuildImageUrl(ContainerName.notes, entity.ImageId),
        Entries = noteEntries
    };

    public UsedFilmThumbnailDto ToDTO(UsedFilmThumbnailEntity entity) => new()
    {
        Id = entity.Id,
        FilmName = entity.FilmName,
        ImageId = entity.ImageId.ToString(),
        ImageUrl = BuildImageUrl(ContainerName.films, entity.ImageId)
    };

    public UsedDevKitThumbnailDto ToDTO(UsedDevKitThumbnailEntity entity) => new()
    {
        Id = entity.Id,
        DevKitName = entity.DevKitName,
        ImageId = entity.ImageId.ToString(),
        ImageUrl = BuildImageUrl(ContainerName.devkits, entity.ImageId)
    };

    public NoteEntryDto ToDTO(NoteEntryEntity entity) => new()
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

    public NoteEntryRuleDto ToDTO(NoteEntryRuleEntity entity) => new()
    {
        Id = entity.Id,
        NoteEntryId = entity.NoteEntryId,
        FilmInterval = entity.FilmInterval,
        TimeIncrement = entity.TimeIncrement
    };

    public NoteEntryOverrideDto ToDTO(NoteEntryOverrideEntity entity) => new()
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

    public UserSettingsDto ToDTO(UserSettingsEntity entity) => new()
    {
        UserId = entity.UserId,
        IsSubscribed = entity.IsSubscribed,
        CurrentFilmId = entity.CurrentFilmId,
        TableView = entity.TableView,
        EntitiesPerPage = entity.EntitiesPerPage
    };

    public IdeaDto ToDTO(IdeaEntity entity)
    {
        var linked = BuildIdeaConnectedSessions(entity);
        return new()
        {
            Id = entity.Id,
            Title = entity.Title,
            Description = entity.Description,
            Outcome = entity.Outcome,
            ConnectedSessionIds = linked.Select(x => x.Id).ToList(),
            ConnectedSessions = linked
        };
    }

    private static List<IdeaSessionSummaryDto> BuildIdeaConnectedSessions(IdeaEntity entity)
    {
        if (entity.IdeaSessions == null || entity.IdeaSessions.Count == 0)
        {
            return [];
        }

        return entity.IdeaSessions
            .Where(x => x.Session != null)
            .OrderBy(x => x.Session!.Index < 1 ? int.MaxValue : x.Session.Index)
            .ThenBy(x => x.SessionId)
            .Select(x => new IdeaSessionSummaryDto
            {
                Id = x.SessionId,
                DisplayLabel = string.IsNullOrWhiteSpace(x.Session!.Name)
                    ? (x.Session.Index >= 1 ? $"Session {x.Session.Index}" : string.Empty)
                    : x.Session.Name.Trim()
            })
            .ToList();
    }
}

