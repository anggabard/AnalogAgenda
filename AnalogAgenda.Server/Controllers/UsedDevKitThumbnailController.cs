using Azure.Storage.Blobs;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
[ApiController, Authorize]
public class UsedDevKitThumbnailController(IDatabaseService databaseService, IBlobService blobsService, DtoConvertor dtoConvertor) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly DtoConvertor dtoConvertor = dtoConvertor;
    private readonly BlobContainerClient devKitsContainer = blobsService.GetBlobContainer(ContainerName.devkits);

    [HttpGet("search")]
    public async Task<IActionResult> SearchByDevKitName([FromQuery] string? devKitName)
    {
        try
        {
            var allThumbnails = await databaseService.GetAllAsync<UsedDevKitThumbnailEntity>();
            
            // If devKitName is empty or null, return all thumbnails
            // Otherwise, filter by partial, case-insensitive match on devkit name
            var matchingThumbnails = string.IsNullOrWhiteSpace(devKitName)
                ? allThumbnails
                    .OrderBy(t => t.DevKitName)
                    .Select(dtoConvertor.ToDTO)
                    .ToList()
                : allThumbnails
                    .Where(t => t.DevKitName.Contains(devKitName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t.DevKitName)
                    .Select(dtoConvertor.ToDTO)
                    .ToList();

            return Ok(matchingThumbnails);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateThumbnailEntry([FromBody] UsedDevKitThumbnailDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.DevKitName))
            return BadRequest("DevKitName is required.");

        if (string.IsNullOrWhiteSpace(dto.ImageBase64))
            return BadRequest("ImageBase64 is required for uploading a new thumbnail.");

        try
        {
            // Upload image to blob storage
            var imageId = Guid.NewGuid();
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(devKitsContainer, dto.ImageBase64, imageId);

            // Create thumbnail entry in database
            var entity = new UsedDevKitThumbnailEntity
            {
                DevKitName = dto.DevKitName,
                ImageId = imageId
            };
            await databaseService.AddAsync(entity);
            
            var createdDto = dtoConvertor.ToDTO(entity);
            return Created(string.Empty, createdDto);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }
}
