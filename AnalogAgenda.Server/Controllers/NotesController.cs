using AnalogAgenda.Server.Helpers;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
public class NotesController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) : BaseEntityController<NoteEntity, NoteDto>(storageCfg, tablesService, blobsService)
{
    private readonly TableClient notesTable = tablesService.GetTable(TableName.Notes);
    private readonly TableClient notesEntriesTable = tablesService.GetTable(TableName.NotesEntries);
    private readonly BlobContainerClient notesContainer = blobsService.GetBlobContainer(ContainerName.notes);

    protected override TableClient GetTable() => notesTable;
    protected override BlobContainerClient GetBlobContainer() => notesContainer;
    protected override Guid GetDefaultImageId() => Constants.DefaultNoteImageId;
    protected override NoteEntity DtoToEntity(NoteDto dto) => dto.ToNoteEntity();
    protected override NoteDto EntityToDto(NoteEntity entity) => entity.ToDTO(storageCfg.AccountName);
    
    private NoteDto EntityToDtoWithEntries(NoteEntity entity, IEnumerable<NoteEntryEntity> entries) => 
        entity.ToDTO(storageCfg.AccountName, entries.ToList());

    [HttpPost]
    public async Task<IActionResult> CreateNewNote([FromBody] NoteDto dto)
    {
        return await CreateNoteWithEntriesAsync(dto);
    }

    private async Task<IActionResult> CreateNoteWithEntriesAsync(NoteDto dto)
    {
        // First create the note using the base controller's image handling
        var creationDate = DateTime.UtcNow;
        var noteCreationResult = await CreateEntityWithImageAsync(dto, dto => dto.ImageBase64, creationDate);
        
        if (noteCreationResult is not OkResult)
        {
            return noteCreationResult; // Return error if note creation failed
        }

        // If note creation succeeded, create the note entries
        var noteEntity = dto.ToNoteEntity();
        noteEntity.CreatedDate = creationDate;
        var entries = dto.ToNoteEntryEntities(noteEntity.RowKey);

        try
        {
            foreach (var entry in entries)
            {
                await notesEntriesTable.AddEntityAsync(entry);
            }
            
            return Ok(noteEntity.RowKey);
        }
        catch (Exception ex)
        {
            // If entries creation failed, clean up the note we just created
            await DeleteNoteAndCleanupAsync(noteEntity.RowKey);
            return UnprocessableEntity($"Failed to create note entries: {ex.Message}");
        }
    }

