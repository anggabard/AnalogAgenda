using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Database.Data;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
public class NotesController(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService, AnalogAgendaDbContext dbContext) : BaseEntityController<NoteEntity, NoteDto>(storageCfg, databaseService, blobsService)
{
    private readonly BlobContainerClient notesContainer = blobsService.GetBlobContainer(ContainerName.notes);

    protected override BlobContainerClient GetBlobContainer() => notesContainer;
    protected override Guid GetDefaultImageId() => Constants.DefaultNoteImageId;
    protected override NoteEntity DtoToEntity(NoteDto dto) => dto.ToNoteEntity();
    protected override NoteDto EntityToDto(NoteEntity entity) => entity.ToDTO(storageCfg.AccountName);
    
    private async Task<NoteDto> EntityToDtoWithEntriesAsync(NoteEntity entity, IEnumerable<NoteEntryEntity> entries)
    {
        var entryList = entries.ToList();
        var entryIds = entryList.Select(e => e.Id).ToList();
        
        // Get rules and overrides for all entries
        var rules = await dbContext.NoteEntryRules
            .Where(r => entryIds.Contains(r.NoteEntryId))
            .ToListAsync();
        var overrides = await dbContext.NoteEntryOverrides
            .Where(o => entryIds.Contains(o.NoteEntryId))
            .ToListAsync();
        
        var rulesByEntry = rules
            .GroupBy(r => r.NoteEntryId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.ToDTO()).ToList());
            
        var overridesByEntry = overrides
            .GroupBy(o => o.NoteEntryId)
            .ToDictionary(g => g.Key, g => g.Select(o => o.ToDTO()).ToList());
        
        // Create DTOs with rules and overrides
        var entryDtos = entryList.OrderBy(e => e.Time).Select(entry =>
        {
            var dto = entry.ToDTO();
            dto.Rules = rulesByEntry.GetValueOrDefault(entry.Id, new List<NoteEntryRuleDto>());
            dto.Overrides = overridesByEntry.GetValueOrDefault(entry.Id, new List<NoteEntryOverrideDto>());
            return dto;
        }).ToList();
        
        var noteDto = entity.ToDTO(storageCfg.AccountName);
        noteDto.Entries = entryDtos;
        return noteDto;
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

        var entries = dto.ToNoteEntryEntities(createdNote.Id);

        try
        {
            foreach (var entry in entries)
            {
                await databaseService.AddAsync(entry);
            }
            
            // Save rules and overrides for each entry
            await SaveRulesAndOverridesAsync(dto.Entries);
            
            return Created(string.Empty, createdNote);
        }
        catch (Exception ex)
        {
            // If entries creation failed, clean up the note we just created
            await DeleteNoteAndCleanupAsync(createdNote.Id);
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
            var ruleEntity = rule.ToEntity(entry.Id);
            await databaseService.AddAsync(ruleEntity);
        }
        
