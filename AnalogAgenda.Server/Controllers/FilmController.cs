using AnalogAgenda.Server.Identity;
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
public class FilmController(
    IDatabaseService databaseService,
    IBlobService blobsService,
    DtoConvertor dtoConvertor,
    EntityConvertor entityConvertor
) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly IBlobService blobsService = blobsService;
    private readonly DtoConvertor dtoConvertor = dtoConvertor;
    private readonly EntityConvertor entityConvertor = entityConvertor;

    [HttpPost]
    public async Task<IActionResult> CreateNewFilm(
        [FromBody] FilmDto dto,
        [FromQuery] int bulkCount = 1
    )
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
                var entity = entityConvertor.ToEntity(dto);

                // Add milliseconds offset to ensure unique dates
                entity.CreatedDate = entity.CreatedDate.AddMilliseconds(i);
                entity.UpdatedDate = entity.UpdatedDate.AddMilliseconds(i);

                // If no ImageUrl provided, use default image
                if (entity.ImageId == Guid.Empty)
                {
                    entity.ImageId = Constants.DefaultFilmImageId;
                }

                await databaseService.AddAsync(entity);

                var createdDto = dtoConvertor.ToDTO(entity);
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
    public async Task<IActionResult> GetAllFilms(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 5
    )
    {
        // For backward compatibility, if page is 0 or negative, return all films
        if (page <= 0)
        {
            var entities = await databaseService.GetAllAsync<FilmEntity>();
            var results = entities.Select(dtoConvertor.ToDTO);
            return Ok(results);
        }

        var pagedEntities = await databaseService.GetPagedAsync<FilmEntity>(
            page,
            pageSize,
            entities => entities.ApplyStandardSorting()
        );
        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = pagedEntities.Data.Select(dtoConvertor.ToDTO),
            TotalCount = pagedEntities.TotalCount,
            PageSize = pagedEntities.PageSize,
            CurrentPage = pagedEntities.CurrentPage,
        };

        return Ok(pagedResults);
    }

    [HttpGet("developed")]
    public async Task<IActionResult> GetDevelopedFilms([FromQuery] FilmSearchDto searchDto)
    {
        return await GetFilteredFilms(f => f.Developed, searchDto, includePhotos: true);
    }

    [HttpGet("my/developed")]
    public async Task<IActionResult> GetMyDevelopedFilms([FromQuery] FilmSearchDto searchDto)
    {
        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();

        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        return await GetMyFilteredFilms(f => f.Developed, currentUserEnum, searchDto, includePhotos: true);
    }

    [HttpGet("not-developed")]
    public async Task<IActionResult> GetNotDevelopedFilms([FromQuery] FilmSearchDto searchDto)
    {
        return await GetFilteredFilms(f => !f.Developed, searchDto, includePhotos: false);
    }

    [HttpGet("my/not-developed")]
    public async Task<IActionResult> GetMyNotDevelopedFilms([FromQuery] FilmSearchDto searchDto)
    {
        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();

        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        return await GetMyFilteredFilms(f => !f.Developed, currentUserEnum, searchDto, includePhotos: false);
    }

    private async Task<IActionResult> GetFilteredFilms(
        Expression<Func<FilmEntity, bool>> predicate,
        FilmSearchDto searchDto,
        bool includePhotos = false
    )
    {
        // For backward compatibility, if page is 0 or negative, return all filtered films
        if (searchDto.Page <= 0)
        {
            var entities = includePhotos 
                ? await databaseService.GetAllWithIncludesAsync<FilmEntity>(f => f.Photos)
                : await databaseService.GetAllAsync(predicate);
            var filteredEntities = includePhotos
                ? ApplySearchFilters(entities.Where(predicate.Compile()), searchDto)
                : ApplySearchFilters(entities, searchDto);
            var results = filteredEntities
                .ApplyStandardSorting()
                .Select(dtoConvertor.ToDTO);
            return Ok(results);
        }

        var allEntities = includePhotos
            ? await databaseService.GetAllWithIncludesAsync<FilmEntity>(f => f.Photos)
            : await databaseService.GetAllAsync(predicate);
        var predicateFiltered = includePhotos
            ? allEntities.Where(predicate.Compile())
            : allEntities;
        var searchFilteredEntities = ApplySearchFilters(predicateFiltered, searchDto);
        var sortedEntities = searchFilteredEntities.ApplyStandardSorting().ToList();

        // Apply pagination
        var totalCount = sortedEntities.Count;
        var skip = (searchDto.Page - 1) * searchDto.PageSize;
        var pagedData = sortedEntities.Skip(skip).Take(searchDto.PageSize).ToList();

        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = pagedData.Select(dtoConvertor.ToDTO),
            TotalCount = totalCount,
            PageSize = searchDto.PageSize,
            CurrentPage = searchDto.Page,
        };

        return Ok(pagedResults);
    }

    private async Task<IActionResult> GetMyFilteredFilms(
        Expression<Func<FilmEntity, bool>> statusPredicate,
        EUsernameType currentUserEnum,
        FilmSearchDto searchDto,
        bool includePhotos = false
    )
    {
        if (searchDto.Page <= 0)
        {
            // Get all entities with status filter, then filter by user in memory
            var allEntities = includePhotos
                ? await databaseService.GetAllWithIncludesAsync<FilmEntity>(f => f.Photos)
                : await databaseService.GetAllAsync(statusPredicate);
            var statusFiltered = includePhotos
                ? allEntities.Where(statusPredicate.Compile())
                : allEntities;
            var myEntities = statusFiltered.Where(f => f.PurchasedBy == currentUserEnum);
            var filteredEntities = ApplySearchFilters(myEntities, searchDto);
            var results = filteredEntities
                .ApplyUserFilteredSorting()
                .Select(dtoConvertor.ToDTO);
            return Ok(results);
        }

        // For pagination, we need to get all status-filtered entities and then page them
        var allEntitiesForPaging = includePhotos
            ? await databaseService.GetAllWithIncludesAsync<FilmEntity>(f => f.Photos)
            : await databaseService.GetAllAsync(statusPredicate);
        var statusFilteredForPaging = includePhotos
            ? allEntitiesForPaging.Where(statusPredicate.Compile())
            : allEntitiesForPaging;
        var myFilms = statusFilteredForPaging.Where(f => f.PurchasedBy == currentUserEnum);
        var filteredFilms = ApplySearchFilters(myFilms, searchDto);
        var sortedFilms = filteredFilms.ApplyUserFilteredSorting().ToList();

        var totalCount = sortedFilms.Count;
        var skip = (searchDto.Page - 1) * searchDto.PageSize;
        var pagedData = sortedFilms.Skip(skip).Take(searchDto.PageSize).ToList();

        var pagedResults = new PagedResponseDto<FilmDto>
        {
            Data = pagedData.Select(dtoConvertor.ToDTO),
            TotalCount = totalCount,
            PageSize = searchDto.PageSize,
            CurrentPage = searchDto.Page,
        };

        return Ok(pagedResults);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetFilmById(string id)
    {
        var entity = await databaseService.GetByIdWithIncludesAsync<FilmEntity>(
            id,
            f => f.ExposureDates,
            f => f.Photos
        );
        if (entity == null)
            return NotFound($"No Film found with Id: {id}");

        return Ok(dtoConvertor.ToDTO(entity));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFilm(string id, [FromBody] FilmDto updateDto)
    {
        if (updateDto == null)
            return BadRequest("Invalid data.");

        // Load existing entity
        var existingEntity = await databaseService.GetByIdAsync<FilmEntity>(id);

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
    public async Task<IActionResult> DeleteFilm(string id)
    {
        // First, delete all associated photos
        await DeleteAssociatedPhotosAsync(id);

        // Then delete the film itself
        var entity = await databaseService.GetByIdAsync<FilmEntity>(id);
        if (entity == null)
            return NotFound();

        await databaseService.DeleteAsync(entity);
        return NoContent();
    }

    [HttpGet("{filmId}/exposure-dates")]
    public async Task<IActionResult> GetExposureDates(string filmId)
    {
        var film = await databaseService.GetByIdWithIncludesAsync<FilmEntity>(
            filmId,
            f => f.ExposureDates
        );
        if (film == null)
            return NotFound($"No Film found with Id: {filmId}");

        var exposureDates = film.ExposureDates
            .OrderBy(e => e.Date)
            .Select(e => new ExposureDateDto
            {
                Id = e.Id,
                FilmId = e.FilmId,
                Date = e.Date,
                Description = e.Description
            })
            .ToList();

        return Ok(exposureDates);
    }

    [HttpPut("{filmId}/exposure-dates")]
    public async Task<IActionResult> UpdateExposureDates(string filmId, [FromBody] List<ExposureDateDto> exposureDates)
    {
        if (exposureDates == null)
            return BadRequest("Invalid data.");

        var film = await databaseService.GetByIdWithIncludesAsync<FilmEntity>(
            filmId,
            f => f.ExposureDates
        );
        if (film == null)
            return NotFound($"No Film found with Id: {filmId}");

        try
        {
            // Remove all existing exposure dates
            var existingDates = film.ExposureDates.ToList();
            foreach (var existingDate in existingDates)
            {
                await databaseService.DeleteAsync(existingDate);
            }

            // Add new exposure dates
            foreach (var dto in exposureDates)
            {
                // Skip entries with invalid dates
                if (dto.Date == default(DateOnly))
                    continue;

                var exposureDate = new ExposureDateEntity
                {
                    Id = string.IsNullOrEmpty(dto.Id) ? string.Empty : dto.Id,
                    FilmId = filmId,
                    Date = dto.Date,
                    Description = dto.Description ?? string.Empty
                };

                if (string.IsNullOrEmpty(exposureDate.Id))
                {
                    exposureDate.Id = exposureDate.GetId();
                }

                await databaseService.AddAsync(exposureDate);
            }

            // If film has exposure dates, set it as current film for the user
            if (exposureDates.Any(ed => ed.Date != default(DateOnly)))
            {
                var currentUserId = User.Id();
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    var userSettingsList = await databaseService.GetAllAsync<UserSettingsEntity>(us => us.UserId == currentUserId);
                    var userSettings = userSettingsList.FirstOrDefault();

                    if (userSettings != null)
                    {
                        userSettings.CurrentFilmId = filmId;
                        userSettings.UpdatedDate = DateTime.UtcNow;
                        await databaseService.UpdateAsync(userSettings);
                    }
                }
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }

    private async Task DeleteAssociatedPhotosAsync(string filmId)
    {
        var photosContainer = blobsService.GetBlobContainer(ContainerName.photos);

        var photos = await databaseService.GetAllAsync<PhotoEntity>(p => p.FilmId == filmId);

        foreach (var photo in photos)
        {
            // Delete the image blob and preview blob
            if (photo.ImageId != Guid.Empty)
            {
                await photosContainer
                    .GetBlobClient(photo.ImageId.ToString())
                    .DeleteIfExistsAsync();
                await photosContainer
                    .GetBlobClient($"preview/{photo.ImageId}")
                    .DeleteIfExistsAsync();
            }

            // Delete the table entry
            await databaseService.DeleteAsync(photo);
        }
    }

    private IEnumerable<FilmEntity> ApplySearchFilters(
        IEnumerable<FilmEntity> films,
        FilmSearchDto searchDto
    )
    {
        var filteredFilms = films;

        // Apply each non-null/non-empty filter with AND logic
        if (!string.IsNullOrEmpty(searchDto.Name))
        {
            var searchTerm = searchDto.Name;
            filteredFilms = filteredFilms.Where(f =>
                f.Name != null && f.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (!string.IsNullOrEmpty(searchDto.Brand))
        {
            var searchTerm = searchDto.Brand;
            filteredFilms = filteredFilms.Where(f =>
                f.Brand.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (!string.IsNullOrEmpty(searchDto.Id))
        {
            filteredFilms = filteredFilms.Where(f =>
                f.Id.Contains(searchDto.Id, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (!string.IsNullOrEmpty(searchDto.Iso))
        {
            filteredFilms = filteredFilms.Where(f =>
                f.Iso.Contains(searchDto.Iso, StringComparison.OrdinalIgnoreCase)
            );
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
            filteredFilms = filteredFilms.Where(f =>
                f.DevelopedWithDevKitId == searchDto.DevelopedWithDevKitId
            );
        }

        if (!string.IsNullOrEmpty(searchDto.DevelopedInSessionId))
        {
            filteredFilms = filteredFilms.Where(f =>
                f.DevelopedInSessionId == searchDto.DevelopedInSessionId
            );
        }

        return filteredFilms;
    }

}
