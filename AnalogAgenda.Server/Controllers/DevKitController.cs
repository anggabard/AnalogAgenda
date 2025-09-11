using AnalogAgenda.Server.Helpers;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Route("[controller]"), Authorize]
public class DevKitController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) : ControllerBase
{
    private readonly TableClient devKitsTable = tablesService.GetTable(TableName.DevKits);
    private readonly BlobContainerClient devKitsContainer = blobsService.GetBlobContainer(ContainerName.devkits);

    [HttpPost]
    public async Task<IActionResult> CreateNewKit([FromBody] DevKitDto dto)
    {
        var imageId = Constants.DefaultDevKitImageId;

        try
        {
            if (!string.IsNullOrEmpty(dto.ImageBase64))
            {
                imageId = Guid.NewGuid();
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(devKitsContainer, dto.ImageBase64, imageId);
            }

            var entity = dto.ToEntity();
            entity.ImageId = imageId;


            await devKitsTable.AddEntityAsync(entity);
        }
        catch (Exception ex)
        {
            if (imageId != Constants.DefaultDevKitImageId)
                await devKitsContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();

            return UnprocessableEntity(ex.Message);
        }

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllKits()
    {
        var entities = await tablesService.GetTableEntriesAsync<DevKitEntity>();
        var results = entities.Select(entity => entity.ToDTO(storageCfg.AccountName));

        return Ok(results);
    }

    [HttpGet("{rowKey}")]
    public async Task<IActionResult> GetKitByRowKey(string rowKey)
    {
        var entity = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(TableName.DevKits.PartitionKey(), rowKey);

        if (entity == null)
        {
            return NotFound($"No DevKit found with RowKey: {rowKey}");
        }

        return Ok(entity.ToDTO(storageCfg.AccountName));
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateProduct(string rowKey, [FromBody] DevKitDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        var existingEntity = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(TableName.DevKits.PartitionKey(), rowKey);
        if (existingEntity == null)
            return NotFound();

        var updatedEntity = updateDto.ToEntity();
        updatedEntity.CreatedDate = existingEntity.CreatedDate;

        var imageId = existingEntity.ImageId;
        if (!string.IsNullOrEmpty(updateDto.ImageBase64))
        {
            if(existingEntity.ImageId != Constants.DefaultDevKitImageId)
            {
                await devKitsContainer.DeleteBlobAsync(existingEntity.ImageId.ToString());
            }

            imageId = Guid.NewGuid();
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(devKitsContainer, updateDto.ImageBase64, imageId);
        }

        updatedEntity.ImageId = imageId;
        updatedEntity.UpdatedDate = DateTime.UtcNow;

        await devKitsTable.UpdateEntityAsync(updatedEntity, existingEntity.ETag, TableUpdateMode.Replace);

        return NoContent();
    }
}