        // Save overrides
        foreach (var overrideItem in entry.Overrides)
        {
            var overrideEntity = overrideItem.ToEntity(entry.Id);
            await databaseService.AddAsync(overrideEntity);
        }
    }

    private async Task UpdateRulesAndOverridesForEntryAsync(NoteEntryDto entry)
    {
        // Delete existing rules and overrides
        await databaseService.DeleteRangeAsync<NoteEntryRuleEntity>(r => r.NoteEntryId == entry.Id);
        await databaseService.DeleteRangeAsync<NoteEntryOverrideEntity>(o => o.NoteEntryId == entry.Id);
        
        // Save new rules and overrides
        await SaveRulesAndOverridesForEntryAsync(entry);
    }

    private async Task DeleteNoteAndCleanupAsync(string noteId)
    {
        try
        {
            // Get all entry IDs for this note
            var entryIds = await dbContext.NoteEntries
                .Where(e => e.NoteId == noteId)
                .Select(e => e.Id)
                .ToListAsync();
            
            // Delete rules and overrides for all entries
            await databaseService.DeleteRangeAsync<NoteEntryRuleEntity>(r => entryIds.Contains(r.NoteEntryId));
            await databaseService.DeleteRangeAsync<NoteEntryOverrideEntity>(o => entryIds.Contains(o.NoteEntryId));
            
            // Delete note entries
            await databaseService.DeleteRangeAsync<NoteEntryEntity>(entry => entry.NoteId == noteId);
            
            // Delete the note (this will also handle image cleanup via base controller logic)
            await DeleteEntityWithImageAsync(noteId);
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
            IQueryable<NoteEntity> query = dbContext.Notes;
            
            if (withEntries)
            {
                query = query.Include(n => n.Entries)
                    .ThenInclude(e => e.Rules)
                    .Include(n => n.Entries)
                    .ThenInclude(e => e.Overrides);
            }

            var notesEntities = await query.ToListAsync();

            if (!withEntries)
            {
                return Ok(notesEntities.Select(EntityToDto));
            }

            var results = new List<NoteDto>();
            foreach (var noteEntity in notesEntities)
            {
                var noteDto = await EntityToDtoWithEntriesAsync(noteEntity, noteEntity.Entries);
                results.Add(noteDto);
            }

            return Ok(results);
        }

        // Paged query
        IQueryable<NoteEntity> pagedQuery = dbContext.Notes;
        
        if (withEntries)
        {
            pagedQuery = pagedQuery.Include(n => n.Entries)
                .ThenInclude(e => e.Rules)
                .Include(n => n.Entries)
                .ThenInclude(e => e.Overrides);
        }

        var totalCount = await pagedQuery.CountAsync();
        var pagedEntities = await pagedQuery
            .OrderByDescending(n => n.UpdatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        if (!withEntries)
        {
            var pagedResults = new PagedResponseDto<NoteDto>
            {
                Data = pagedEntities.Select(EntityToDto),
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page
            };
            return Ok(pagedResults);
        }

        var notesWithEntries = new List<NoteDto>();
        foreach (var noteEntity in pagedEntities)
        {
            var noteDto = await EntityToDtoWithEntriesAsync(noteEntity, noteEntity.Entries);
            notesWithEntries.Add(noteDto);
        }

        var pagedResultsWithEntries = new PagedResponseDto<NoteDto>
        {
            Data = notesWithEntries,
            TotalCount = totalCount,
            PageSize = pageSize,
            CurrentPage = page
        };

        return Ok(pagedResultsWithEntries);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetNoteById(string id)
    {
        var noteEntity = await dbContext.Notes
            .Include(n => n.Entries)
                .ThenInclude(e => e.Rules)
            .Include(n => n.Entries)
                .ThenInclude(e => e.Overrides)
            .FirstOrDefaultAsync(n => n.Id == id);

        if (noteEntity == null)
        {
            return NotFound($"No Notes found with Id: {id}");
        }

        return Ok(await EntityToDtoWithEntriesAsync(noteEntity, noteEntity.Entries.OrderBy(entry => entry.Time).ToList()));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNote(string id, [FromBody] NoteDto updateDto)
    {
        return await UpdateNoteWithEntriesAsync(id, updateDto);
    }

    private async Task<IActionResult> UpdateNoteWithEntriesAsync(string id, NoteDto updateDto)
    {
        // First update the note using the base controller's image handling
        var noteUpdateResult = await UpdateEntityWithImageAsync(id, updateDto, dto => dto.ImageBase64);
        
        if (noteUpdateResult is not NoContentResult)
        {
            return noteUpdateResult; // Return error if note update failed
        }

        // If note update succeeded, update the note entries
        return await UpdateNoteEntriesAsync(id, updateDto);

    }

    private async Task<IActionResult> UpdateNoteEntriesAsync(string id, NoteDto updateDto)
    {
        try
        {
            var existingEntryEntities = await databaseService.GetAllAsync<NoteEntryEntity>(entry => entry.NoteId == id);
            
            foreach (var noteEntryDto in updateDto.Entries)
            {
                var updatedNoteEntryEntity = noteEntryDto.ToEntity(id);

                if (string.IsNullOrEmpty(noteEntryDto.Id))
                {
                    await databaseService.AddAsync(updatedNoteEntryEntity);
                    // Save rules and overrides for new entry using the generated ID
                    var entryDtoWithId = noteEntryDto;
                    entryDtoWithId.Id = updatedNoteEntryEntity.Id; // Get the generated ID
                    await SaveRulesAndOverridesForEntryAsync(entryDtoWithId);
                    continue;
                }

                var existingEntryEntity = existingEntryEntities.FirstOrDefault(existingEntry => existingEntry.Id == noteEntryDto.Id);
                if (existingEntryEntity == null)
                    continue;

                updatedNoteEntryEntity.CreatedDate = existingEntryEntity.CreatedDate;
                updatedNoteEntryEntity.UpdatedDate = DateTime.UtcNow;
                
                await databaseService.UpdateAsync(updatedNoteEntryEntity);
                
                // Update rules and overrides for existing entry
                await UpdateRulesAndOverridesForEntryAsync(noteEntryDto);
                
                existingEntryEntities.Remove(existingEntryEntity);
            }

            // Delete remaining entries and their rules/overrides
            var deletedEntryIds = existingEntryEntities.Select(e => e.Id).ToList();
            await databaseService.DeleteRangeAsync<NoteEntryRuleEntity>(r => deletedEntryIds.Contains(r.NoteEntryId));
            await databaseService.DeleteRangeAsync<NoteEntryOverrideEntity>(o => deletedEntryIds.Contains(o.NoteEntryId));
            await databaseService.DeleteRangeAsync(existingEntryEntities);

            return NoContent();
        }
        catch (Exception ex)
        {
            return UnprocessableEntity($"Failed to update note entries: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNote(string id)
    {
        return await DeleteNoteWithEntriesAsync(id);
    }

    private async Task<IActionResult> DeleteNoteWithEntriesAsync(string id)
    {
        // Get entry IDs first
        var entryIds = await dbContext.NoteEntries
            .Where(e => e.NoteId == id)
            .Select(e => e.Id)
            .ToListAsync();
        
        // Delete rules and overrides for all entries
        await databaseService.DeleteRangeAsync<NoteEntryRuleEntity>(r => entryIds.Contains(r.NoteEntryId));
        await databaseService.DeleteRangeAsync<NoteEntryOverrideEntity>(o => entryIds.Contains(o.NoteEntryId));
        
        // Delete note entries
        await databaseService.DeleteRangeAsync<NoteEntryEntity>(entry => entry.NoteId == id);
        
        // Then delete the note (this will also handle image cleanup via base controller)
        return await DeleteEntityWithImageAsync(id);
    }

    [HttpGet("merge/{compositeId}")]
    public async Task<IActionResult> GetMergedNotes(string compositeId)
    {
        try
        {
            // Decode composite ID into individual note Ids
            var noteIds = DecodeCompositeId(compositeId);
            
            if (noteIds.Count == 0)
            {
                return BadRequest("Invalid composite ID format");
            }

            // Fetch all notes with their entries, rules, and overrides
            var notes = new List<NoteDto>();
            foreach (var noteId in noteIds)
            {
                var noteEntity = await dbContext.Notes
                    .Include(n => n.Entries)
                        .ThenInclude(e => e.Rules)
                    .Include(n => n.Entries)
                        .ThenInclude(e => e.Overrides)
                    .FirstOrDefaultAsync(n => n.Id == noteId);
                if (noteEntity == null) continue;

                var noteDto = await EntityToDtoWithEntriesAsync(noteEntity, noteEntity.Entries.OrderBy(e => e.Time).ToList());
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
        // Composite ID is created by interleaving characters from note Ids
        // Each note Id is 4 characters (NoteEntity has IdLength = 4), so for 2 notes: positions 0,2,4,6 belong to first note, 1,3,5,7 to second
        var noteIds = new List<string>();
        var noteCount = compositeId.Length / 4; // Each note contributes 4 characters
        
        for (int noteIndex = 0; noteIndex < noteCount; noteIndex++)
        {
            var id = "";
            for (int charIndex = 0; charIndex < 4; charIndex++)
            {
                var position = noteIndex + charIndex * noteCount;
                if (position < compositeId.Length)
                {
                    id += compositeId[position];
                }
            }
            if (id.Length == 4)
            {
                noteIds.Add(id);
            }
        }
        
        return noteIds;
    }


}
