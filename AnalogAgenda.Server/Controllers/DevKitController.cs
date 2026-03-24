using Azure.Storage.Blobs;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Text.Json;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]"), ApiController, Authorize]
public class DevKitController(IDatabaseService databaseService, IBlobService blobsService, DtoConvertor dtoConvertor, EntityConvertor entityConvertor) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly DtoConvertor dtoConvertor = dtoConvertor;
    private readonly EntityConvertor entityConvertor = entityConvertor;
    private readonly BlobContainerClient devKitsContainer = blobsService.GetBlobContainer(ContainerName.devkits);

    [HttpPost]
    public async Task<IActionResult> CreateNewKit([FromBody] DevKitDto dto)
    {
        try
        {
            var entity = entityConvertor.ToEntity(dto);
            
            // If no ImageUrl provided, use default image
            if (entity.ImageId == Guid.Empty)
            {
                entity.ImageId = Constants.DefaultDevKitImageId;
            }
            
            await databaseService.AddAsync(entity);
            
            var createdDto = dtoConvertor.ToDTO(entity);
            return Created(string.Empty, createdDto);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllKits([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        if (page <= 0)
        {
            var entities = await databaseService.GetAllAsync<DevKitEntity>();
            var results = entities.ApplyStandardSorting().Select(dtoConvertor.ToDTO);
            return Ok(results);
        }

        var pagedEntities = await databaseService.GetPagedAsync<DevKitEntity>(page, pageSize, entities => entities.ApplyStandardSorting());
        var pagedResults = new PagedResponseDto<DevKitDto>
        {
            Data = pagedEntities.Data.Select(dtoConvertor.ToDTO),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableKits([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        return await GetFilteredKits(k => !k.Expired, page, pageSize);
    }

    [HttpGet("expired")]
    public async Task<IActionResult> GetExpiredKits([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        return await GetFilteredKits(k => k.Expired, page, pageSize);
    }

    private async Task<IActionResult> GetFilteredKits(
        Expression<Func<DevKitEntity, bool>> predicate, 
        int page = 1, 
        int pageSize = 5)
    {
        if (page <= 0)
        {
            var entities = await databaseService.GetAllAsync(predicate);
            var results = entities.ApplyStandardSorting().Select(dtoConvertor.ToDTO);
            return Ok(results);
        }

        var pagedEntities = await databaseService.GetPagedAsync(predicate, page, pageSize, entities => entities.ApplyStandardSorting());
        var pagedResults = new PagedResponseDto<DevKitDto>
        {
            Data = pagedEntities.Data.Select(dtoConvertor.ToDTO),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetKitById(string id)
    {
        var entity = await databaseService.GetByIdAsync<DevKitEntity>(id);
        if (entity == null)
            return NotFound($"No DevKit found with Id: {id}");
        
        return Ok(dtoConvertor.ToDTO(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateKit(string id, [FromBody] DevKitDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        // Load existing entity
        var existingEntity = await databaseService.GetByIdAsync<DevKitEntity>(id);
        
        if (existingEntity == null)
            return NotFound();

        try
        {
            // Update entity using the Update method
            existingEntity.Update(updateDto);
            
            // UpdateAsync will handle UpdatedDate
            await databaseService.UpdateAsync(existingEntity);
            return NoContent();
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKit(string id)
    {
        var entity = await databaseService.GetByIdAsync<DevKitEntity>(id);
        if (entity == null)
            return NotFound();
        
        // Delete image blob if not default
        if (entity.ImageId != Constants.DefaultDevKitImageId)
            await devKitsContainer.DeleteBlobAsync(entity.ImageId.ToString());
        
        await databaseService.DeleteAsync(entity);
        return NoContent();
    }

    [HttpGet("{id}/assignment/sessions")]
    public async Task<IActionResult> GetSessionAssignment(string id, [FromQuery] bool showAll = false)
    {
        if (!await databaseService.ExistsAsync<DevKitEntity>(id))
            return NotFound($"No DevKit found with Id: {id}");

        // Source of truth for assignment UI: DevKitSessions only (not SessionDevKit navigation).
        var selectedIds = (await databaseService.GetEntitiesAsync<DevKitSessionEntity>(x => x.DevKitId == id))
            .Select(x => x.SessionId)
            .ToHashSet();

        var allSessions = (await databaseService.GetAllAsync<SessionEntity>())
            .ApplyStandardSorting()
            .ToList();

        IEnumerable<SessionEntity> rows = showAll
            ? allSessions
            : allSessions.Where(s => selectedIds.Contains(s.Id));

        var result = rows.Select(s => new DevKitSessionAssignmentRowDto
        {
            Id = s.Id,
            SessionDate = DateOnly.FromDateTime(s.SessionDate),
            Location = s.Location,
            ParticipantsPreview = PreviewParticipants(s.Participants),
            IsSelected = selectedIds.Contains(s.Id)
        });

        return Ok(result);
    }

    [HttpPut("{id}/assignment/sessions")]
    public async Task<IActionResult> PutSessionAssignment(string id, [FromBody] IdListDto? body)
    {
        if (body?.Ids == null)
            return BadRequest("Invalid data.");

        if (!await databaseService.ExistsAsync<DevKitEntity>(id))
            return NotFound($"No DevKit found with Id: {id}");

        var devKit = await databaseService.GetByIdAsync<DevKitEntity>(id);
        if (devKit == null)
            return NotFound();

        var newSet = body.Ids.Distinct().ToHashSet();
        var oldLinks = await databaseService.GetEntitiesAsync<DevKitSessionEntity>(x => x.DevKitId == id);
        var oldSet = oldLinks.Select(l => l.SessionId).ToHashSet();
        var affectedSessionIds = oldSet.Union(newSet).ToHashSet();

        await databaseService.ExecuteInTransactionAsync(async () =>
        {
            var sessions = await databaseService.GetAllWhereWithIncludesAsync<SessionEntity>(
                s => affectedSessionIds.Contains(s.Id),
                s => s.UsedDevKits);

            foreach (var session in sessions)
            {
                var sid = session.Id;
                var shouldHave = newSet.Contains(sid);
                var has = session.UsedDevKits.Any(d => d.Id == id);
                if (shouldHave && !has)
                    session.UsedDevKits.Add(devKit);
                else if (!shouldHave && has)
                {
                    var link = session.UsedDevKits.FirstOrDefault(d => d.Id == id);
                    if (link != null)
                        session.UsedDevKits.Remove(link);
                }
            }

            await databaseService.SaveChangesAsync();

            await databaseService.ReplaceEntitiesAsync<DevKitSessionEntity>(
                x => x.DevKitId == id,
                newSet.Select(sid => new DevKitSessionEntity { DevKitId = id, SessionId = sid }));
        });

        return await GetSessionAssignment(id, showAll: true);
    }

    [HttpGet("{id}/assignment/films")]
    public async Task<IActionResult> GetFilmAssignment(string id, [FromQuery] bool showAll = false)
    {
        if (!await databaseService.ExistsAsync<DevKitEntity>(id))
            return NotFound($"No DevKit found with Id: {id}");

        // Source of truth for assignment UI: DevKitFilms only (not Film.DevelopedWithDevKitId).
        var selectedIds = (await databaseService.GetEntitiesAsync<DevKitFilmEntity>(x => x.DevKitId == id))
            .Select(x => x.FilmId)
            .ToHashSet();

        var allDeveloped = (await databaseService.GetAllWithIncludesAsync<FilmEntity>(f => f.ExposureDates))
            .Where(f => f.Developed)
            .ApplyExposureDateSorting()
            .ToList();

        IEnumerable<FilmEntity> rows = showAll
            ? allDeveloped
            : allDeveloped.Where(f => selectedIds.Contains(f.Id));

        var result = rows.Select(f =>
        {
            var filmDto = dtoConvertor.ToDTO(f);
            return new DevKitFilmAssignmentRowDto
            {
                Id = filmDto.Id,
                Name = filmDto.Name,
                Brand = filmDto.Brand,
                Iso = filmDto.Iso,
                Type = filmDto.Type,
                FormattedExposureDate = filmDto.FormattedExposureDate,
                IsSelected = selectedIds.Contains(f.Id)
            };
        });

        return Ok(result);
    }

    [HttpPut("{id}/assignment/films")]
    public async Task<IActionResult> PutFilmAssignment(string id, [FromBody] IdListDto? body)
    {
        if (body?.Ids == null)
            return BadRequest("Invalid data.");

        if (!await databaseService.ExistsAsync<DevKitEntity>(id))
            return NotFound($"No DevKit found with Id: {id}");

        foreach (var filmId in body.Ids.Distinct())
        {
            var film = await databaseService.GetByIdAsync<FilmEntity>(filmId);
            if (film == null)
                return BadRequest($"Film not found: {filmId}");
            if (!film.Developed)
                return BadRequest($"Film {filmId} is not developed.");
        }

        var devKitId = id;
        var newFilmIds = body.Ids.Distinct().ToHashSet();

        await databaseService.ExecuteInTransactionAsync(async () =>
        {
            var oldRows = await databaseService.GetEntitiesAsync<DevKitFilmEntity>(x => x.DevKitId == devKitId);
            var oldFilmIds = oldRows.Select(x => x.FilmId).ToHashSet();
            var filmIdSet = oldFilmIds.Union(newFilmIds).ToHashSet();
            var films = await databaseService.GetAllAsync<FilmEntity>(f => filmIdSet.Contains(f.Id));

            foreach (var f in films)
            {
                if (newFilmIds.Contains(f.Id))
                    f.DevelopedWithDevKitId = devKitId;
                else if (oldFilmIds.Contains(f.Id) && f.DevelopedWithDevKitId == devKitId)
                    f.DevelopedWithDevKitId = null;
            }

            await databaseService.SaveChangesAsync();

            await databaseService.ReplaceEntitiesAsync<DevKitFilmEntity>(
                x => x.DevKitId == devKitId,
                newFilmIds.Select(fid => new DevKitFilmEntity { DevKitId = devKitId, FilmId = fid }));
        });

        return await GetFilmAssignment(id, showAll: true);
    }

    private static string PreviewParticipants(string participantsJson)
    {
        if (string.IsNullOrWhiteSpace(participantsJson))
            return string.Empty;
        try
        {
            var list = JsonSerializer.Deserialize<List<string>>(participantsJson);
            if (list == null || list.Count == 0)
                return string.Empty;
            return string.Join(", ", list.Take(4));
        }
        catch
        {
            return participantsJson.Length > 80 ? participantsJson[..80] + "…" : participantsJson;
        }
    }

}