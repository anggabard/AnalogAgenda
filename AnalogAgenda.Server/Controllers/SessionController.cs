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
public class SessionController(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService, AnalogAgendaDbContext dbContext) : BaseEntityController<SessionEntity, SessionDto>(storageCfg, databaseService, blobsService, dbContext)
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
            if (createdResult.Value is SessionDto createdSession)
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
        IQueryable<SessionEntity> query = dbContext.Sessions
            .Include(s => s.UsedDevKits)
            .Include(s => s.DevelopedFilms);
        
        if (page <= 0)
        {
            var entities = await query.ApplyStandardSorting().ToListAsync();
            return Ok(entities.Select(EntityToDto));
        }

        var totalCount = await query.CountAsync();
        var pagedEntities = await query
            .ApplyStandardSorting()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var pagedResults = new PagedResponseDto<SessionDto>
        {
            Data = pagedEntities.Select(EntityToDto),
            TotalCount = totalCount,
            PageSize = pageSize,
            CurrentPage = page
        };

        return Ok(pagedResults);
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
                    film.UpdatedDate = DateTime.UtcNow;
                });
                
                // Increment devkit count if film is assigned to a devkit
                if (!string.IsNullOrEmpty(devKitId))
                {
                    await UpdateDevKitEntitySafelyAsync(devKitId, devKit =>
                    {
                        devKit.FilmsDeveloped++;
                        devKit.UpdatedDate = DateTime.UtcNow;
                    });
                }
            }
            
            // Process removed films (revert their state)
            foreach (var filmId in removedFilms)
            {
                // Get film to check its current state and devkit reference
                var filmInfo = await dbContext.Set<FilmEntity>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == filmId);
                
                if (filmInfo != null && filmInfo.Developed)
                {
                    // Decrement devkit count if film was assigned to one
                    if (!string.IsNullOrEmpty(filmInfo.DevelopedWithDevKitId))
                    {
                        await UpdateDevKitEntitySafelyAsync(filmInfo.DevelopedWithDevKitId, devKit =>
                        {
                            devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - 1);
                            devKit.UpdatedDate = DateTime.UtcNow;
                        });
                    }
                    
                    // Clear film references
                    await UpdateFilmEntitySafelyAsync(filmId, film =>
                    {
                        film.Developed = false;
                        film.DevelopedInSessionId = null;
                        film.DevelopedWithDevKitId = null;
                        film.UpdatedDate = DateTime.UtcNow;
                    });
                }
            }
            
            // Handle films that changed devkits (in updates only)
            if (originalSession != null)
            {
                var unchangedFilms = newDevelopedFilms.Intersect(originalDevelopedFilms).ToList();
                foreach (var filmId in unchangedFilms)
                {
                    // Get film info without tracking
                    var filmInfo = await dbContext.Set<FilmEntity>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(f => f.Id == filmId);
                    
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
                                    devKit.UpdatedDate = DateTime.UtcNow;
                                });
                            }
                            
                            // Increment new devkit count
                            if (!string.IsNullOrEmpty(newDevKitId))
                            {
                                await UpdateDevKitEntitySafelyAsync(newDevKitId, devKit =>
                                {
                                    devKit.FilmsDeveloped++;
                                    devKit.UpdatedDate = DateTime.UtcNow;
                                });
                            }
                            
                            // Update film's devkit reference
                            await UpdateFilmEntitySafelyAsync(filmId, film =>
                            {
                                film.DevelopedWithDevKitId = newDevKitId;
                                film.UpdatedDate = DateTime.UtcNow;
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
                // Get film info without tracking
                var filmInfo = await dbContext.Set<FilmEntity>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == filmId);
                
                if (filmInfo != null && filmInfo.Developed)
                {
                    // Decrement devkit count if film was assigned to one
                    if (!string.IsNullOrEmpty(filmInfo.DevelopedWithDevKitId))
                    {
                        await UpdateDevKitEntitySafelyAsync(filmInfo.DevelopedWithDevKitId, devKit =>
                        {
                            devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - 1);
                            devKit.UpdatedDate = DateTime.UtcNow;
                        });
                    }
                    
                    // Clear film references
                    await UpdateFilmEntitySafelyAsync(filmId, film =>
                    {
                        film.Developed = false;
                        film.DevelopedInSessionId = null;
                        film.DevelopedWithDevKitId = null;
                        film.UpdatedDate = DateTime.UtcNow;
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
    /// Safely updates a FilmEntity by loading without tracking, applying changes, and saving.
    /// This avoids EF Core tracking conflicts when the same entity might be tracked elsewhere.
    /// </summary>
    private async Task UpdateFilmEntitySafelyAsync(string filmId, Action<FilmEntity> updateAction)
    {
        // Load entity without tracking
        var existingEntity = await dbContext.Set<FilmEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == filmId);
        
        if (existingEntity == null)
            return;
        
        // Apply updates to the existing entity (creating a new instance)
        var updatedEntity = new FilmEntity
        {
            Id = existingEntity.Id,
            CreatedDate = existingEntity.CreatedDate,
            UpdatedDate = DateTime.UtcNow,
            Name = existingEntity.Name,
            Iso = existingEntity.Iso,
            Type = existingEntity.Type,
            NumberOfExposures = existingEntity.NumberOfExposures,
            Cost = existingEntity.Cost,
            PurchasedBy = existingEntity.PurchasedBy,
            PurchasedOn = existingEntity.PurchasedOn,
            ImageId = existingEntity.ImageId,
            Description = existingEntity.Description,
            Developed = existingEntity.Developed,
            DevelopedInSessionId = existingEntity.DevelopedInSessionId,
            DevelopedWithDevKitId = existingEntity.DevelopedWithDevKitId,
            ExposureDates = existingEntity.ExposureDates
        };
        
        // Apply the update action
        updateAction(updatedEntity);
        
        // Attach and update
        dbContext.Set<FilmEntity>().Attach(updatedEntity);
        dbContext.Entry(updatedEntity).State = EntityState.Modified;
        
        // Clear navigation properties
        dbContext.Entry(updatedEntity).Reference(f => f.DevelopedWithDevKit).CurrentValue = null;
        dbContext.Entry(updatedEntity).Reference(f => f.DevelopedInSession).CurrentValue = null;
        dbContext.Entry(updatedEntity).Collection(f => f.Photos).IsLoaded = false;
        
        await dbContext.SaveChangesAsync();
    }
    
    /// <summary>
    /// Safely updates a DevKitEntity by loading without tracking, applying changes, and saving.
    /// This avoids EF Core tracking conflicts when the same entity might be tracked elsewhere.
    /// </summary>
    private async Task UpdateDevKitEntitySafelyAsync(string devKitId, Action<DevKitEntity> updateAction)
    {
        // Load entity without tracking
        var existingEntity = await dbContext.Set<DevKitEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == devKitId);
        
        if (existingEntity == null)
            return;
        
        // Apply updates to the existing entity (creating a new instance)
        var updatedEntity = new DevKitEntity
        {
            Id = existingEntity.Id,
            CreatedDate = existingEntity.CreatedDate,
            UpdatedDate = DateTime.UtcNow,
            Name = existingEntity.Name,
            Url = existingEntity.Url,
            Type = existingEntity.Type,
            PurchasedBy = existingEntity.PurchasedBy,
            PurchasedOn = existingEntity.PurchasedOn,
            MixedOn = existingEntity.MixedOn,
            ValidForWeeks = existingEntity.ValidForWeeks,
            ValidForFilms = existingEntity.ValidForFilms,
            FilmsDeveloped = existingEntity.FilmsDeveloped,
            ImageId = existingEntity.ImageId,
            Description = existingEntity.Description,
            Expired = existingEntity.Expired
        };
        
        // Apply the update action
        updateAction(updatedEntity);
        
        // Attach and update
        dbContext.Set<DevKitEntity>().Attach(updatedEntity);
        dbContext.Entry(updatedEntity).State = EntityState.Modified;
        
        // Clear navigation properties
        dbContext.Entry(updatedEntity).Collection(d => d.DevelopedFilms).IsLoaded = false;
        dbContext.Entry(updatedEntity).Collection(d => d.UsedInSessions).IsLoaded = false;
        
        await dbContext.SaveChangesAsync();
    }
}
