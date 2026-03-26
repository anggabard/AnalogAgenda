using AnalogAgenda.Server.Helpers;
using Database.DTOs;
using Database.Entities;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]"), ApiController, Authorize]
public class IdeaController(
    IDatabaseService databaseService,
    DtoConvertor dtoConvertor,
    EntityConvertor entityConvertor
) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly DtoConvertor dtoConvertor = dtoConvertor;
    private readonly EntityConvertor entityConvertor = entityConvertor;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var entities = await databaseService.GetAllIdeasWithSessionLinksAsync();
        var results = entities.Select(dtoConvertor.ToDTO);
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var entity = await databaseService.GetIdeaByIdWithSessionLinksAsync(id);
        if (entity == null)
            return NotFound($"No Idea found with Id: {id}");

        return Ok(dtoConvertor.ToDTO(entity));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] IdeaDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid data.");

        var entity = entityConvertor.ToEntity(dto);
        await databaseService.AddAsync(entity);
        await ReplaceIdeaSessionsAsync(entity.Id, dto.ConnectedSessionIds);
        entity = await databaseService.GetIdeaByIdWithSessionLinksAsync(entity.Id) ?? entity;
        var createdDto = dtoConvertor.ToDTO(entity);
        return Created(string.Empty, createdDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] IdeaDto dto)
    {
        if (dto == null)
            return BadRequest("Invalid data.");

        var existingEntity = await databaseService.GetByIdAsync<IdeaEntity>(id);
        if (existingEntity == null)
            return NotFound($"No Idea found with Id: {id}");

        existingEntity.Title = dto.Title;
        existingEntity.Description = dto.Description ?? string.Empty;
        existingEntity.Outcome = dto.Outcome ?? string.Empty;
        existingEntity.UpdatedDate = DateTime.UtcNow;

        await databaseService.UpdateAsync(existingEntity);
        // Omit ConnectedSessionIds from the JSON body to leave links unchanged (links are edited on the session page).
        if (dto.ConnectedSessionIds != null)
            await ReplaceIdeaSessionsAsync(id, dto.ConnectedSessionIds);
        return NoContent();
    }

    private async Task ReplaceIdeaSessionsAsync(string ideaId, List<string>? connectedSessionIds)
    {
        var distinct = (connectedSessionIds ?? []).Distinct().ToList();
        var validIds = new List<string>();
        foreach (var sessionId in distinct)
        {
            if (await databaseService.ExistsAsync<SessionEntity>(sessionId))
                validIds.Add(sessionId);
        }

        await databaseService.ReplaceEntitiesAsync<IdeaSessionEntity>(
            x => x.IdeaId == ideaId,
            validIds.Select(sid => new IdeaSessionEntity { IdeaId = ideaId, SessionId = sid }));
    }

    [HttpGet("{ideaId}/photos")]
    public async Task<IActionResult> GetIdeaPhotos(string ideaId)
    {
        if (!await databaseService.ExistsAsync<IdeaEntity>(ideaId))
            return NotFound($"No Idea found with Id: {ideaId}");

        var links = await databaseService.GetEntitiesAsync<IdeaPhotoEntity>(x => x.IdeaId == ideaId);
        var photoIds = links.Select(l => l.PhotoId).Distinct().ToList();
        List<PhotoEntity> photos = [];
        if (photoIds.Count > 0)
        {
            photos = await databaseService.GetAllWhereWithIncludesAsync<PhotoEntity>(
                p => photoIds.Contains(p.Id),
                p => p.Film);
            photos = photos.OrderBy(p => p.FilmId).ThenBy(p => p.Index).ToList();
        }

        var visible = photos
            .Where(p => p.Film != null && (!p.Restricted || FilmOwnerHelper.IsCurrentUserFilmOwner(User, p.Film)))
            .Select(dtoConvertor.ToDTO)
            .ToList();

        return Ok(visible);
    }

    [HttpPost("{ideaId}/photos")]
    public async Task<IActionResult> AddPhotosToIdea(string ideaId, [FromBody] IdListDto? body)
    {
        if (body?.Ids == null || body.Ids.Count == 0)
            return BadRequest("Photo ids are required.");

        if (!await databaseService.ExistsAsync<IdeaEntity>(ideaId))
            return NotFound($"No Idea found with Id: {ideaId}");

        foreach (var photoId in body.Ids.Distinct())
        {
            var photo = await databaseService.GetByIdAsync<PhotoEntity>(photoId);
            if (photo == null)
                return BadRequest($"Photo not found: {photoId}");

            var film = await databaseService.GetByIdAsync<FilmEntity>(photo.FilmId);
            if (film == null || !FilmOwnerHelper.IsCurrentUserFilmOwner(User, film))
                return Forbid();
        }

        var distinctIds = body.Ids.Distinct().ToList();
        var existing = await databaseService.GetEntitiesAsync<IdeaPhotoEntity>(
            x => x.IdeaId == ideaId && distinctIds.Contains(x.PhotoId));
        var have = existing.Select(x => x.PhotoId).ToHashSet();
        var toAdd = distinctIds
            .Where(pid => !have.Contains(pid))
            .Select(pid => new IdeaPhotoEntity { IdeaId = ideaId, PhotoId = pid })
            .ToList();
        if (toAdd.Count > 0)
            await databaseService.AddEntitiesAsync(toAdd);

        return await GetIdeaPhotos(ideaId);
    }

    [HttpDelete("{ideaId}/photos/{photoId}")]
    public async Task<IActionResult> RemovePhotoFromIdea(string ideaId, string photoId)
    {
        if (!await databaseService.ExistsAsync<IdeaEntity>(ideaId))
            return NotFound($"No Idea found with Id: {ideaId}");

        var photo = await databaseService.GetByIdAsync<PhotoEntity>(photoId);
        if (photo == null)
            return NotFound("Photo not found.");

        var film = await databaseService.GetByIdAsync<FilmEntity>(photo.FilmId);
        if (film == null || !FilmOwnerHelper.IsCurrentUserFilmOwner(User, film))
            return Forbid();

        await databaseService.DeleteEntitiesAsync<IdeaPhotoEntity>(x => x.IdeaId == ideaId && x.PhotoId == photoId);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var entity = await databaseService.GetByIdAsync<IdeaEntity>(id);
        if (entity == null)
            return NotFound($"No Idea found with Id: {id}");

        await databaseService.DeleteAsync(entity);
        return NoContent();
    }
}
