using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
public class SessionController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) : BaseEntityController<SessionEntity, SessionDto>(storageCfg, tablesService, blobsService)
{
    private readonly TableClient sessionsTable = tablesService.GetTable(TableName.Sessions);
    private readonly BlobContainerClient sessionsContainer = blobsService.GetBlobContainer(ContainerName.sessions);

    protected override TableClient GetTable() => sessionsTable;
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

    [HttpGet("{rowKey}")]
    public async Task<IActionResult> GetSessionByRowKey(string rowKey)
    {
        var sessionEntity = await tablesService.GetTableEntryIfExistsAsync<SessionEntity>(rowKey);
        if (sessionEntity == null)
            return NotFound($"No Session found with RowKey: {rowKey}");

        var sessionDto = EntityToDto(sessionEntity);
        
        // Populate FilmToDevKitMapping based on films' DevelopedWithDevKitRowKey
        await PopulateFilmToDevKitMapping(sessionDto);
        
        return Ok(sessionDto);
    }
    
    private async Task PopulateFilmToDevKitMapping(SessionDto sessionDto)
    {
        sessionDto.FilmToDevKitMapping = new Dictionary<string, List<string>>();
        
        var developedFilms = sessionDto.DevelopedFilmsList ?? [];
        if (developedFilms.Count == 0)
            return;
        
        // Get all films for this session
        var filmsTable = tablesService.GetTable(TableName.Films);
        foreach (var filmRowKey in developedFilms)
        {
            var film = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowKey);
            if (film != null && !string.IsNullOrEmpty(film.DevelopedWithDevKitRowKey))
            {
                // Add film to the devkit's list
                if (!sessionDto.FilmToDevKitMapping.ContainsKey(film.DevelopedWithDevKitRowKey))
                {
                    sessionDto.FilmToDevKitMapping[film.DevelopedWithDevKitRowKey] = new List<string>();
                }
                sessionDto.FilmToDevKitMapping[film.DevelopedWithDevKitRowKey].Add(filmRowKey);
            }
        }
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateSession(string rowKey, [FromBody] SessionDto updateDto)
    {
        // Get the original session first
        var originalSession = await tablesService.GetTableEntryIfExistsAsync<SessionEntity>(rowKey);
        SessionDto? originalDto = originalSession?.ToDTO(storageCfg.AccountName);
        
        var result = await UpdateEntityWithImageAsync(rowKey, updateDto, dto => dto.ImageBase64);
        
        // If update was successful, process the business logic
        if (result is NoContentResult)
        {
            await ProcessSessionBusinessLogic(updateDto, originalDto);
        }
        
        return result;
    }

    [HttpDelete("{rowKey}")]
    public async Task<IActionResult> DeleteSession(string rowKey)
    {
        // Get the session before deletion to revert business logic
        var sessionToDelete = await tablesService.GetTableEntryIfExistsAsync<SessionEntity>(rowKey);
        SessionDto? sessionDto = sessionToDelete?.ToDTO(storageCfg.AccountName);
        
        var result = await DeleteEntityWithImageAsync(rowKey);
        
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
            var filmsTable = tablesService.GetTable(TableName.Films);
            var devKitsTable = tablesService.GetTable(TableName.DevKits);
            
            var newDevelopedFilms = newSession.DevelopedFilmsList ?? [];
            var originalDevelopedFilms = originalSession?.DevelopedFilmsList ?? [];
            
            // Films that were added in this update
            var addedFilms = newDevelopedFilms.Except(originalDevelopedFilms).ToList();
            // Films that were removed in this update
            var removedFilms = originalDevelopedFilms.Except(newDevelopedFilms).ToList();
            
            // Process newly added films
            foreach (var filmRowKey in addedFilms)
            {
                var film = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowKey);
                if (film != null)
                {
                    // Find which devkit this film belongs to
                    string? devKitRowKey = null;
                    foreach (var mapping in newSession.FilmToDevKitMapping)
                    {
                        if (mapping.Value.Contains(filmRowKey))
                        {
                            devKitRowKey = mapping.Key;
                            break;
                        }
                    }
                    
                    // Update film properties
                    film.Developed = true;
                    film.DevelopedInSessionRowKey = newSession.RowKey;
                    film.DevelopedWithDevKitRowKey = devKitRowKey; // Can be null if film is unassigned
                    film.UpdatedDate = DateTime.UtcNow;
                    await filmsTable.UpdateEntityAsync(film, film.ETag, TableUpdateMode.Replace);
                    
                    // Increment devkit count if film is assigned to a devkit
                    if (!string.IsNullOrEmpty(devKitRowKey))
                    {
                        var devKit = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(devKitRowKey);
                        if (devKit != null)
                        {
                            devKit.FilmsDeveloped++;
                            devKit.UpdatedDate = DateTime.UtcNow;
                            await devKitsTable.UpdateEntityAsync(devKit, devKit.ETag, TableUpdateMode.Replace);
                        }
                    }
                }
            }
            
