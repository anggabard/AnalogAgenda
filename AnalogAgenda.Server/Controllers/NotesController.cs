using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
public class NotesController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) : BaseEntityController<NoteEntity, NoteDto>(storageCfg, tablesService, blobsService)
{
    private readonly TableClient notesTable = tablesService.GetTable(TableName.Notes);
    private readonly TableClient notesEntriesTable = tablesService.GetTable(TableName.NotesEntries);
    private readonly TableClient notesEntryRulesTable = tablesService.GetTable(TableName.NotesEntryRules);
    private readonly TableClient notesEntryOverridesTable = tablesService.GetTable(TableName.NotesEntryOverrides);
    private readonly BlobContainerClient notesContainer = blobsService.GetBlobContainer(ContainerName.notes);

    protected override TableClient GetTable() => notesTable;
    protected override BlobContainerClient GetBlobContainer() => notesContainer;
    protected override Guid GetDefaultImageId() => Constants.DefaultNoteImageId;
    protected override NoteEntity DtoToEntity(NoteDto dto) => dto.ToNoteEntity();
    protected override NoteDto EntityToDto(NoteEntity entity) => entity.ToDTO(storageCfg.AccountName);
    
    private async Task<NoteDto> EntityToDtoWithEntriesAsync(NoteEntity entity, IEnumerable<NoteEntryEntity> entries)
    {
        var entryList = entries.ToList();
        var entryRowKeys = entryList.Select(e => e.RowKey).ToList();
        
        // Get rules and overrides for all entries
        var rules = await tablesService.GetTableEntriesAsync<NoteEntryRuleEntity>();
        var overrides = await tablesService.GetTableEntriesAsync<NoteEntryOverrideEntity>();
        
        var rulesByEntry = rules.Where(r => entryRowKeys.Contains(r.NoteEntryRowKey))
            .GroupBy(r => r.NoteEntryRowKey)
            .ToDictionary(g => g.Key, g => g.Select(r => r.ToDTO()).ToList());
            
        var overridesByEntry = overrides.Where(o => entryRowKeys.Contains(o.NoteEntryRowKey))
            .GroupBy(o => o.NoteEntryRowKey)
            .ToDictionary(g => g.Key, g => g.Select(o => o.ToDTO()).ToList());
        
        // Create DTOs with rules and overrides
        var entryDtos = entryList.Select(entry =>
        {
            var dto = entry.ToDTO();
            dto.Rules = rulesByEntry.GetValueOrDefault(entry.RowKey, new List<NoteEntryRuleDto>());
            dto.Overrides = overridesByEntry.GetValueOrDefault(entry.RowKey, new List<NoteEntryOverrideDto>());
            return dto;
        }).ToList();
        
        return entity.ToDTO(storageCfg.AccountName, entryDtos);
    }

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
        
        if (noteCreationResult is not CreatedResult createdResult)
        {
            return noteCreationResult; // Return error if note creation failed
        }

        // If note creation succeeded, create the note entries
        if (createdResult.Value is not NoteDto createdNote)
        {
            return UnprocessableEntity("Failed to retrieve created note.");
        }

        var entries = dto.ToNoteEntryEntities(createdNote.RowKey);

