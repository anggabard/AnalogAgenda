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
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
public class FilmController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) : BaseEntityController<FilmEntity, FilmDto>(storageCfg, tablesService, blobsService)
{
    private readonly TableClient filmsTable = tablesService.GetTable(TableName.Films);
    private readonly BlobContainerClient filmsContainer = blobsService.GetBlobContainer(ContainerName.films);

    protected override TableClient GetTable() => filmsTable;
    protected override BlobContainerClient GetBlobContainer() => filmsContainer;
    protected override Guid GetDefaultImageId() => Constants.DefaultFilmImageId;
    protected override FilmEntity DtoToEntity(FilmDto dto) => dto.ToEntity();
    protected override FilmDto EntityToDto(FilmEntity entity) => entity.ToDTO(storageCfg.AccountName);

    [HttpPost]
    public async Task<IActionResult> CreateNewFilm([FromBody] FilmDto dto)
    {
        return await CreateEntityWithImageAsync(dto, dto => dto.ImageBase64);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all films
        if (page <= 0)
        {
            var entities = await tablesService.GetTableEntriesAsync<FilmEntity>();
            var results = entities.Select(EntityToDto);
            return Ok(results);
        }

        var pagedEntities = await tablesService.GetTableEntriesPagedAsync<FilmEntity>(page, pageSize);
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
    public async Task<IActionResult> GetDevelopedFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        return await GetFilteredFilms(f => f.Developed, page, pageSize);
    }

    [HttpGet("my/developed")]
    public async Task<IActionResult> GetMyDevelopedFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();

        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        // Azure Table Storage may have issues with compound expressions, so filter by user first
        return await GetMyFilteredFilms(f => f.Developed, currentUserEnum, page, pageSize);
    }

    [HttpGet("not-developed")]
    public async Task<IActionResult> GetNotDevelopedFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        return await GetFilteredFilms(f => !f.Developed, page, pageSize);
    }

    [HttpGet("my/not-developed")]
    public async Task<IActionResult> GetMyNotDevelopedFilms([FromQuery] int page = 1, [FromQuery] int pageSize = 5)
    {
        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();

        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        // Azure Table Storage may have issues with compound expressions, so filter by user first
        return await GetMyFilteredFilms(f => !f.Developed, currentUserEnum, page, pageSize);
    }

    private async Task<IActionResult> GetFilteredFilms(
        Expression<Func<FilmEntity, bool>> predicate,
        int page = 1,
        int pageSize = 5)
    {
        // For backward compatibility, if page is 0 or negative, return all filtered films
        if (page <= 0)
        {
            var entities = await tablesService.GetTableEntriesAsync<FilmEntity>(predicate);
            var results = entities
                .OrderBy(f => f.PurchasedBy) // First sort by owner
                .ThenByDescending(f => f.PurchasedOn) // Then by date (newest first)
                .Select(EntityToDto);
            return Ok(results);
        }

        var pagedEntities = await tablesService.GetTableEntriesPagedAsync<FilmEntity>(predicate, page, pageSize);
        var sortedData = pagedEntities.Data
            .OrderBy(f => f.PurchasedBy) // First sort by owner
            .ThenByDescending(f => f.PurchasedOn) // Then by date (newest first)
            .ToList();

        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = sortedData.Select(EntityToDto),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage
        };

        return Ok(pagedResults);
    }

    private async Task<IActionResult> GetMyFilteredFilms(
        Expression<Func<FilmEntity, bool>> statusPredicate,
        EUsernameType currentUserEnum,
        int page = 1,
        int pageSize = 5)
    {
        if (page <= 0)
        {
            // Get all entities with status filter, then filter by user in memory
            var allEntities = await tablesService.GetTableEntriesAsync(statusPredicate);
            var myEntities = allEntities
                .Where(f => f.PurchasedBy == currentUserEnum)
                .OrderByDescending(f => f.PurchasedOn)
                .Select(EntityToDto);
            return Ok(myEntities);
        }

        // For pagination, we need to get all status-filtered entities and then page them
        var statusFilteredEntities = await tablesService.GetTableEntriesAsync(statusPredicate);
        var myFilms = statusFilteredEntities
            .Where(f => f.PurchasedBy == currentUserEnum)
            .OrderByDescending(f => f.PurchasedOn)
            .ToList();

        var totalCount = myFilms.Count;
        var skip = (page - 1) * pageSize;
        var pagedData = myFilms
            .Skip(skip)
            .Take(pageSize)
            .ToList();

        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = pagedData.Select(EntityToDto),
            TotalCount = totalCount,
            PageSize = pageSize,
            CurrentPage = page
        };

        return Ok(pagedResults);
    }

    [HttpGet("{rowKey}")]
    public async Task<IActionResult> GetFilmByRowKey(string rowKey)
    {
        return await GetEntityByRowKeyAsync(rowKey);
    }

    [HttpPut("{rowKey}")]
    public async Task<IActionResult> UpdateFilm(string rowKey, [FromBody] FilmDto updateDto)
    {
        return await UpdateEntityWithImageAsync(rowKey, updateDto, dto => dto.ImageBase64);
    }

    [HttpDelete("{rowKey}")]
    public async Task<IActionResult> DeleteFilm(string rowKey)
    {
        return await DeleteEntityWithImageAsync(rowKey);
    }

}
