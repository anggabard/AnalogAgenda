using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
public class DevKitController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) : BaseEntityController<DevKitEntity, DevKitDto>(storageCfg, tablesService, blobsService)
{
    private readonly TableClient devKitsTable = tablesService.GetTable(TableName.DevKits);
    private readonly BlobContainerClient devKitsContainer = blobsService.GetBlobContainer(ContainerName.devkits);

    protected override TableClient GetTable() => devKitsTable;
    protected override BlobContainerClient GetBlobContainer() => devKitsContainer;
    protected override Guid GetDefaultImageId() => Constants.DefaultDevKitImageId;
    protected override DevKitEntity DtoToEntity(DevKitDto dto) => dto.ToEntity();
    protected override DevKitDto EntityToDto(DevKitEntity entity) => entity.ToDTO(storageCfg.AccountName);

    [HttpPost]
    public async Task<IActionResult> CreateNewKit([FromBody] DevKitDto dto)
    {
        return await CreateEntityWithImageAsync(dto, dto => dto.ImageBase64);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllKits([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        return await GetEntitiesWithBackwardCompatibilityAsync(
            page, 
            pageSize,
            entities => entities.ApplyStandardSorting(),
            sorted => sorted.Select(EntityToDto)
        );
    }

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableKits([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        return await GetFilteredKits(k => !k.Expired, page, pageSize);
    }

    [HttpGet("expired")]
    public async Task<IActionResult> GetExpiredKits([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        return await GetFilteredKits(k => k.Expired, page, pageSize);
    }

    private async Task<IActionResult> GetFilteredKits(
        Expression<Func<DevKitEntity, bool>> predicate, 
        int page = 1, 
        int pageSize = 5)
    {
        return await GetFilteredEntitiesWithBackwardCompatibilityAsync(
            predicate,
            page, 
            pageSize,
            entities => entities.ApplyStandardSorting(),
            sorted => sorted.Select(EntityToDto)
        );
    }

    [HttpGet("{rowKey}")]
    public async Task<IActionResult> GetKitByRowKey(string rowKey)
    {
        return await GetEntityByRowKeyAsync(rowKey);
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateKit(string rowKey, [FromBody] DevKitDto updateDto)
    {
        return await UpdateEntityWithImageAsync(rowKey, updateDto, dto => dto.ImageBase64);
    }

    [HttpDelete("{rowKey}")]
    public async Task<IActionResult> DeleteKit(string rowKey)
    {
        return await DeleteEntityWithImageAsync(rowKey);
    }

}