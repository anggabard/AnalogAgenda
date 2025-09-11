using Azure.Data.Tables;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Route("[controller]"), Authorize]
public class NotesController(ITableService tablesService) : ControllerBase
{
    private readonly TableClient notesTable = tablesService.GetTable(TableName.Notes);
    private readonly TableClient notesEntriesTable = tablesService.GetTable(TableName.NotesEntries);

    [HttpPost]
    public async Task<IActionResult> CreateNewNote([FromBody] NoteDto dto)
    {
        var entity = dto.ToNoteEntity();
        var entries = dto.ToNoteEntryEntities(entity.RowKey);

        try
        {
            await notesTable.AddEntityAsync(entity);

            foreach (var entry in entries)
            {
                await notesEntriesTable.AddEntityAsync(entry);
            }
        }
        catch (Exception ex)
        {
            if(await tablesService.EntryExistsAsync(entity))
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

    //[HttpGet]
    //public async Task<IActionResult> GetAllKits()
    //{
    //    var entities = await tablesService.GetTableEntries<NotesEntity>();
    //    var results =
    //        entities.Select(entity =>
    //            {
    //                var dto = mapper.Map<NotesDto>(entity);
    //                dto.ImageUrl = BlobUrlHelper.GetUrlFromImageImageInfo(storageCfg.AccountName, ContainerName.Notess.ToString(), entity.ImageId);

    //                return dto;
    //            });

    //    return Ok(results);
    //}

    //[HttpGet("{rowKey}")]
    //public async Task<IActionResult> GetKitByRowKey(string rowKey)
    //{
    //    var entity = await tablesService.GetTableEntryIfExists<NotesEntity>(TableName.Notess.PartitionKey(), rowKey);

    //    if (entity == null)
    //    {
    //        return NotFound($"No Notes found with RowKey: {rowKey}");
    //    }

    //    var dto = mapper.Map<NotesDto>(entity);
    //    dto.ImageUrl = BlobUrlHelper.GetUrlFromImageImageInfo(storageCfg.AccountName, ContainerName.Notess.ToString(), entity.ImageId);
        
    //    return Ok(dto);
    //}

    //[HttpPut("{rowKey}")]
    //public async Task<IActionResult> UpdateProduct(string rowKey, [FromBody] NotesDto updateDto)
    //{
    //    if (updateDto == null)
    //        return BadRequest("Invalid data.");

    //    var existingEntity = await tablesService.GetTableEntryIfExists<NotesEntity>(TableName.Notess.PartitionKey(), rowKey);
    //    if (existingEntity == null)
    //        return NotFound();

    //    var updatedEntity = mapper.Map<NotesEntity>(updateDto);
    //    updatedEntity.CreatedDate = existingEntity.CreatedDate;

    //    var imageId = existingEntity.ImageId;
    //    if (!string.IsNullOrEmpty(updateDto.ImageBase64))
    //    {
    //        if(existingEntity.ImageId != Constants.DefaultNotesImageId)
    //        {
    //            await NotessContainer.DeleteBlobAsync(existingEntity.ImageId.ToString());
    //        }

    //        imageId = Guid.NewGuid();
    //        await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(NotessContainer, updateDto.ImageBase64, imageId);
    //    }

    //    updatedEntity.ImageId = imageId;
    //    updatedEntity.UpdatedDate = DateTime.UtcNow;

    //    await NotessTable.UpdateEntityAsync(updatedEntity, existingEntity.ETag, TableUpdateMode.Replace);

    //    return NoContent();
    //}
}