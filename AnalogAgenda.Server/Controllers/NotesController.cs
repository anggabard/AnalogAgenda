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
    
    private NoteDto EntityToDtoWithEntries(NoteEntity entity, IEnumerable<NoteEntryEntity> entries) => 
        entity.ToDTO(storageCfg.AccountName, [.. entries]);

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
            
            return Created(string.Empty, createdNote);
        }
        catch (Exception ex)
        {
            // If entries creation failed, clean up the note we just created
            await DeleteNoteAndCleanupAsync(createdNote.Id);
            return UnprocessableEntity($"Failed to create note entries: {ex.Message}");
        }
    }

    private async Task DeleteNoteAndCleanupAsync(string noteId)
    {
        try
        {
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
                query = query.Include(n => n.Entries);
            }

            var notesEntities = await query.ToListAsync();

            if (!withEntries)
            {
                return Ok(notesEntities.Select(EntityToDto));
            }

            var results = notesEntities.Select(noteEntity => EntityToDtoWithEntries(noteEntity, noteEntity.Entries));

            return Ok(results);
        }

        // Paged query
        IQueryable<NoteEntity> pagedQuery = dbContext.Notes;
        
        if (withEntries)
        {
            pagedQuery = pagedQuery.Include(n => n.Entries);
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

        var notesWithEntries = pagedEntities.Select(noteEntity => EntityToDtoWithEntries(noteEntity, noteEntity.Entries));

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
            .FirstOrDefaultAsync(n => n.Id == id);

        if (noteEntity == null)
        {
            return NotFound($"No Notes found with Id: {id}");
        }

        return Ok(EntityToDtoWithEntries(noteEntity, noteEntity.Entries.OrderBy(entry => entry.Time).ToList()));
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
                    continue;
                }

                var existingEntryEntity = existingEntryEntities.FirstOrDefault(existingEntry => existingEntry.Id == noteEntryDto.Id);
                if (existingEntryEntity == null)
                    continue;

                updatedNoteEntryEntity.CreatedDate = existingEntryEntity.CreatedDate;
                updatedNoteEntryEntity.UpdatedDate = DateTime.UtcNow;
                
                await databaseService.UpdateAsync(updatedNoteEntryEntity);
                existingEntryEntities.Remove(existingEntryEntity);
            }

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
        // First delete note entries
        await databaseService.DeleteRangeAsync<NoteEntryEntity>(entry => entry.NoteId == id);
        
        // Then delete the note (this will also handle image cleanup via base controller)
        return await DeleteEntityWithImageAsync(id);
    }

}