        try
        {
            foreach (var entry in entries)
            {
                await notesEntriesTable.AddEntityAsync(entry);
            }
            
            // Save rules and overrides for each entry
            await SaveRulesAndOverridesAsync(dto.Entries);
            
            return Created(string.Empty, createdNote);
        }
        catch (Exception ex)
        {
            // If entries creation failed, clean up the note we just created
            await DeleteNoteAndCleanupAsync(createdNote.RowKey);
            return UnprocessableEntity($"Failed to create note entries: {ex.Message}");
        }
    }

    private async Task SaveRulesAndOverridesAsync(List<NoteEntryDto> entries)
    {
        foreach (var entry in entries)
        {
            await SaveRulesAndOverridesForEntryAsync(entry);
        }
    }

    private async Task SaveRulesAndOverridesForEntryAsync(NoteEntryDto entry)
    {
        // Save rules
        foreach (var rule in entry.Rules)
        {
            var ruleEntity = rule.ToEntity(entry.RowKey);
            await notesEntryRulesTable.AddEntityAsync(ruleEntity);
        }
        
        // Save overrides
        foreach (var overrideItem in entry.Overrides)
        {
            var overrideEntity = overrideItem.ToEntity(entry.RowKey);
            await notesEntryOverridesTable.AddEntityAsync(overrideEntity);
        }
    }

    private async Task UpdateRulesAndOverridesForEntryAsync(NoteEntryDto entry)
    {
        // Delete existing rules and overrides
        await tablesService.DeleteTableEntriesAsync<NoteEntryRuleEntity>(rule => rule.NoteEntryRowKey == entry.RowKey);
        await tablesService.DeleteTableEntriesAsync<NoteEntryOverrideEntity>(overrideItem => overrideItem.NoteEntryRowKey == entry.RowKey);
        
        // Save new rules and overrides
        await SaveRulesAndOverridesForEntryAsync(entry);
    }

    private async Task DeleteRulesAndOverridesForEntriesAsync(List<string> entryRowKeys)
    {
        foreach (var entryRowKey in entryRowKeys)
        {
            await tablesService.DeleteTableEntriesAsync<NoteEntryRuleEntity>(rule => rule.NoteEntryRowKey == entryRowKey);
            await tablesService.DeleteTableEntriesAsync<NoteEntryOverrideEntity>(overrideItem => overrideItem.NoteEntryRowKey == entryRowKey);
        }
    }

    private async Task DeleteNoteAndCleanupAsync(string noteRowKey)
    {
        try
        {
            // Delete rules and overrides first
            await tablesService.DeleteTableEntriesAsync<NoteEntryRuleEntity>(rule => 
                tablesService.GetTableEntriesAsync<NoteEntryEntity>(entry => entry.NoteRowKey == noteRowKey)
                    .Result.Any(e => e.RowKey == rule.NoteEntryRowKey));
            await tablesService.DeleteTableEntriesAsync<NoteEntryOverrideEntity>(overrideItem => 
                tablesService.GetTableEntriesAsync<NoteEntryEntity>(entry => entry.NoteRowKey == noteRowKey)
                    .Result.Any(e => e.RowKey == overrideItem.NoteEntryRowKey));
            
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

            var results = new List<NoteDto>();
            foreach (var noteEntity in notesEntities)
            {
                var entryEntities = entriesByNoteKey.GetValueOrDefault(noteEntity.RowKey, new List<NoteEntryEntity>())
                    .OrderBy(e => e.Index).ToList();
                var noteDto = await EntityToDtoWithEntriesAsync(noteEntity, entryEntities);
                results.Add(noteDto);
            }

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

        var notesWithEntries = new List<NoteDto>();
        foreach (var noteEntity in pagedEntities.Data)
        {
            var entryEntities = pagedEntriesByNoteKey.GetValueOrDefault(noteEntity.RowKey, new List<NoteEntryEntity>())
                .OrderBy(e => e.Index).ToList();
            var noteDto = await EntityToDtoWithEntriesAsync(noteEntity, entryEntities);
            notesWithEntries.Add(noteDto);
        }

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

        return Ok(await EntityToDtoWithEntriesAsync(noteEntity, entryEntities.OrderBy(entry => entry.Index).ToList()));
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
                    // Save rules and overrides for new entry
                    await SaveRulesAndOverridesForEntryAsync(noteEntryDto);
                    continue;
                }

                var existingEntryEntity = existingEntryEntities.FirstOrDefault(existingEntry => existingEntry.RowKey == noteEntryDto.RowKey);
                if (existingEntryEntity == null)
                    continue;

                updatedNoteEntryEntity.CreatedDate = existingEntryEntity.CreatedDate;
                updatedNoteEntryEntity.UpdatedDate = DateTime.UtcNow;
                
                await notesEntriesTable.UpdateEntityAsync(updatedNoteEntryEntity, existingEntryEntity.ETag, TableUpdateMode.Replace);
                
                // Update rules and overrides for existing entry
                await UpdateRulesAndOverridesForEntryAsync(noteEntryDto);
                
                existingEntryEntities.Remove(existingEntryEntity);
            }

            // Delete remaining entries and their rules/overrides
            await DeleteRulesAndOverridesForEntriesAsync(existingEntryEntities.Select(e => e.RowKey).ToList());
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
        // Get entry row keys first
        var entryEntities = await tablesService.GetTableEntriesAsync<NoteEntryEntity>(entry => entry.NoteRowKey == rowKey);
        var entryRowKeys = entryEntities.Select(e => e.RowKey).ToList();
        
        // Delete rules and overrides for all entries
        await DeleteRulesAndOverridesForEntriesAsync(entryRowKeys);
        
        // Delete note entries
        await tablesService.DeleteTableEntriesAsync(entryEntities);
        
        // Then delete the note (this will also handle image cleanup via base controller)
        return await DeleteEntityWithImageAsync(rowKey);
    }

    [HttpGet("merge/{compositeId}")]
    public async Task<IActionResult> GetMergedNotes(string compositeId)
    {
        try
        {
            // Decode composite ID into individual note RowKeys
            var noteRowKeys = DecodeCompositeId(compositeId);
            
            if (noteRowKeys.Count == 0)
            {
                return BadRequest("Invalid composite ID format");
            }

            // Fetch all notes with their entries, rules, and overrides
            var notes = new List<NoteDto>();
            foreach (var noteRowKey in noteRowKeys)
            {
                var noteEntity = await tablesService.GetTableEntryIfExistsAsync<NoteEntity>(noteRowKey);
                if (noteEntity == null) continue;

                var entryEntities = await tablesService.GetTableEntriesAsync<NoteEntryEntity>(entry => entry.NoteRowKey == noteEntity.RowKey);
                var noteDto = await EntityToDtoWithEntriesAsync(noteEntity, entryEntities.OrderBy(e => e.Index).ToList());
                notes.Add(noteDto);
            }

            if (notes.Count == 0)
            {
                return NotFound("No notes found for the given composite ID");
            }

            return Ok(notes);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity($"Failed to merge notes: {ex.Message}");
        }
    }

    private List<string> DecodeCompositeId(string compositeId)
    {
        // Composite ID is created by interleaving characters from note RowKeys
        // Each note RowKey is 4 characters, so for 2 notes: positions 0,2,4,6 belong to first note, 1,3,5,7 to second
        var noteRowKeys = new List<string>();
        var noteCount = compositeId.Length / 4; // Each note contributes 4 characters
        
        for (int noteIndex = 0; noteIndex < noteCount; noteIndex++)
        {
            var rowKey = "";
            for (int charIndex = 0; charIndex < 4; charIndex++)
            {
                var position = noteIndex + charIndex * noteCount;
                if (position < compositeId.Length)
                {
                    rowKey += compositeId[position];
                }
            }
            if (rowKey.Length == 4)
            {
                noteRowKeys.Add(rowKey);
            }
        }
        
        return noteRowKeys;
    }


}