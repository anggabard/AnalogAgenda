using AnalogAgenda.Server.Helpers;
using AnalogAgenda.Server.Identity;
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

[ApiController, Route("api/[controller]"), Authorize]
public class FilmController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) : ControllerBase
{
    private readonly TableClient filmsTable = tablesService.GetTable(TableName.Films);
    private readonly BlobContainerClient filmsContainer = blobsService.GetBlobContainer(ContainerName.films);

    [HttpPost]
    public async Task<IActionResult> CreateNewFilm([FromBody] FilmDto dto)
    {
        var imageId = Constants.DefaultFilmImageId;

        try
        {
            if (!string.IsNullOrEmpty(dto.ImageBase64))
            {
                imageId = Guid.NewGuid();
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(filmsContainer, dto.ImageBase64, imageId);
            }

            var entity = dto.ToEntity();
            entity.ImageId = imageId;

            await filmsTable.AddEntityAsync(entity);
        }
        catch (Exception ex)
        {
            if (imageId != Constants.DefaultFilmImageId)
                await filmsContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();

            return UnprocessableEntity(ex.Message);
        }

        return Ok();
    }

    [HttpGet]
    public async Task<IActionResult> GetAllFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all films
        if (page <= 0)
        {
            var entities = await tablesService.GetTableEntriesAsync<FilmEntity>();
            var results = entities.Select(entity => entity.ToDTO(storageCfg.AccountName));
            return Ok(results);
        }

