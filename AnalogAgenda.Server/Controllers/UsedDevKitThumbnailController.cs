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
public class UsedDevKitThumbnailController(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService) : ControllerBase
{
    private readonly Storage storageCfg = storageCfg;
    private readonly IDatabaseService databaseService = databaseService;
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
                    .Select(t => t.ToDTO(storageCfg.AccountName))
                    .ToList()
                : allThumbnails
                    .Where(t => t.DevKitName.Contains(devKitName, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(t => t.DevKitName)
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
            
            var createdDto = entity.ToDTO(storageCfg.AccountName);
            return Created(string.Empty, createdDto);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity(ex.Message);
        }
    }
}
