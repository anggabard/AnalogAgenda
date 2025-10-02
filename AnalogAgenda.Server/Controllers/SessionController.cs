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
    protected override Guid GetDefaultImageId() => Constants.DefaultSessionImageId; // Reuse film default image for now
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
        return await GetEntityByRowKeyAsync(rowKey);
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateSession(string rowKey, [FromBody] SessionDto updateDto)
    {
        // Get the original session first
        var originalSession = await tablesService.GetTableEntryIfExistsAsync<SessionEntity>(rowKey);
        SessionDto? originalDto = originalSession?.ToDTO(storageCfg.AccountName);
        
        var result = await UpdateEntityWithImageAsync(rowKey, updateDto, dto => dto.ImageBase64);
        
        // If update was successful, process the business logic
        if (result is OkResult)
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
    /// - Mark films as developed when assigned to devkits
    /// - Increment devkit FilmsDeveloped count
    /// </summary>
    private async Task ProcessSessionBusinessLogic(SessionDto newSession, SessionDto? originalSession)
    {
        try
        {
            var newDevelopedFilms = newSession.DevelopedFilmsList ?? [];
            var originalDevelopedFilms = originalSession?.DevelopedFilmsList ?? [];
            var newUsedSubstances = newSession.UsedSubstancesList ?? [];
            var originalUsedSubstances = originalSession?.UsedSubstancesList ?? [];
            
            // Films that were added in this update
            var addedFilms = newDevelopedFilms.Except(originalDevelopedFilms).ToList();
            // Films that were removed in this update
            var removedFilms = originalDevelopedFilms.Except(newDevelopedFilms).ToList();
            
            // Mark newly added films as developed
            var filmsTable = tablesService.GetTable(TableName.Films);
            foreach (var filmRowKey in addedFilms)
            {
                var film = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowKey);
                if (film != null && !film.Developed)
                {
                    film.Developed = true;
                    film.UpdatedDate = DateTime.UtcNow;
                    await filmsTable.UpdateEntityAsync(film, film.ETag, TableUpdateMode.Replace);
                }
            }
            
            // Mark removed films as not developed (revert)
            foreach (var filmRowKey in removedFilms)
            {
                var film = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowKey);
                if (film != null && film.Developed)
                {
                    film.Developed = false;
                    film.UpdatedDate = DateTime.UtcNow;
                    await filmsTable.UpdateEntityAsync(film, film.ETag, TableUpdateMode.Replace);
                }
            }
            
            // Update devkit film counts for newly used substances
            var addedSubstances = newUsedSubstances.Except(originalUsedSubstances).ToList();
            var removedSubstances = originalUsedSubstances.Except(newUsedSubstances).ToList();
            
            var devKitsTable = tablesService.GetTable(TableName.DevKits);
            foreach (var devKitRowKey in addedSubstances)
            {
                var devKit = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(devKitRowKey);
                if (devKit != null)
                {
                    // Count how many films from this session were developed with this devkit
                    var filmsForThisDevKit = newDevelopedFilms.Count; // In this simplified version, all films in session are counted
                    devKit.FilmsDeveloped += filmsForThisDevKit;
                    devKit.UpdatedDate = DateTime.UtcNow;
                    await devKitsTable.UpdateEntityAsync(devKit, devKit.ETag, TableUpdateMode.Replace);
                }
            }
            
            // Revert devkit film counts for removed substances
            foreach (var devKitRowKey in removedSubstances)
            {
                var devKit = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(devKitRowKey);
                if (devKit != null)
                {
                    var filmsForThisDevKit = originalDevelopedFilms.Count;
                    devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - filmsForThisDevKit);
                    devKit.UpdatedDate = DateTime.UtcNow;
                    await devKitsTable.UpdateEntityAsync(devKit, devKit.ETag, TableUpdateMode.Replace);
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
            var usedSubstances = deletedSession.UsedSubstancesList ?? [];
            
            // Mark films as not developed
            var filmsTable = tablesService.GetTable(TableName.Films);
            foreach (var filmRowKey in developedFilms)
            {
                var film = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowKey);
                if (film != null && film.Developed)
                {
                    film.Developed = false;
                    film.UpdatedDate = DateTime.UtcNow;
                    await filmsTable.UpdateEntityAsync(film, film.ETag, TableUpdateMode.Replace);
                }
            }
            
            // Decrement devkit film counts
            var devKitsTable = tablesService.GetTable(TableName.DevKits);
            foreach (var devKitRowKey in usedSubstances)
            {
                var devKit = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(devKitRowKey);
                if (devKit != null)
                {
                    devKit.FilmsDeveloped = Math.Max(0, devKit.FilmsDeveloped - developedFilms.Count);
                    devKit.UpdatedDate = DateTime.UtcNow;
                    await devKitsTable.UpdateEntityAsync(devKit, devKit.ETag, TableUpdateMode.Replace);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reverting session business logic: {ex.Message}");
        }
    }
}
