using Azure.Storage.Blobs;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]"), ApiController, Authorize]
public class DevKitController(IDatabaseService databaseService, IBlobService blobsService, DtoConvertor dtoConvertor, EntityConvertor entityConvertor) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly DtoConvertor dtoConvertor = dtoConvertor;
    private readonly EntityConvertor entityConvertor = entityConvertor;
    private readonly BlobContainerClient devKitsContainer = blobsService.GetBlobContainer(ContainerName.devkits);

    [HttpPost]
    public async Task<IActionResult> CreateNewKit([FromBody] DevKitDto dto)
    {
        try
        {
            var entity = entityConvertor.ToEntity(dto);
            
            // If no ImageUrl provided, use default image
            if (entity.ImageId == Guid.Empty)
            {
                entity.ImageId = Constants.DefaultDevKitImageId;
            }
            
            await databaseService.AddAsync(entity);
            
            var createdDto = dtoConvertor.ToDTO(entity);
            return Created(string.Empty, createdDto);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllKits([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        if (page <= 0)
        {
            var entities = await databaseService.GetAllAsync<DevKitEntity>();
            var results = entities.ApplyStandardSorting().Select(dtoConvertor.ToDTO);
            return Ok(results);
        }

        var pagedEntities = await databaseService.GetPagedAsync<DevKitEntity>(page, pageSize, entities => entities.ApplyStandardSorting());
        var pagedResults = new PagedResponseDto<DevKitDto>
        {
            Data = pagedEntities.Data.Select(dtoConvertor.ToDTO),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
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
        if (page <= 0)
        {
            var entities = await databaseService.GetAllAsync(predicate);
            var results = entities.ApplyStandardSorting().Select(dtoConvertor.ToDTO);
            return Ok(results);
        }

        var pagedEntities = await databaseService.GetPagedAsync(predicate, page, pageSize, entities => entities.ApplyStandardSorting());
        var pagedResults = new PagedResponseDto<DevKitDto>
        {
            Data = pagedEntities.Data.Select(dtoConvertor.ToDTO),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetKitById(string id)
    {
        var entity = await databaseService.GetByIdAsync<DevKitEntity>(id);
        if (entity == null)
            return NotFound($"No DevKit found with Id: {id}");
        
        return Ok(dtoConvertor.ToDTO(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateKit(string id, [FromBody] DevKitDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        // Load existing entity
        var existingEntity = await databaseService.GetByIdAsync<DevKitEntity>(id);
        
        if (existingEntity == null)
            return NotFound();

        try
        {
            // Update entity using the Update method
            existingEntity.Update(updateDto);
            
            // UpdateAsync will handle UpdatedDate
            await databaseService.UpdateAsync(existingEntity);
            return NoContent();
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteKit(string id)
    {
        var entity = await databaseService.GetByIdAsync<DevKitEntity>(id);
        if (entity == null)
            return NotFound();
        
        // Delete image blob if not default
        if (entity.ImageId != Constants.DefaultDevKitImageId)
            await devKitsContainer.DeleteBlobAsync(entity.ImageId.ToString());
        
        await databaseService.DeleteAsync(entity);
        return NoContent();
    }

}