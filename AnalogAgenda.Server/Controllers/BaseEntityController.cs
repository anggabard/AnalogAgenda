using AnalogAgenda.Server.Helpers;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[ApiController, Authorize]
public abstract class BaseEntityController<TEntity, TDto>(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService) : ControllerBase 
    where TEntity : BaseEntity, IImageEntity
{
    protected readonly Storage storageCfg = storageCfg;
    protected readonly IDatabaseService databaseService = databaseService;
    protected readonly IBlobService blobsService = blobsService;

    protected abstract BlobContainerClient GetBlobContainer();
    protected abstract Guid GetDefaultImageId();
    protected abstract TEntity DtoToEntity(TDto dto);
    protected abstract TDto EntityToDto(TEntity entity);

    protected async Task<IActionResult> CreateEntityWithImageAsync(TDto dto, Func<TDto, string?> getImageBase64, DateTime creationDate = default)
    {
        var imageId = GetDefaultImageId();
        var container = GetBlobContainer();

        try
        {
            var imageBase64 = getImageBase64(dto);
            if (!string.IsNullOrEmpty(imageBase64))
            {
                imageId = Guid.NewGuid();
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(container, imageBase64, imageId);
            }

            var entity = DtoToEntity(dto);
            entity.ImageId = imageId;
            if (creationDate != default)
            {
                entity.CreatedDate = creationDate;
                entity.UpdatedDate = creationDate;
            }

            await databaseService.AddAsync(entity);
            
            // Return the created entity as DTO
            var createdDto = EntityToDto(entity);
            return Created(string.Empty, createdDto);
        }
        catch (Exception ex)
        {
            if (imageId != GetDefaultImageId())
                await container.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();

            return UnprocessableEntity(ex.Message);
        }
    }

    protected async Task<IActionResult> UpdateEntityWithImageAsync(string id, TDto updateDto, Func<TDto, string?> getImageBase64)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        var container = GetBlobContainer();
        
        var existingEntity = await databaseService.GetByIdAsync<TEntity>(id);
        if (existingEntity == null)
            return NotFound();

        var updatedEntity = DtoToEntity(updateDto);
        updatedEntity.Id = id; // Preserve the ID
        updatedEntity.CreatedDate = existingEntity.CreatedDate;

        var imageId = existingEntity.ImageId;
        var imageBase64 = getImageBase64(updateDto);
        if (!string.IsNullOrEmpty(imageBase64))
        {
            if (existingEntity.ImageId != GetDefaultImageId())
            {
                await container.DeleteBlobAsync(existingEntity.ImageId.ToString());
            }

            imageId = Guid.NewGuid();
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(container, imageBase64, imageId);
        }

        updatedEntity.ImageId = imageId;
        updatedEntity.UpdatedDate = DateTime.UtcNow;

        await databaseService.UpdateAsync(updatedEntity);

        return NoContent();
    }

    protected async Task<IActionResult> DeleteEntityWithImageAsync(string id)
    {
        var container = GetBlobContainer();
        
        var existingEntity = await databaseService.GetByIdAsync<TEntity>(id);
        if (existingEntity == null)
            return NotFound();

        if (existingEntity.ImageId != GetDefaultImageId())
            await container.DeleteBlobAsync(existingEntity.ImageId.ToString());

        await databaseService.DeleteAsync(existingEntity);
        return NoContent();
    }

    protected async Task<IActionResult> GetEntityByIdAsync(string id)
    {
        var entity = await databaseService.GetByIdAsync<TEntity>(id);

        if (entity == null)
        {
            return NotFound($"No {typeof(TEntity).Name} found with Id: {id}");
        }

        return Ok(EntityToDto(entity));
    }

    /// <summary>
    /// Get entities with optional pagination. For backward compatibility, if page <= 0, returns all entities.
    /// </summary>
    protected async Task<IActionResult> GetEntitiesWithBackwardCompatibilityAsync<TSorted>(
        int page, 
        int pageSize, 
        Func<IQueryable<TEntity>, IOrderedQueryable<TSorted>> sortFunc,
        Func<IEnumerable<TSorted>, IEnumerable<TDto>> selectFunc) 
        where TSorted : TEntity
    {
        if (page <= 0)
        {
            var entities = await databaseService.GetAllAsync<TEntity>();
            var results = selectFunc(sortFunc(entities.AsQueryable()));
            return Ok(results);
        }

        var pagedEntities = await databaseService.GetPagedAsync<TEntity>(page, pageSize, sortFunc);

        var pagedResults = new PagedResponseDto<TDto>
        {
            Data = selectFunc(pagedEntities.Data.Cast<TSorted>()),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    /// <summary>
    /// Get filtered entities with optional pagination. For backward compatibility, if page <= 0, returns all filtered entities.
    /// </summary>
    protected async Task<IActionResult> GetFilteredEntitiesWithBackwardCompatibilityAsync<TSorted>(
        System.Linq.Expressions.Expression<Func<TEntity, bool>> predicate,
        int page, 
        int pageSize, 
        Func<IQueryable<TEntity>, IOrderedQueryable<TSorted>> sortFunc,
        Func<IEnumerable<TSorted>, IEnumerable<TDto>> selectFunc)
        where TSorted : TEntity
    {
        if (page <= 0)
        {
            var entities = await databaseService.GetAllAsync(predicate);
            var results = selectFunc(sortFunc(entities.AsQueryable()));
            return Ok(results);
        }

        var pagedEntities = await databaseService.GetPagedAsync(predicate, page, pageSize, sortFunc);

        var pagedResults = new PagedResponseDto<TDto>
        {
            Data = selectFunc(pagedEntities.Data.Cast<TSorted>()),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }
}
