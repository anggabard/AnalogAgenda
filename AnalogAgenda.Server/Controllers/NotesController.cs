using Database.Helpers;
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

[Route("api/[controller]"), ApiController, Authorize]
public class NotesController(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService) : ControllerBase
{
    private readonly Storage storageCfg = storageCfg;
    private readonly IDatabaseService databaseService = databaseService;
    private readonly IBlobService blobsService = blobsService;
    private readonly BlobContainerClient notesContainer = blobsService.GetBlobContainer(ContainerName.notes);
    
    private async Task<NoteDto> EntityToDtoWithEntriesAsync(NoteEntity entity, IEnumerable<NoteEntryEntity> entries)
    {
        var entryList = entries.ToList();
        var entryIds = entryList.Select(e => e.Id).ToList();
        
        // Get rules and overrides for all entries
        var rules = await databaseService.GetAllAsync<NoteEntryRuleEntity>(r => entryIds.Contains(r.NoteEntryId));
        var overrides = await databaseService.GetAllAsync<NoteEntryOverrideEntity>(o => entryIds.Contains(o.NoteEntryId));
        
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
        var imageId = Constants.DefaultNoteImageId;

        try
        {
            var imageBase64 = dto.ImageBase64;
            if (!string.IsNullOrEmpty(imageBase64))
            {
                imageId = Guid.NewGuid();
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(notesContainer, imageBase64, imageId);
            }

            var entity = dto.ToNoteEntity();
            entity.ImageId = imageId;

            await databaseService.AddAsync(entity);

            // Return the created entity as DTO
            var createdNote = entity.ToDTO(storageCfg.AccountName);

            var entries = dto.ToNoteEntryEntities(createdNote.Id);

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
            if (imageId != Constants.DefaultNoteImageId)
                await notesContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();

            return UnprocessableEntity($"Failed to create note: {ex.Message}");
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

    [HttpGet]
    public async Task<IActionResult> GetAllNotes([FromQuery] bool withEntries = false, [FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all notes
        if (page <= 0)
        {
            var notesEntities = await databaseService.GetAllAsync<NoteEntity>();

            if (!withEntries)
            {
                return Ok(notesEntities.Select(e => e.ToDTO(storageCfg.AccountName)));
            }

            var results = new List<NoteDto>();
            foreach (var noteEntity in notesEntities)
            {
                // Load entries for this note
                var entries = await databaseService.GetAllAsync<NoteEntryEntity>(e => e.NoteId == noteEntity.Id);
                var noteDto = await EntityToDtoWithEntriesAsync(noteEntity, entries);
                results.Add(noteDto);
            }

            return Ok(results);
        }

        // Paged query
        var pagedEntities = await databaseService.GetPagedAsync<NoteEntity>(page, pageSize);
        
        if (!withEntries)
        {
            var pagedResults = new PagedResponseDto<NoteDto>
            {
                Data = pagedEntities.Data.Select(e => e.ToDTO(storageCfg.AccountName)),
                TotalCount = pagedEntities.TotalCount,
                PageSize = pagedEntities.PageSize,
                CurrentPage = pagedEntities.CurrentPage
            };
            return Ok(pagedResults);
        }

        var notesWithEntries = new List<NoteDto>();
        foreach (var noteEntity in pagedEntities.Data)
        {
            // Load entries for this note
            var entries = await databaseService.GetAllAsync<NoteEntryEntity>(e => e.NoteId == noteEntity.Id);
            var noteDto = await EntityToDtoWithEntriesAsync(noteEntity, entries);
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetNoteById(string id)
    {
        var noteEntity = await databaseService.GetByIdAsync<NoteEntity>(id);

        if (noteEntity == null)
        {
            return NotFound($"No Notes found with Id: {id}");
        }

        // Load entries for this note
        var entries = await databaseService.GetAllAsync<NoteEntryEntity>(e => e.NoteId == id);
        return Ok(await EntityToDtoWithEntriesAsync(noteEntity, entries.OrderBy(entry => entry.Time).ToList()));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateNote(string id, [FromBody] NoteDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        // Load existing entity
        var existingEntity = await databaseService.GetByIdAsync<NoteEntity>(id);

        if (existingEntity == null)
            return NotFound();

        // Handle image update if provided
        var imageBase64 = updateDto.ImageBase64;
        if (!string.IsNullOrEmpty(imageBase64))
        {
            if (existingEntity.ImageId != Constants.DefaultNoteImageId)
            {
                await notesContainer.DeleteBlobAsync(existingEntity.ImageId.ToString());
            }

            var newImageId = Guid.NewGuid();
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(notesContainer, imageBase64, newImageId);
            existingEntity.ImageId = newImageId;
        }

        // Update entity using the Update method
        existingEntity.Update(updateDto);

        // UpdateAsync will handle UpdatedDate
        await databaseService.UpdateAsync(existingEntity);

        // If note update succeeded, update the note entries
        return await UpdateNoteEntriesAsync(id, updateDto);
    }

    private async Task<IActionResult> UpdateNoteEntriesAsync(string id, NoteDto updateDto)
    {
        try
        {
            // Load existing entries
            var existingEntryEntities = await databaseService.GetAllAsync<NoteEntryEntity>(entry => entry.NoteId == id);
            
            foreach (var noteEntryDto in updateDto.Entries)
            {
                // Handle new entries
                if (string.IsNullOrEmpty(noteEntryDto.Id))
                {
                    var newEntry = noteEntryDto.ToEntity(id);
                    await databaseService.AddAsync(newEntry);
                    // Save rules and overrides for new entry using the generated ID
                    var entryDtoWithId = noteEntryDto;
                    entryDtoWithId.Id = newEntry.Id; // Get the generated ID
                    await SaveRulesAndOverridesForEntryAsync(entryDtoWithId);
                    continue;
                }

                // Handle existing entries
                var existingEntryEntity = existingEntryEntities.FirstOrDefault(existingEntry => existingEntry.Id == noteEntryDto.Id);
                if (existingEntryEntity == null)
                    continue;

                // Update entity using the Update method
                existingEntryEntity.Update(noteEntryDto);
                
                // UpdateAsync will handle UpdatedDate automatically
                await databaseService.UpdateAsync(existingEntryEntity);
                
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
        var entries = await databaseService.GetAllAsync<NoteEntryEntity>(e => e.NoteId == id);
        var entryIds = entries.Select(e => e.Id).ToList();
        
        // Delete rules and overrides for all entries
        await databaseService.DeleteRangeAsync<NoteEntryRuleEntity>(r => entryIds.Contains(r.NoteEntryId));
        await databaseService.DeleteRangeAsync<NoteEntryOverrideEntity>(o => entryIds.Contains(o.NoteEntryId));
        
        // Delete note entries
        await databaseService.DeleteRangeAsync<NoteEntryEntity>(entry => entry.NoteId == id);
        
        // Then delete the note with image cleanup
        var entity = await databaseService.GetByIdAsync<NoteEntity>(id);
        if (entity == null)
            return NotFound();
        
        // Delete image blob if not default
        if (entity.ImageId != Constants.DefaultNoteImageId)
            await notesContainer.DeleteBlobAsync(entity.ImageId.ToString());
        
        await databaseService.DeleteAsync(entity);
        return NoContent();
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
                var noteEntity = await databaseService.GetByIdAsync<NoteEntity>(noteId);
                if (noteEntity == null) continue;

                // Load entries for this note
                var entries = await databaseService.GetAllAsync<NoteEntryEntity>(e => e.NoteId == noteId);
                var noteDto = await EntityToDtoWithEntriesAsync(noteEntity, entries.OrderBy(e => e.Time).ToList());
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
