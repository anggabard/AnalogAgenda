using AnalogAgenda.Server.Helpers;
using AutoMapper;
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
public class DevKitController(IMapper mapper, Storage storageCfg, ITableService tablesService, IBlobService blobsService) : ControllerBase
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

            var entity = mapper.Map<DevKitEntity>(dto);
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
        var entities = await tablesService.GetTableEntries<DevKitEntity>();
        var results =
            entities.Select(entity =>
                {
                    var dto = mapper.Map<DevKitDto>(entity);
                    dto.ImageUrl = BlobUrlHelper.GetUrlFromImageImageInfo(storageCfg.AccountName, ContainerName.devkits.ToString(), entity.ImageId);

                    return dto;
                });

        return Ok(results);
    }

    [HttpGet("{rowKey}")]
    public async Task<IActionResult> GetKitByRowKey(string rowKey)
    {
        var entity = await tablesService.GetTableEntry<DevKitEntity>(TableName.DevKits.PartitionKey(), rowKey);

        if (entity == null)
        {
            return NotFound($"No DevKit found with RowKey: {rowKey}");
        }

        var dto = mapper.Map<DevKitDto>(entity);
        dto.ImageUrl = BlobUrlHelper.GetUrlFromImageImageInfo(storageCfg.AccountName, ContainerName.devkits.ToString(), entity.ImageId);
        
        return Ok(dto);
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateProduct(string rowKey, [FromBody] DevKitDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        var existingEntity = await tablesService.GetTableEntry<DevKitEntity>(TableName.DevKits.PartitionKey(), rowKey);
        if (existingEntity == null)
            return NotFound();

        var updatedEntity = mapper.Map<DevKitEntity>(updateDto);

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


        var devKitsTable = tablesService.GetTable(TableName.DevKits);
        await devKitsTable.DeleteEntityAsync(existingEntity.PartitionKey, existingEntity.RowKey);
        await devKitsTable.AddEntityAsync(updatedEntity);

        return NoContent();
    }
}