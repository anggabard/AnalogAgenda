using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.Data;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
public class DevKitController(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService, AnalogAgendaDbContext dbContext) : BaseEntityController<DevKitEntity, DevKitDto>(storageCfg, databaseService, blobsService, dbContext)
{
    private readonly BlobContainerClient devKitsContainer = blobsService.GetBlobContainer(ContainerName.devkits);

    protected override BlobContainerClient GetBlobContainer() => devKitsContainer;
    protected override Guid GetDefaultImageId() => Constants.DefaultDevKitImageId;
    protected override DevKitEntity DtoToEntity(DevKitDto dto) => dto.ToEntity();
    protected override DevKitDto EntityToDto(DevKitEntity entity) => entity.ToDTO(storageCfg.AccountName);

    [HttpPost]
    public async Task<IActionResult> CreateNewKit([FromBody] DevKitDto dto)
    {
        try
        {
            var entity = dto.ToEntity();
            
            // If no ImageUrl provided, use default image
            if (entity.ImageId == Guid.Empty)
            {
                entity.ImageId = Constants.DefaultDevKitImageId;
            }
            
            await databaseService.AddAsync(entity);
            
            var createdDto = entity.ToDTO(storageCfg.AccountName);
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

    [HttpGet("{id}")]
    public async Task<IActionResult> GetKitById(string id)
    {
        return await GetEntityByIdAsync(id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateKit(string id, [FromBody] DevKitDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        // Load entity without tracking to avoid conflicts with navigation properties
        var existingEntity = await dbContext.Set<DevKitEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);
        
        if (existingEntity == null)
            return NotFound();

        try
        {
            // Create entity from DTO using ToEntity() method
            var updatedEntity = updateDto.ToEntity();
            updatedEntity.Id = id; // Preserve the ID
            updatedEntity.CreatedDate = existingEntity.CreatedDate; // Preserve CreatedDate
            updatedEntity.UpdatedDate = DateTime.UtcNow;
            
            // If no ImageUrl provided, keep existing ImageId
            if (updatedEntity.ImageId == Guid.Empty)
            {
                updatedEntity.ImageId = existingEntity.ImageId;
            }
            
            // Attach and update the entity using Entry API
            // This avoids tracking conflicts with navigation properties
            dbContext.Set<DevKitEntity>().Attach(updatedEntity);
            dbContext.Entry(updatedEntity).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            
            // Clear navigation properties to avoid tracking conflicts
            dbContext.Entry(updatedEntity).Collection(d => d.DevelopedFilms).IsLoaded = false;
            dbContext.Entry(updatedEntity).Collection(d => d.UsedInSessions).IsLoaded = false;
            
            await dbContext.SaveChangesAsync();
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
        return await DeleteEntityWithImageAsync(id);
    }

}