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
    public async Task<IActionResult> GetAllKits([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all kits
        if (page <= 0)
        {
            var entities = await tablesService.GetTableEntriesAsync<DevKitEntity>();
            var results = entities.Select(entity => entity.ToDTO(storageCfg.AccountName));
            return Ok(results);
        }

        var pagedEntities = await tablesService.GetTableEntriesPagedAsync<DevKitEntity>(page, pageSize);
        var pagedResults = new PagedResponseDto<DevKitDto>
        {
            Data = pagedEntities.Data.Select(entity => entity.ToDTO(storageCfg.AccountName)),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableKits([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all available kits
        if (page <= 0)
        {
            var entities = await tablesService.GetTableEntriesAsync<DevKitEntity>(k => !k.Expired);
            var results = entities
                .OrderBy(k => k.PurchasedOn) // Sort by purchased date (oldest first)
                .Select(entity => entity.ToDTO(storageCfg.AccountName));
            return Ok(results);
        }

        var pagedEntities = await tablesService.GetTableEntriesPagedAsync<DevKitEntity>(k => !k.Expired, page, pageSize);
        var sortedData = pagedEntities.Data
            .OrderBy(k => k.PurchasedOn) // Sort by purchased date (oldest first)
            .ToList();

        var pagedResults = new PagedResponseDto<DevKitDto>
        {
            Data = sortedData.Select(entity => entity.ToDTO(storageCfg.AccountName)),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("expired")]
    public async Task<IActionResult> GetExpiredKits([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all expired kits
        if (page <= 0)
        {
            var entities = await tablesService.GetTableEntriesAsync<DevKitEntity>(k => k.Expired);
            var results = entities
                .OrderBy(k => k.PurchasedOn) // Sort by purchased date (oldest first)
                .Select(entity => entity.ToDTO(storageCfg.AccountName));
            return Ok(results);
        }

        var pagedEntities = await tablesService.GetTableEntriesPagedAsync<DevKitEntity>(k => k.Expired, page, pageSize);
        var sortedData = pagedEntities.Data
            .OrderBy(k => k.PurchasedOn) // Sort by purchased date (oldest first)
            .ToList();

        var pagedResults = new PagedResponseDto<DevKitDto>
        {
            Data = sortedData.Select(entity => entity.ToDTO(storageCfg.AccountName)),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("{rowKey}")]
    public async Task<IActionResult> GetKitByRowKey(string rowKey)
    {
        var entity = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(rowKey);

        if (entity == null)
        {
            return NotFound($"No DevKit found with RowKey: {rowKey}");
        }

        return Ok(entity.ToDTO(storageCfg.AccountName));
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateKit(string rowKey, [FromBody] DevKitDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        var existingEntity = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(rowKey);
        if (existingEntity == null)
            return NotFound();

        var updatedEntity = updateDto.ToEntity();
        updatedEntity.CreatedDate = existingEntity.CreatedDate;

        var imageId = existingEntity.ImageId;
        if (!string.IsNullOrEmpty(updateDto.ImageBase64))
        {
            if (existingEntity.ImageId != Constants.DefaultDevKitImageId)
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

    [HttpDelete("{rowKey}")]
    public async Task<IActionResult> DeleteKit(string rowKey)
    {
        var existingEntity = await tablesService.GetTableEntryIfExistsAsync<DevKitEntity>(rowKey);
        if (existingEntity == null)
            return NotFound();

        if (existingEntity.ImageId != Constants.DefaultDevKitImageId)
            await devKitsContainer.DeleteBlobAsync(existingEntity.ImageId.ToString());

        await devKitsTable.DeleteEntityAsync(existingEntity);
        return NoContent();
    }

}