    private async Task DeleteNoteAndCleanupAsync(string noteRowKey)
    {
        try
        {
            // Delete the note (this will also handle image cleanup via base controller logic)
            await DeleteEntityWithImageAsync(noteRowKey);
        }
        catch
        {
            // Swallow cleanup errors - the original error is more important
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllNotes([FromQuery] bool withEntries = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all notes
        if (page <= 0)
        {
            var notesEntities = await tablesService.GetTableEntriesAsync<NoteEntity>();

            if (!withEntries)
            {
                return Ok(notesEntities.Select(EntityToDto));
            }

            // Get all note entries in a single query instead of N+1 queries
            var noteRowKeys = notesEntities.Select(n => n.RowKey).ToList();
            var allEntryEntities = await tablesService.GetTableEntriesAsync<NoteEntryEntity>();
            var entriesByNoteKey = allEntryEntities
                .Where(entry => noteRowKeys.Contains(entry.NoteRowKey))
                .GroupBy(entry => entry.NoteRowKey)
                .ToDictionary(g => g.Key, g => g.ToList());

            var results = notesEntities.Select(noteEntity =>
                {
                    var entryEntities = entriesByNoteKey.GetValueOrDefault(noteEntity.RowKey, new List<NoteEntryEntity>());
                    return EntityToDtoWithEntries(noteEntity, entryEntities);
                });

            return Ok(results);
        }

        var pagedEntities = await tablesService.GetTableEntriesPagedAsync<NoteEntity>(page, pageSize);
        
        if (!withEntries)
        {
            var pagedResults = new PagedResponseDto<NoteDto>
            {
                Data = pagedEntities.Data.Select(EntityToDto),
                TotalCount = pagedEntities.TotalCount,
                PageSize = pagedEntities.PageSize,
                CurrentPage = pagedEntities.CurrentPage
            };
            return Ok(pagedResults);
        }

        // Get all note entries in a single query instead of N+1 queries
        var pagedNoteRowKeys = pagedEntities.Data.Select(n => n.RowKey).ToList();
        var pagedAllEntryEntities = await tablesService.GetTableEntriesAsync<NoteEntryEntity>();
        var pagedEntriesByNoteKey = pagedAllEntryEntities
            .Where(entry => pagedNoteRowKeys.Contains(entry.NoteRowKey))
            .GroupBy(entry => entry.NoteRowKey)
            .ToDictionary(g => g.Key, g => g.ToList());

        var notesWithEntries = pagedEntities.Data.Select(noteEntity =>
            {
                var entryEntities = pagedEntriesByNoteKey.GetValueOrDefault(noteEntity.RowKey, new List<NoteEntryEntity>());
                return EntityToDtoWithEntries(noteEntity, entryEntities);
            });

        var pagedResultsWithEntries = new PagedResponseDto<NoteDto>
        {
            Data = notesWithEntries,
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResultsWithEntries);
    }

    [HttpGet("{rowKey}")]
    public async Task<IActionResult> GetNoteByRowKey(string rowKey)
    {
        var noteEntity = await tablesService.GetTableEntryIfExistsAsync<NoteEntity>(rowKey);

        if (noteEntity == null)
        {
            return NotFound($"No Notes found with RowKey: {rowKey}");
        }

        var entryEntities = await tablesService.GetTableEntriesAsync<NoteEntryEntity>(entry => entry.NoteRowKey == noteEntity.RowKey);

        return Ok(EntityToDtoWithEntries(noteEntity, entryEntities.OrderBy(entry => entry.Time).ToList()));
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateNote(string rowKey, [FromBody] NoteDto updateDto)
    {
        return await UpdateNoteWithEntriesAsync(rowKey, updateDto);
    }

    private async Task<IActionResult> UpdateNoteWithEntriesAsync(string rowKey, NoteDto updateDto)
    {
        // First update the note using the base controller's image handling
        var noteUpdateResult = await UpdateEntityWithImageAsync(rowKey, updateDto, dto => dto.ImageBase64);
        
        if (noteUpdateResult is not NoContentResult)
        {
            return noteUpdateResult; // Return error if note update failed
        }

        // If note update succeeded, update the note entries
        return await UpdateNoteEntriesAsync(rowKey, updateDto);

    }

    private async Task<IActionResult> UpdateNoteEntriesAsync(string rowKey, NoteDto updateDto)
    {
        try
        {
            var existingEntryEntities = await tablesService.GetTableEntriesAsync<NoteEntryEntity>(entry => entry.NoteRowKey == rowKey);
            
            foreach (var noteEntryDto in updateDto.Entries)
            {
                var updatedNoteEntryEntity = noteEntryDto.ToEntity(rowKey);

                if (string.IsNullOrEmpty(noteEntryDto.RowKey))
                {
                    await notesEntriesTable.AddEntityAsync(updatedNoteEntryEntity);
                    continue;
                }

                var existingEntryEntity = existingEntryEntities.FirstOrDefault(existingEntry => existingEntry.RowKey == noteEntryDto.RowKey);
                if (existingEntryEntity == null)
                    continue;

                updatedNoteEntryEntity.CreatedDate = existingEntryEntity.CreatedDate;
                updatedNoteEntryEntity.UpdatedDate = DateTime.UtcNow;
                
                await notesEntriesTable.UpdateEntityAsync(updatedNoteEntryEntity, existingEntryEntity.ETag, TableUpdateMode.Replace);
                existingEntryEntities.Remove(existingEntryEntity);
            }

            await tablesService.DeleteTableEntriesAsync(existingEntryEntities);

            return NoContent();
        }
        catch (Exception ex)
        {
            return UnprocessableEntity($"Failed to update note entries: {ex.Message}");
        }
    }

    [HttpDelete("{rowKey}")]
    public async Task<IActionResult> DeleteNote(string rowKey)
    {
        return await DeleteNoteWithEntriesAsync(rowKey);
    }

    private async Task<IActionResult> DeleteNoteWithEntriesAsync(string rowKey)
    {
        // First delete note entries
        await tablesService.DeleteTableEntriesAsync<NoteEntryEntity>(entry => entry.NoteRowKey == rowKey);
        
        // Then delete the note (this will also handle image cleanup via base controller)
        return await DeleteEntityWithImageAsync(rowKey);
    }

}