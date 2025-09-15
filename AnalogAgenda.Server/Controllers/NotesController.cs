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

[ApiController, Route("api/[controller]"), Authorize]
public class NotesController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) : ControllerBase
{
    private readonly TableClient notesTable = tablesService.GetTable(TableName.Notes);
    private readonly TableClient notesEntriesTable = tablesService.GetTable(TableName.NotesEntries);
    private readonly BlobContainerClient notesContainer = blobsService.GetBlobContainer(ContainerName.notes);

    [HttpPost]
    public async Task<IActionResult> CreateNewNote([FromBody] NoteDto dto)
    {
        var entity = dto.ToNoteEntity();
        var entries = dto.ToNoteEntryEntities(entity.RowKey);

        var imageId = Constants.DefaultNoteImageId;
        try
        {
            if (!string.IsNullOrEmpty(dto.ImageBase64))
            {
                imageId = Guid.NewGuid();
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(notesContainer, dto.ImageBase64, imageId);
            }

            entity.ImageId = imageId;
            await notesTable.AddEntityAsync(entity);

            foreach (var entry in entries)
            {
                await notesEntriesTable.AddEntityAsync(entry);
            }
        }
        catch (Exception ex)
        {
            if (imageId != Constants.DefaultNoteImageId)
                await notesContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();

            if (await tablesService.EntryExistsAsync(entity))
                await notesTable.DeleteEntityAsync(entity);

            foreach (var entry in entries)
            {
                if (await tablesService.EntryExistsAsync(entry))
                    await notesEntriesTable.DeleteEntityAsync(entry);
            }

            return UnprocessableEntity(ex.Message);
        }

        return Ok(entity.RowKey);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllNotes([FromQuery] bool withEntries = false)
    {
        var result = new List<NoteDto>();
        var notesEntities = await tablesService.GetTableEntriesAsync<NoteEntity>();

        if (!withEntries)
        {
            return Ok(notesEntities.Select(note => note.ToDTO(storageCfg.AccountName)));
        }

        var results = await Task.WhenAll(
            notesEntities.Select(async noteEntity =>
                {
                    var entryEntities = await tablesService.GetTableEntriesAsync<NoteEntryEntity>(entry => entry.NoteRowKey == noteEntity.RowKey);

                    return noteEntity.ToDTO(storageCfg.AccountName, entryEntities);
                }));

        return Ok(results);
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

        return Ok(noteEntity.ToDTO(storageCfg.AccountName, entryEntities.OrderBy(entry => entry.Time).ToList()));
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateNote(string rowKey, [FromBody] NoteDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        var existingEntity = await tablesService.GetTableEntryIfExistsAsync<NoteEntity>(rowKey);
        if (existingEntity == null)
            return NotFound();

        var updatedNoteEntity = updateDto.ToNoteEntity();
        updatedNoteEntity.CreatedDate = existingEntity.CreatedDate;

        var imageId = existingEntity.ImageId;
        if (!string.IsNullOrEmpty(updateDto.ImageBase64))
        {
            if (existingEntity.ImageId != Constants.DefaultNoteImageId)
            {
                await notesContainer.DeleteBlobAsync(existingEntity.ImageId.ToString());
            }

            imageId = Guid.NewGuid();
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(notesContainer, updateDto.ImageBase64, imageId);
        }

        updatedNoteEntity.ImageId = imageId;
        updatedNoteEntity.UpdatedDate = DateTime.UtcNow;

        await notesTable.UpdateEntityAsync(updatedNoteEntity, existingEntity.ETag, TableUpdateMode.Replace);

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

    [HttpDelete("{rowKey}")]
    public async Task<IActionResult> DeleteNote(string rowKey)
    {
        var existingEntity = await tablesService.GetTableEntryIfExistsAsync<NoteEntity>(rowKey);
        if (existingEntity == null)
            return NotFound();

        if (existingEntity.ImageId != Constants.DefaultNoteImageId)
            await notesContainer.DeleteBlobAsync(existingEntity.ImageId.ToString());

        await tablesService.DeleteTableEntryAsync<NoteEntity>(existingEntity.RowKey);
        await tablesService.DeleteTableEntriesAsync<NoteEntryEntity>(entry => entry.NoteRowKey == existingEntity.RowKey);
        return NoContent();
    }
}