        var pagedEntities = await tablesService.GetTableEntriesPagedAsync<FilmEntity>(page, pageSize);
        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = pagedEntities.Data.Select(entity => entity.ToDTO(storageCfg.AccountName)),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("developed")]
    public async Task<IActionResult> GetDevelopedFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all developed films
        if (page <= 0)
        {
            var entities = await tablesService.GetTableEntriesAsync<FilmEntity>(f => f.Developed);
            var results = entities
                .OrderBy(f => f.PurchasedBy) // First sort by owner
                .ThenByDescending(f => f.PurchasedOn) // Then by date (newest first)
                .Select(entity => entity.ToDTO(storageCfg.AccountName));
            return Ok(results);
        }

        var pagedEntities = await tablesService.GetTableEntriesPagedAsync<FilmEntity>(f => f.Developed, page, pageSize);
        var sortedData = pagedEntities.Data
            .OrderBy(f => f.PurchasedBy) // First sort by owner
            .ThenByDescending(f => f.PurchasedOn) // Then by date (newest first)
            .ToList();

        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = sortedData.Select(entity => entity.ToDTO(storageCfg.AccountName)),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("my/developed")]
    public async Task<IActionResult> GetMyDevelopedFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();

        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        if (page <= 0)
        {
            var allDevelopedEntities = await tablesService.GetTableEntriesAsync<FilmEntity>(f => f.Developed);
            var myEntities = allDevelopedEntities.Where(f => f.PurchasedBy == currentUserEnum).ToList();
            var results = myEntities
                .OrderByDescending(f => f.PurchasedOn)                .Select(entity => entity.ToDTO(storageCfg.AccountName));
            return Ok(results);
        }

        var allDevelopedPagedEntities = await tablesService.GetTableEntriesAsync<FilmEntity>(f => f.Developed);
        var myDevelopedFilms = allDevelopedPagedEntities.Where(f => f.PurchasedBy == currentUserEnum).ToList();
        
        var totalCount = myDevelopedFilms.Count;
        var skip = (page - 1) * pageSize;
        var pagedData = myDevelopedFilms
            .OrderByDescending(f => f.PurchasedOn)            .Skip(skip)
            .Take(pageSize)
            .ToList();

        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = pagedData.Select(entity => entity.ToDTO(storageCfg.AccountName)),
            TotalCount = totalCount,
            PageSize = pageSize,
            CurrentPage = page
        };

        return Ok(pagedResults);
    }

    [HttpGet("not-developed")]
    public async Task<IActionResult> GetNotDevelopedFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all not developed films
        if (page <= 0)
        {
            var entities = await tablesService.GetTableEntriesAsync<FilmEntity>(f => !f.Developed);
            var results = entities
                .OrderBy(f => f.PurchasedBy) // First sort by owner
                .ThenByDescending(f => f.PurchasedOn) // Then by date (newest first)
                .Select(entity => entity.ToDTO(storageCfg.AccountName));
            return Ok(results);
        }

        var pagedEntities = await tablesService.GetTableEntriesPagedAsync<FilmEntity>(f => !f.Developed, page, pageSize);
        var sortedData = pagedEntities.Data
            .OrderBy(f => f.PurchasedBy) // First sort by owner
            .ThenByDescending(f => f.PurchasedOn) // Then by date (newest first)
            .ToList();

        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = sortedData.Select(entity => entity.ToDTO(storageCfg.AccountName)),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    [HttpGet("my/not-developed")]
    public async Task<IActionResult> GetMyNotDevelopedFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();

        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        if (page <= 0)
        {
            var allNotDevelopedEntities = await tablesService.GetTableEntriesAsync<FilmEntity>(f => !f.Developed);
            var myEntities = allNotDevelopedEntities.Where(f => f.PurchasedBy == currentUserEnum).ToList();
            var results = myEntities
                .OrderByDescending(f => f.PurchasedOn)                .Select(entity => entity.ToDTO(storageCfg.AccountName));
            return Ok(results);
        }

        var allNotDevelopedPagedEntities = await tablesService.GetTableEntriesAsync<FilmEntity>(f => !f.Developed);
        var myNotDevelopedFilms = allNotDevelopedPagedEntities.Where(f => f.PurchasedBy == currentUserEnum).ToList();
        
        var totalCount = myNotDevelopedFilms.Count;
        var skip = (page - 1) * pageSize;
        var pagedData = myNotDevelopedFilms
            .OrderByDescending(f => f.PurchasedOn)            .Skip(skip)
            .Take(pageSize)
            .ToList();

        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = pagedData.Select(entity => entity.ToDTO(storageCfg.AccountName)),
            TotalCount = totalCount,
            PageSize = pageSize,
            CurrentPage = page
        };

        return Ok(pagedResults);
    }

    [HttpGet("{rowKey}")]
    public async Task<IActionResult> GetFilmByRowKey(string rowKey)
    {
        var entity = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(rowKey);

        if (entity == null)
        {
            return NotFound($"No Film found with RowKey: {rowKey}");
        }

        return Ok(entity.ToDTO(storageCfg.AccountName));
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateFilm(string rowKey, [FromBody] FilmDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        var existingEntity = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(rowKey);
        if (existingEntity == null)
            return NotFound();

        var updatedEntity = updateDto.ToEntity();
        updatedEntity.CreatedDate = existingEntity.CreatedDate;

        var imageId = existingEntity.ImageId;
        if (!string.IsNullOrEmpty(updateDto.ImageBase64))
        {
            if (existingEntity.ImageId != Constants.DefaultFilmImageId)
            {
                await filmsContainer.DeleteBlobAsync(existingEntity.ImageId.ToString());
            }

            imageId = Guid.NewGuid();
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(filmsContainer, updateDto.ImageBase64, imageId);
        }

        updatedEntity.ImageId = imageId;
        updatedEntity.UpdatedDate = DateTime.UtcNow;

        await filmsTable.UpdateEntityAsync(updatedEntity, existingEntity.ETag, TableUpdateMode.Replace);

        return NoContent();
    }

    [HttpDelete("{rowKey}")]
    public async Task<IActionResult> DeleteFilm(string rowKey)
    {
        var existingEntity = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(rowKey);
        if (existingEntity == null)
            return NotFound();

        if (existingEntity.ImageId != Constants.DefaultFilmImageId)
            await filmsContainer.DeleteBlobAsync(existingEntity.ImageId.ToString());

        await filmsTable.DeleteEntityAsync(existingEntity);
        return NoContent();
    }

}
