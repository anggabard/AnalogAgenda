using AnalogAgenda.Server.Identity;
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
public class FilmController(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService) : BaseEntityController<FilmEntity, FilmDto>(storageCfg, databaseService, blobsService)
{
    private readonly BlobContainerClient filmsContainer = blobsService.GetBlobContainer(ContainerName.films);

    protected override BlobContainerClient GetBlobContainer() => filmsContainer;
    protected override Guid GetDefaultImageId() => Constants.DefaultFilmImageId;
    protected override FilmEntity DtoToEntity(FilmDto dto) => dto.ToEntity();
    protected override FilmDto EntityToDto(FilmEntity entity) => entity.ToDTO(storageCfg.AccountName);

    [HttpPost]
    public async Task<IActionResult> CreateNewFilm([FromBody] FilmDto dto, [FromQuery] int bulkCount = 1)
    {
        try
        {
            // Validate bulkCount
            if (bulkCount < 1 || bulkCount > 10)
            {
                return BadRequest("bulkCount must be between 1 and 10");
            }

            var createdDtos = new List<FilmDto>();

            for (int i = 0; i < bulkCount; i++)
            {
                // Create entity directly from the original DTO
                var entity = dto.ToEntity();
                
                // Add milliseconds offset to ensure unique dates
                entity.CreatedDate = entity.CreatedDate.AddMilliseconds(i);
                entity.UpdatedDate = entity.UpdatedDate.AddMilliseconds(i);
                
                // If no ImageUrl provided, use default image
                if (entity.ImageId == Guid.Empty)
                {
                    entity.ImageId = Constants.DefaultFilmImageId;
                }
                
                await databaseService.AddAsync(entity);
                
                var createdDto = entity.ToDTO(storageCfg.AccountName);
                createdDtos.Add(createdDto);
            }
            
            // Return the first created film DTO for consistency with single creation
            return Created(string.Empty, createdDtos.First());
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAllFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all films
        if (page <= 0)
        {
            var entities = await databaseService.GetAllAsync<FilmEntity>();
            var results = entities.Select(EntityToDto);
            return Ok(results);
        }

        var pagedEntities = await databaseService.GetPagedAsync<FilmEntity>(page, pageSize, entities => entities.ApplyStandardSorting());
        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = pagedEntities.Data.Select(EntityToDto),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("developed")]
    public async Task<IActionResult> GetDevelopedFilms([FromQuery] FilmSearchDto searchDto)
    {
        return await GetFilteredFilms(f => f.Developed, searchDto);
    }

    [HttpGet("my/developed")]
    public async Task<IActionResult> GetMyDevelopedFilms([FromQuery] FilmSearchDto searchDto)
    {
        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();

        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        // Azure Table Storage may have issues with compound expressions, so filter by user first
        return await GetMyFilteredFilms(f => f.Developed, currentUserEnum, searchDto);
    }

    [HttpGet("not-developed")]
    public async Task<IActionResult> GetNotDevelopedFilms([FromQuery] FilmSearchDto searchDto)
    {
        return await GetFilteredFilms(f => !f.Developed, searchDto);
    }

    [HttpGet("my/not-developed")]
    public async Task<IActionResult> GetMyNotDevelopedFilms([FromQuery] FilmSearchDto searchDto)
    {
        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();

        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        // Azure Table Storage may have issues with compound expressions, so filter by user first
        return await GetMyFilteredFilms(f => !f.Developed, currentUserEnum, searchDto);
    }

    private async Task<IActionResult> GetFilteredFilms(
        Expression<Func<FilmEntity, bool>> predicate,
        FilmSearchDto searchDto)
    {
        // For backward compatibility, if page is 0 or negative, return all filtered films
        if (searchDto.Page <= 0)
        {
            var entities = await databaseService.GetAllAsync(predicate);
            var filteredEntities = ApplySearchFilters(entities, searchDto);
            var results = filteredEntities
                .ApplyStandardSorting()
                .Select(EntityToDto);
            return Ok(results);
        }

        var allEntities = await databaseService.GetAllAsync(predicate);
        var searchFilteredEntities = ApplySearchFilters(allEntities, searchDto);
        var sortedEntities = searchFilteredEntities.ApplyStandardSorting().ToList();
        
        // Apply pagination
        var totalCount = sortedEntities.Count;
        var skip = (searchDto.Page - 1) * searchDto.PageSize;
        var pagedData = sortedEntities.Skip(skip).Take(searchDto.PageSize).ToList();

        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = pagedData.Select(EntityToDto),
            TotalCount = totalCount,
            PageSize = searchDto.PageSize,
            CurrentPage = searchDto.Page
        };

        return Ok(pagedResults);
    }