            // Process removed films (revert their state)
            foreach (var filmRowKey in removedFilms)
            {
                var film = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowKey);
                if (film != null && film.Developed)
                {
                    // Decrement devkit count if film was assigned to one
                    if (!string.IsNullOrEmpty(film.DevelopedWithDevKitRowKey))
                    {
                        var devKit = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(film.DevelopedWithDevKitRowKey);
                        if (devKit != null)
                        {
                            devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - 1);
                            devKit.UpdatedDate = DateTime.UtcNow;
                            await devKitsTable.UpdateEntityAsync(devKit, devKit.ETag, TableUpdateMode.Replace);
                        }
                    }
                    
                    // Clear film references
                    film.Developed = false;
                    film.DevelopedInSessionRowKey = null;
                    film.DevelopedWithDevKitRowKey = null;
                    film.UpdatedDate = DateTime.UtcNow;
                    await filmsTable.UpdateEntityAsync(film, film.ETag, TableUpdateMode.Replace);
                }
            }
            
            // Handle films that changed devkits (in updates only)
            if (originalSession != null)
            {
                var unchangedFilms = newDevelopedFilms.Intersect(originalDevelopedFilms).ToList();
                foreach (var filmRowKey in unchangedFilms)
                {
                    var film = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowKey);
                    if (film != null)
                    {
                        // Find new devkit for this film
                        string? newDevKitRowKey = null;
                        foreach (var mapping in newSession.FilmToDevKitMapping)
                        {
                            if (mapping.Value.Contains(filmRowKey))
                            {
                                newDevKitRowKey = mapping.Key;
                                break;
                            }
                        }
                        
                        // Check if devkit changed
                        if (film.DevelopedWithDevKitRowKey != newDevKitRowKey)
                        {
                            // Decrement old devkit count
                            if (!string.IsNullOrEmpty(film.DevelopedWithDevKitRowKey))
                            {
                                var oldDevKit = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(film.DevelopedWithDevKitRowKey);
                                if (oldDevKit != null)
                                {
                                    oldDevKit.FilmsDeveloped = Math.Max(0, oldDevKit.FilmsDeveloped - 1);
                                    oldDevKit.UpdatedDate = DateTime.UtcNow;
                                    await devKitsTable.UpdateEntityAsync(oldDevKit, oldDevKit.ETag, TableUpdateMode.Replace);
                                }
                            }
                            
                            // Increment new devkit count
                            if (!string.IsNullOrEmpty(newDevKitRowKey))
                            {
                                var newDevKit = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(newDevKitRowKey);
                                if (newDevKit != null)
                                {
                                    newDevKit.FilmsDeveloped++;
                                    newDevKit.UpdatedDate = DateTime.UtcNow;
                                    await devKitsTable.UpdateEntityAsync(newDevKit, newDevKit.ETag, TableUpdateMode.Replace);
                                }
                            }
                            
                            // Update film's devkit reference
                            film.DevelopedWithDevKitRowKey = newDevKitRowKey;
                            film.UpdatedDate = DateTime.UtcNow;
                            await filmsTable.UpdateEntityAsync(film, film.ETag, TableUpdateMode.Replace);
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
            var filmsTable = tablesService.GetTable(TableName.Films);
            var devKitsTable = tablesService.GetTable(TableName.DevKits);
            
            // Revert each film's state
            foreach (var filmRowKey in developedFilms)
            {
                var film = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowKey);
                if (film != null && film.Developed)
                {
                    // Decrement devkit count if film was assigned to one
                    if (!string.IsNullOrEmpty(film.DevelopedWithDevKitRowKey))
                    {
                        var devKit = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(film.DevelopedWithDevKitRowKey);
                        if (devKit != null)
                        {
                            devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - 1);
                            devKit.UpdatedDate = DateTime.UtcNow;
                            await devKitsTable.UpdateEntityAsync(devKit, devKit.ETag, TableUpdateMode.Replace);
                        }
                    }
                    
                    // Clear film references
                    film.Developed = false;
                    film.DevelopedInSessionRowKey = null;
                    film.DevelopedWithDevKitRowKey = null;
                    film.UpdatedDate = DateTime.UtcNow;
                    await filmsTable.UpdateEntityAsync(film, film.ETag, TableUpdateMode.Replace);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reverting session business logic: {ex.Message}");
        }
    }
}
