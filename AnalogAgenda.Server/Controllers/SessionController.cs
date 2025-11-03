using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Data;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
public class SessionController(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService, AnalogAgendaDbContext dbContext) : BaseEntityController<SessionEntity, SessionDto>(storageCfg, databaseService, blobsService)
{
    private readonly BlobContainerClient sessionsContainer = blobsService.GetBlobContainer(ContainerName.sessions);

    protected override BlobContainerClient GetBlobContainer() => sessionsContainer;
    protected override Guid GetDefaultImageId() => Constants.DefaultSessionImageId;
    protected override SessionEntity DtoToEntity(SessionDto dto) => dto.ToEntity();
    protected override SessionDto EntityToDto(SessionEntity entity) => entity.ToDTO(storageCfg.AccountName);

    [HttpPost]
    public async Task<IActionResult> CreateNewSession([FromBody] SessionDto dto)
    {
        var result = await CreateEntityWithImageAsync(dto, dto => dto.ImageBase64);
        
        // If creation was successful, process the business logic
        if (result is CreatedResult createdResult)
        {
            var createdSession = createdResult.Value as SessionDto;
            if (createdSession != null)
            {
                // Copy the filmToDevKitMapping from the original DTO
                createdSession.FilmToDevKitMapping = dto.FilmToDevKitMapping;
                await ProcessSessionBusinessLogic(createdSession, null);
            }
        }
        
        return result;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSessions([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        return await GetEntitiesWithBackwardCompatibilityAsync(
            page, 
            pageSize,
            entities => entities.ApplyStandardSorting(),
            sorted => sorted.Select(EntityToDto)
        );
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSessionById(string id)
    {
        var sessionEntity = await dbContext.Sessions
            .Include(s => s.DevelopedFilms)
            .ThenInclude(f => f.DevelopedWithDevKit)
            .Include(s => s.UsedDevKits)
            .FirstOrDefaultAsync(s => s.Id == id);
            
        if (sessionEntity == null)
            return NotFound($"No Session found with Id: {id}");

        var sessionDto = EntityToDto(sessionEntity);
        
        // Populate FilmToDevKitMapping based on loaded relationships
        await PopulateFilmToDevKitMapping(sessionDto, sessionEntity);
        
        return Ok(sessionDto);
    }
    
    private async Task PopulateFilmToDevKitMapping(SessionDto sessionDto, SessionEntity? sessionEntity = null)
    {
        sessionDto.FilmToDevKitMapping = new Dictionary<string, List<string>>();
        
        // If we have the entity with loaded relationships, use those
        if (sessionEntity?.DevelopedFilms != null)
        {
            foreach (var film in sessionEntity.DevelopedFilms)
            {
                if (!string.IsNullOrEmpty(film.DevelopedWithDevKitId))
                {
                    if (!sessionDto.FilmToDevKitMapping.ContainsKey(film.DevelopedWithDevKitId))
                    {
                        sessionDto.FilmToDevKitMapping[film.DevelopedWithDevKitId] = new List<string>();
                    }
                    sessionDto.FilmToDevKitMapping[film.DevelopedWithDevKitId].Add(film.Id);
                }
            }
            return;
        }
        
        // Otherwise load from DTO's film list
        var developedFilms = sessionDto.DevelopedFilmsList ?? [];
        if (developedFilms.Count == 0)
            return;
        
        foreach (var filmId in developedFilms)
        {
            var film = await databaseService.GetByIdAsync<FilmEntity>(filmId);
            if (film != null && !string.IsNullOrEmpty(film.DevelopedWithDevKitId))
            {
                if (!sessionDto.FilmToDevKitMapping.ContainsKey(film.DevelopedWithDevKitId))
                {
                    sessionDto.FilmToDevKitMapping[film.DevelopedWithDevKitId] = new List<string>();
                }
                sessionDto.FilmToDevKitMapping[film.DevelopedWithDevKitId].Add(filmId);
            }
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSession(string id, [FromBody] SessionDto updateDto)
    {
        // Get the original session first
        var originalSession = await databaseService.GetByIdAsync<SessionEntity>(id);
        SessionDto? originalDto = originalSession?.ToDTO(storageCfg.AccountName);
        
        var result = await UpdateEntityWithImageAsync(id, updateDto, dto => dto.ImageBase64);
        
        // If update was successful, process the business logic
        if (result is NoContentResult)
        {
            await ProcessSessionBusinessLogic(updateDto, originalDto);
        }
        
        return result;
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSession(string id)
    {
        // Get the session before deletion to revert business logic
        var sessionToDelete = await databaseService.GetByIdAsync<SessionEntity>(id);
        SessionDto? sessionDto = sessionToDelete?.ToDTO(storageCfg.AccountName);
        
        var result = await DeleteEntityWithImageAsync(id);
        
        // If deletion was successful, revert the business logic
        if (result is NoContentResult && sessionDto != null)
        {
            await RevertSessionBusinessLogic(sessionDto);
        }
        
        return result;
    }
    
    /// <summary>
    /// Handles the business logic when a session is created or updated:
    /// - Mark films as developed and set their session/devkit references
    /// - Increment devkit FilmsDeveloped count
    /// </summary>
    private async Task ProcessSessionBusinessLogic(SessionDto newSession, SessionDto? originalSession)
    {
        try
        {
            
            var newDevelopedFilms = newSession.DevelopedFilmsList ?? [];
            var originalDevelopedFilms = originalSession?.DevelopedFilmsList ?? [];
            
            // Films that were added in this update
            var addedFilms = newDevelopedFilms.Except(originalDevelopedFilms).ToList();
            // Films that were removed in this update
            var removedFilms = originalDevelopedFilms.Except(newDevelopedFilms).ToList();
            
            // Process newly added films
            foreach (var filmId in addedFilms)
            {
                var film = await databaseService.GetByIdAsync<FilmEntity>(filmId);
                if (film != null)
                {
                    // Find which devkit this film belongs to
                    string? devKitId = null;
                    foreach (var mapping in newSession.FilmToDevKitMapping)
                    {
                        if (mapping.Value.Contains(filmId))
                        {
                            devKitId = mapping.Key;
                            break;
                        }
                    }
                    
                    // Update film properties
                    film.Developed = true;
                    film.DevelopedInSessionId = newSession.Id;
                    film.DevelopedWithDevKitId = devKitId; // Can be null if film is unassigned
                    film.UpdatedDate = DateTime.UtcNow;
                    await databaseService.UpdateAsync(film);
                    
                    // Increment devkit count if film is assigned to a devkit
                    if (!string.IsNullOrEmpty(devKitId))
                    {
                        var devKit = await databaseService.GetByIdAsync<DevKitEntity>(devKitId);
                        if (devKit != null)
                        {
                            devKit.FilmsDeveloped++;
                            devKit.UpdatedDate = DateTime.UtcNow;
                            await databaseService.UpdateAsync(devKit);
                        }
                    }
                }
            }
            
            // Process removed films (revert their state)
            foreach (var filmId in removedFilms)
            {
                var film = await databaseService.GetByIdAsync<FilmEntity>(filmId);
                if (film != null && film.Developed)
                {
                    // Decrement devkit count if film was assigned to one
                    if (!string.IsNullOrEmpty(film.DevelopedWithDevKitId))
                    {
                        var devKit = await databaseService.GetByIdAsync<DevKitEntity>(film.DevelopedWithDevKitId);
                        if (devKit != null)
                        {
                            devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - 1);
                            devKit.UpdatedDate = DateTime.UtcNow;
                            await databaseService.UpdateAsync(devKit);
                        }
                    }
                    
                    // Clear film references
                    film.Developed = false;
                    film.DevelopedInSessionId = null;
                    film.DevelopedWithDevKitId = null;
                    film.UpdatedDate = DateTime.UtcNow;
                    await databaseService.UpdateAsync(film);
                }
            }
            
            // Handle films that changed devkits (in updates only)
            if (originalSession != null)
            {
                var unchangedFilms = newDevelopedFilms.Intersect(originalDevelopedFilms).ToList();
                foreach (var filmId in unchangedFilms)
                {
                    var film = await databaseService.GetByIdAsync<FilmEntity>(filmId);
                    if (film != null)
                    {
                        // Find new devkit for this film
                        string? newDevKitId = null;
                        foreach (var mapping in newSession.FilmToDevKitMapping)
                        {
                            if (mapping.Value.Contains(filmId))
                            {
                                newDevKitId = mapping.Key;
                                break;
                            }
                        }
                        
                        // Check if devkit changed
                        if (film.DevelopedWithDevKitId != newDevKitId)
                        {
                            // Decrement old devkit count
                            if (!string.IsNullOrEmpty(film.DevelopedWithDevKitId))
                            {
                                var oldDevKit = await databaseService.GetByIdAsync<DevKitEntity>(film.DevelopedWithDevKitId);
                                if (oldDevKit != null)
                                {
                                    oldDevKit.FilmsDeveloped = Math.Max(0, oldDevKit.FilmsDeveloped - 1);
                                    oldDevKit.UpdatedDate = DateTime.UtcNow;
                                    await databaseService.UpdateAsync(oldDevKit);
                                }
                            }
                            
                            // Increment new devkit count
                            if (!string.IsNullOrEmpty(newDevKitId))
                            {
                                var newDevKit = await databaseService.GetByIdAsync<DevKitEntity>(newDevKitId);
                                if (newDevKit != null)
                                {
                                    newDevKit.FilmsDeveloped++;
                                    newDevKit.UpdatedDate = DateTime.UtcNow;
                                    await databaseService.UpdateAsync(newDevKit);
                                }
                            }
                            
                            // Update film's devkit reference
                            film.DevelopedWithDevKitId = newDevKitId;
                            film.UpdatedDate = DateTime.UtcNow;
                            await databaseService.UpdateAsync(film);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            // Log the error but don't fail the main operation
            // In production, you'd want proper logging here
            Console.WriteLine($"Error processing session business logic: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Reverts the business logic when a session is deleted
    /// </summary>
    private async Task RevertSessionBusinessLogic(SessionDto deletedSession)
    {
        try
        {
            var developedFilms = deletedSession.DevelopedFilmsList ?? [];
            
            // Revert each film's state
            foreach (var filmId in developedFilms)
            {
                var film = await databaseService.GetByIdAsync<FilmEntity>(filmId);
                if (film != null && film.Developed)
                {
                    // Decrement devkit count if film was assigned to one
                    if (!string.IsNullOrEmpty(film.DevelopedWithDevKitId))
                    {
                        var devKit = await databaseService.GetByIdAsync<DevKitEntity>(film.DevelopedWithDevKitId);
                        if (devKit != null)
                        {
                            devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - 1);
                            devKit.UpdatedDate = DateTime.UtcNow;
                            await databaseService.UpdateAsync(devKit);
                        }
                    }
                    
                    // Clear film references
                    film.Developed = false;
                    film.DevelopedInSessionId = null;
                    film.DevelopedWithDevKitId = null;
                    film.UpdatedDate = DateTime.UtcNow;
                    await databaseService.UpdateAsync(film);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reverting session business logic: {ex.Message}");
        }
    }
}