    private async Task<IActionResult> GetMyFilteredFilms(
        Expression<Func<FilmEntity, bool>> statusPredicate,
        EUsernameType currentUserEnum,
        FilmSearchDto searchDto)
    {
        if (searchDto.Page <= 0)
        {
            // Get all entities with status filter, then filter by user in memory
            var allEntities = await databaseService.GetAllAsync(statusPredicate);
            var myEntities = allEntities
                .Where(f => f.PurchasedBy == currentUserEnum);
            var filteredEntities = ApplySearchFilters(myEntities, searchDto);
            var results = filteredEntities
                .ApplyUserFilteredSorting()
                .Select(EntityToDto);
            return Ok(results);
        }

        // For pagination, we need to get all status-filtered entities and then page them
        var statusFilteredEntities = await databaseService.GetAllAsync(statusPredicate);
        var myFilms = statusFilteredEntities
            .Where(f => f.PurchasedBy == currentUserEnum);
        var filteredFilms = ApplySearchFilters(myFilms, searchDto);
        var sortedFilms = filteredFilms.ApplyUserFilteredSorting().ToList();

        var totalCount = sortedFilms.Count;
        var skip = (searchDto.Page - 1) * searchDto.PageSize;
        var pagedData = sortedFilms
            .Skip(skip)
            .Take(searchDto.PageSize)
            .ToList();

        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = pagedData.Select(EntityToDto),
            TotalCount = totalCount,
            PageSize = searchDto.PageSize,
            CurrentPage = searchDto.Page
        };

        return Ok(pagedResults);
    }


    [HttpGet("{id}")]
    public async Task<IActionResult> GetFilmById(string id)
    {
        return await GetEntityByIdAsync(id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFilm(string id, [FromBody] FilmDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        var existingEntity = await databaseService.GetByIdAsync<FilmEntity>(id);
        if (existingEntity == null)
            return NotFound();

        try
        {
            var updatedEntity = updateDto.ToEntity();
            updatedEntity.Id = id; // Preserve the ID
            updatedEntity.CreatedDate = existingEntity.CreatedDate;
            updatedEntity.UpdatedDate = DateTime.UtcNow;
            
            // If no ImageUrl provided, keep existing ImageId
            if (updatedEntity.ImageId == Guid.Empty)
            {
                updatedEntity.ImageId = existingEntity.ImageId;
            }
            
            await databaseService.UpdateAsync(updatedEntity);
            return NoContent();
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFilm(string id)
    {
        // First, delete all associated photos
        await DeleteAssociatedPhotosAsync(id);
        
        // Then delete the film itself
        return await DeleteEntityWithImageAsync(id);
    }

    private async Task DeleteAssociatedPhotosAsync(string filmId)
    {
        var photosContainer = blobsService.GetBlobContainer(ContainerName.photos);
        
        var photos = await databaseService.GetAllAsync<PhotoEntity>(p => p.FilmId == filmId);
        
        foreach (var photo in photos)
        {
            // Delete the blob
            if (photo.ImageId != Guid.Empty)
            {
                await photosContainer.GetBlobClient(photo.ImageId.ToString()).DeleteIfExistsAsync();
            }
            
            // Delete the table entry
            await databaseService.DeleteAsync(photo);
        }
    }


    private IEnumerable<FilmEntity> ApplySearchFilters(IEnumerable<FilmEntity> films, FilmSearchDto searchDto)
    {
        var filteredFilms = films;

        // Apply each non-null/non-empty filter with AND logic
        if (!string.IsNullOrEmpty(searchDto.Name))
        {
            filteredFilms = filteredFilms.Where(f => f.Name.Contains(searchDto.Name, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(searchDto.Id))
        {
            filteredFilms = filteredFilms.Where(f => f.Id.Contains(searchDto.Id, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(searchDto.Iso))
        {
            filteredFilms = filteredFilms.Where(f => f.Iso.Contains(searchDto.Iso, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(searchDto.Type))
        {
            if (Enum.TryParse<EFilmType>(searchDto.Type, out var filmType))
            {
                filteredFilms = filteredFilms.Where(f => f.Type == filmType);
            }
        }


        if (!string.IsNullOrEmpty(searchDto.PurchasedBy))
        {
            if (Enum.TryParse<EUsernameType>(searchDto.PurchasedBy, out var usernameType))
            {
                filteredFilms = filteredFilms.Where(f => f.PurchasedBy == usernameType);
            }
        }

        if (!string.IsNullOrEmpty(searchDto.DevelopedWithDevKitId))
        {
            filteredFilms = filteredFilms.Where(f => f.DevelopedWithDevKitId == searchDto.DevelopedWithDevKitId);
        }

        if (!string.IsNullOrEmpty(searchDto.DevelopedInSessionId))
        {
            filteredFilms = filteredFilms.Where(f => f.DevelopedInSessionId == searchDto.DevelopedInSessionId);
        }

        return filteredFilms;
    }

}
