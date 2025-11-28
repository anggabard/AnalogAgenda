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

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]"), ApiController, Authorize]
public class SessionController(IDatabaseService databaseService, IBlobService blobsService, DtoConvertor dtoConvertor, EntityConvertor entityConvertor) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly DtoConvertor dtoConvertor = dtoConvertor;
    private readonly EntityConvertor entityConvertor = entityConvertor;
    private readonly BlobContainerClient sessionsContainer = blobsService.GetBlobContainer(ContainerName.sessions);

    [HttpPost]
    public async Task<IActionResult> CreateNewSession([FromBody] SessionDto dto)
    {
        var imageId = Constants.DefaultSessionImageId;
        try
        {
            var imageBase64 = dto.ImageBase64;
            if (!string.IsNullOrEmpty(imageBase64))
            {
                imageId = Guid.NewGuid();
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(sessionsContainer, imageBase64, imageId);
            }

            var entity = entityConvertor.ToEntity(dto);
            entity.ImageId = imageId;

            // Sync navigation properties before saving
            await SyncNavigationPropertiesAsync(entity, dto);

            await databaseService.AddAsync(entity);
            
            // Return the created entity as DTO
            var createdDto = dtoConvertor.ToDTO(entity);
            
            // Copy the filmToDevKitMapping from the original DTO
            createdDto.FilmToDevKitMapping = dto.FilmToDevKitMapping;
            await ProcessSessionBusinessLogic(createdDto, null);
            
            return Created(string.Empty, createdDto);
        }
        catch (Exception ex)
        {
            if (imageId != Constants.DefaultSessionImageId)
                await sessionsContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();

            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSessions([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all sessions
        if (page <= 0)
        {
            var entities = await databaseService.GetAllWithIncludesAsync<SessionEntity>(
                s => s.UsedDevKits, 
                s => s.DevelopedFilms);
            return Ok(entities.Select(dtoConvertor.ToDTO));
        }

        var pagedEntities = await databaseService.GetPagedWithIncludesAsync<SessionEntity>(
            page, 
            pageSize, 
            entities => entities.ApplyStandardSorting(),
            s => s.UsedDevKits, 
            s => s.DevelopedFilms);
        var pagedResults = new PagedResponseDto<SessionDto>
        {
            Data = pagedEntities.Data.Select(dtoConvertor.ToDTO),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSessionById(string id)
    {
        // Load session with navigation properties
        var sessionEntity = await databaseService.GetByIdWithIncludesAsync<SessionEntity>(
            id, 
            s => s.UsedDevKits, 
            s => s.DevelopedFilms);
            
        if (sessionEntity == null)
            return NotFound($"No Session found with Id: {id}");

        var sessionDto = dtoConvertor.ToDTO(sessionEntity);
        
        // Populate FilmToDevKitMapping based on loaded relationships
        await PopulateFilmToDevKitMapping(sessionDto, sessionEntity);
        
        return Ok(sessionDto);
    }
    
    /// <summary>
    /// Syncs navigation properties on SessionEntity from SessionDto.
    /// Loads DevKit and Film entities and populates UsedDevKits and DevelopedFilms collections.
    /// </summary>
    private async Task SyncNavigationPropertiesAsync(SessionEntity entity, SessionDto dto)
    {
        // Clear existing collections
        entity.UsedDevKits.Clear();
        entity.DevelopedFilms.Clear();
        
        // Populate UsedDevKits (many-to-many)
        var devKitIds = dto.UsedSubstancesList ?? [];
        foreach (var devKitId in devKitIds)
        {
            var devKit = await databaseService.GetByIdAsync<DevKitEntity>(devKitId);
            if (devKit != null)
            {
                entity.UsedDevKits.Add(devKit);
            }
        }
        
        // Populate DevelopedFilms (one-to-many)
        var filmIds = dto.DevelopedFilmsList ?? [];
        foreach (var filmId in filmIds)
        {
            var film = await databaseService.GetByIdAsync<FilmEntity>(filmId);
            if (film != null)
            {
                entity.DevelopedFilms.Add(film);
            }
        }
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
        if (updateDto == null)
            return BadRequest("Invalid data.");

        // Get the original session first with navigation properties
        var originalSession = await databaseService.GetByIdWithIncludesAsync<SessionEntity>(
            id, 
            s => s.UsedDevKits, 
            s => s.DevelopedFilms);
        if (originalSession == null)
            return NotFound();
        
        SessionDto? originalDto = dtoConvertor.ToDTO(originalSession);
        
        // Handle image update if provided
        var imageBase64 = updateDto.ImageBase64;
        if (!string.IsNullOrEmpty(imageBase64))
        {
            if (originalSession.ImageId != Constants.DefaultSessionImageId)
            {
                await sessionsContainer.DeleteBlobAsync(originalSession.ImageId.ToString());
            }

            var newImageId = Guid.NewGuid();
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(sessionsContainer, imageBase64, newImageId);
            originalSession.ImageId = newImageId;
        }

        // Update entity using the Update method
        originalSession.Update(updateDto);
        
        // Sync navigation properties before updating
        await SyncNavigationPropertiesAsync(originalSession, updateDto);
        
        // UpdateAsync will handle UpdatedDate
        await databaseService.UpdateAsync(originalSession);
        
        // Process the business logic
        await ProcessSessionBusinessLogic(updateDto, originalDto);
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSession(string id)
    {
        // Get the session before deletion to revert business logic
        var sessionToDelete = await databaseService.GetByIdAsync<SessionEntity>(id);
        if (sessionToDelete == null)
            return NotFound();
        
        SessionDto? sessionDto = dtoConvertor.ToDTO(sessionToDelete);
        
        // Delete image blob if not default
        if (sessionToDelete.ImageId != Constants.DefaultSessionImageId)
            await sessionsContainer.DeleteBlobAsync(sessionToDelete.ImageId.ToString());
        
        await databaseService.DeleteAsync(sessionToDelete);
        
        // Revert the business logic
        await RevertSessionBusinessLogic(sessionDto);
        
        return NoContent();
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
                
                // Update film properties safely
                await UpdateFilmEntitySafelyAsync(filmId, film =>
                {
                    film.Developed = true;
                    film.DevelopedInSessionId = newSession.Id;
                    film.DevelopedWithDevKitId = devKitId; // Can be null if film is unassigned
                });
                
                // Increment devkit count if film is assigned to a devkit
                if (!string.IsNullOrEmpty(devKitId))
                {
                    await UpdateDevKitEntitySafelyAsync(devKitId, devKit =>
                    {
                        devKit.FilmsDeveloped++;
                    });
                }
            }
            
            // Process removed films (revert their state)
            foreach (var filmId in removedFilms)
            {
                // Get film to check its current state and devkit reference
                var filmInfo = await databaseService.GetByIdAsync<FilmEntity>(filmId);
                
                if (filmInfo != null && filmInfo.Developed)
                {
                    // Decrement devkit count if film was assigned to one
                    if (!string.IsNullOrEmpty(filmInfo.DevelopedWithDevKitId))
                    {
                        await UpdateDevKitEntitySafelyAsync(filmInfo.DevelopedWithDevKitId, devKit =>
                        {
                            devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - 1);
                        });
                    }
                    
                    // Clear film references
                    await UpdateFilmEntitySafelyAsync(filmId, film =>
                    {
                        film.Developed = false;
                        film.DevelopedInSessionId = null;
                        film.DevelopedWithDevKitId = null;
                    });
                }
            }
            
            // Handle films that changed devkits (in updates only)
            if (originalSession != null)
            {
                var unchangedFilms = newDevelopedFilms.Intersect(originalDevelopedFilms).ToList();
                foreach (var filmId in unchangedFilms)
                {
                    // Get film info
                    var filmInfo = await databaseService.GetByIdAsync<FilmEntity>(filmId);
                    
                    if (filmInfo != null)
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
                        if (filmInfo.DevelopedWithDevKitId != newDevKitId)
                        {
                            // Decrement old devkit count
                            if (!string.IsNullOrEmpty(filmInfo.DevelopedWithDevKitId))
                            {
                                await UpdateDevKitEntitySafelyAsync(filmInfo.DevelopedWithDevKitId, devKit =>
                                {
                                    devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - 1);
                                });
                            }
                            
                            // Increment new devkit count
                            if (!string.IsNullOrEmpty(newDevKitId))
                            {
                                await UpdateDevKitEntitySafelyAsync(newDevKitId, devKit =>
                                {
                                    devKit.FilmsDeveloped++;
                                });
                            }
                            
                            // Update film's devkit reference
                            await UpdateFilmEntitySafelyAsync(filmId, film =>
                            {
                                film.DevelopedWithDevKitId = newDevKitId;
                            });
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
                // Get film info
                var filmInfo = await databaseService.GetByIdAsync<FilmEntity>(filmId);
                
                if (filmInfo != null && filmInfo.Developed)
                {
                    // Decrement devkit count if film was assigned to one
                    if (!string.IsNullOrEmpty(filmInfo.DevelopedWithDevKitId))
                    {
                        await UpdateDevKitEntitySafelyAsync(filmInfo.DevelopedWithDevKitId, devKit =>
                        {
                            devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - 1);
                        });
                    }
                    
                    // Clear film references
                    await UpdateFilmEntitySafelyAsync(filmId, film =>
                    {
                        film.Developed = false;
                        film.DevelopedInSessionId = null;
                        film.DevelopedWithDevKitId = null;
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reverting session business logic: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Safely updates a FilmEntity by loading, applying changes, and using UpdateAsync.
    /// </summary>
    private async Task UpdateFilmEntitySafelyAsync(string filmId, Action<FilmEntity> updateAction)
    {
        // Load existing entity
        var existingEntity = await databaseService.GetByIdAsync<FilmEntity>(filmId);
        
        if (existingEntity == null)
            return;
        
        // Apply the update action to get the modified entity
        updateAction(existingEntity);
        
        // UpdateAsync will handle UpdatedDate automatically
        await databaseService.UpdateAsync(existingEntity);
    }
    
    /// <summary>
    /// Safely updates a DevKitEntity by loading, applying changes, and using UpdateAsync.
    /// </summary>
    private async Task UpdateDevKitEntitySafelyAsync(string devKitId, Action<DevKitEntity> updateAction)
    {
        // Load existing entity
        var existingEntity = await databaseService.GetByIdAsync<DevKitEntity>(devKitId);
        
        if (existingEntity == null)
            return;
        
        // Apply the update action to get the modified entity
        updateAction(existingEntity);
        
        // UpdateAsync will handle UpdatedDate automatically
        await databaseService.UpdateAsync(existingEntity);
    }
}
