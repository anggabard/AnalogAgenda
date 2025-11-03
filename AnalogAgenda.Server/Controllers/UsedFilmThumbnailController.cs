using AnalogAgenda.Server.Helpers;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
[ApiController, Authorize]
public class UsedFilmThumbnailController(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService) : ControllerBase
{
    private readonly Storage storageCfg = storageCfg;
    private readonly IDatabaseService databaseService = databaseService;
    private readonly BlobContainerClient filmsContainer = blobsService.GetBlobContainer(ContainerName.films);

    [HttpGet("search")]
    public async Task<IActionResult> SearchByFilmName([FromQuery] string? filmName)
    {
        try
        {
            var allThumbnails = await databaseService.GetAllAsync<UsedFilmThumbnailEntity>();
            
            // If filmName is empty or null, return all thumbnails
            // Otherwise, filter by partial, case-insensitive match on film name
            var matchingThumbnails = string.IsNullOrWhiteSpace(filmName)
                ? allThumbnails
                    .OrderBy(t => t.FilmName)
                    .Select(t => t.ToDTO(storageCfg.AccountName))
                    .ToList()
                : allThumbnails
                    .Where(t => t.FilmName.Contains(filmName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t.FilmName)
                    .Select(t => t.ToDTO(storageCfg.AccountName))
                    .ToList();

            return Ok(matchingThumbnails);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateThumbnailEntry([FromBody] UsedFilmThumbnailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FilmName))
            return BadRequest("FilmName is required.");

        if (string.IsNullOrWhiteSpace(dto.ImageBase64))
            return BadRequest("ImageBase64 is required for uploading a new thumbnail.");

        try
        {
            // Upload image to blob storage
            var imageId = Guid.NewGuid();
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(filmsContainer, dto.ImageBase64, imageId);

            // Create thumbnail entry in database
            var entity = new UsedFilmThumbnailEntity
            {
                FilmName = dto.FilmName,
                ImageId = imageId
            };
            await databaseService.AddAsync(entity);
            
            var createdDto = entity.ToDTO(storageCfg.AccountName);
            return Created(string.Empty, createdDto);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }
}