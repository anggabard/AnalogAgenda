using System.IO.Compression;
using AnalogAgenda.Server.Services.Interfaces;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]"), ApiController, Authorize]
public class PhotoController(
    Storage storageCfg,
    Security securityCfg,
    IDatabaseService databaseService,
    IBlobService blobsService,
    IImageCacheService imageCacheService
) : ControllerBase
{
    private readonly Storage storageCfg = storageCfg;
    private readonly Security securityCfg = securityCfg;
    private readonly IDatabaseService databaseService = databaseService;
    private readonly IBlobService blobsService = blobsService;
    private readonly IImageCacheService imageCacheService = imageCacheService;
    private readonly BlobContainerClient photosContainer = blobsService.GetBlobContainer(ContainerName.photos);

    [HttpGet("UploadKey")]
    public async Task<IActionResult> GetUploadKey([FromQuery] string filmId)
    {
        // Validate filmId exists
        var film = await databaseService.GetByIdAsync<FilmEntity>(filmId);
        if (film == null)
            return NotFound("Film not found.");

        // Generate key using IdGenerator
        var key = IdGenerator.Get(16, filmId, securityCfg.Salt);

        // Create KeyEntity
        var keyEntity = new KeyEntity
        {
            Key = key,
            ExpirationDate = DateTime.UtcNow.AddMinutes(3)
        };

        // Save to database
        await databaseService.AddAsync(keyEntity);

        // Return key as plain string
        return Ok(key);
    }

    [HttpGet("ValidateUploadKey")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateUploadKey([FromQuery] string key, [FromQuery] string filmId)
    {
        // Regenerate expected key
        var expectedKey = IdGenerator.Get(16, filmId, securityCfg.Salt);

        // Check if generated key matches provided key
        if (expectedKey != key)
            return Forbid();

        // Query Keys table for key
        var keyEntity = await databaseService.GetAllAsync<KeyEntity>(k => k.Key == key);
        if (keyEntity.Count == 0)
            return Forbid();

        var foundKey = keyEntity.First();

        // Check ExpirationDate is greater than UTC Now
        if (foundKey.ExpirationDate <= DateTime.UtcNow)
            return Forbid();

        // All validations passed
        return Ok();
    }

    [HttpGet("film/{filmId}")]
    public async Task<IActionResult> GetPhotosByFilmId(string filmId)
    {
        var photos = await databaseService.GetAllAsync<PhotoEntity>(p => p.FilmId == filmId);
        var sortedPhotos = photos
            .ApplyStandardSorting()
            .Select(e => e.ToDTO(storageCfg.AccountName))
            .ToList();

        return Ok(sortedPhotos);
    }

    [HttpGet("preview/{id}")]
    public async Task<IActionResult> GetPreview(string id)
    {
        var photoEntity = await databaseService.GetByIdAsync<PhotoEntity>(id);
        if (photoEntity == null)
            return NotFound("Photo not found.");

        // Check cache first
        if (
            imageCacheService.TryGetPreview(photoEntity.ImageId, out var cachedImage)
            && cachedImage != null
        )
        {
            var (imageBytes, contentType) = cachedImage.Value;
            return File(imageBytes, contentType);
        }

        try
        {
            // Serve preview directly from blob storage (preview/{imageId})
            var previewBlobClient = photosContainer.GetBlobClient($"preview/{photoEntity.ImageId}");
            
            if (!await previewBlobClient.ExistsAsync())
            {
                return NotFound("Preview not found in storage.");
            }

            var response = await previewBlobClient.DownloadAsync();
            var contentType = response.Value.Details.ContentType ?? "image/jpeg";
            
            using var memoryStream = new MemoryStream();
            await response.Value.Content.CopyToAsync(memoryStream);
            var previewBytes = memoryStream.ToArray();

            // Cache the preview with content type
            imageCacheService.SetPreview(photoEntity.ImageId, previewBytes, contentType);

            return File(previewBytes, contentType);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity($"Error loading preview: {ex.Message}");
        }
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadPhoto(string id)
    {
        var photoEntity = await databaseService.GetByIdAsync<PhotoEntity>(id);
        if (photoEntity == null)
            return NotFound("Photo not found.");

        var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(photoEntity.FilmId);
        if (filmEntity == null)
            return NotFound("Associated film not found.");

        try
        {
            var base64WithType = await BlobImageHelper.DownloadImageAsBase64WithContentTypeAsync(
                photosContainer,
                photoEntity.ImageId
            );
            var contentType = BlobImageHelper.GetContentTypeFromBase64(base64WithType);
            var fileExtension = BlobImageHelper.GetFileExtensionFromBase64(base64WithType);
            var fileName =
                $"{photoEntity.Index:D3}-{SanitizeFileName(filmEntity!.Name)}.{fileExtension}";

            // Extract bytes from base64 data URL
            var base64Data = base64WithType.Split(',')[1];
            var bytes = Convert.FromBase64String(base64Data);

            return File(bytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity($"Error downloading photo: {ex.Message}");
        }
    }

    [HttpGet("download-all/{filmId}")]
    public async Task<IActionResult> DownloadAllPhotos(string filmId)
    {
        var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(filmId);
        if (filmEntity == null)
            return NotFound("Film not found.");

        var photos = await databaseService.GetAllAsync<PhotoEntity>(p => p.FilmId == filmId);
        if (photos.Count == 0)
            return NotFound("No photos found for this film.");

        try
        {
            using var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var photo in photos.OrderBy(p => p.Index))
                {
                    var blobClient = photosContainer.GetBlobClient(photo.ImageId.ToString());
                    if (await blobClient.ExistsAsync())
                    {
                        var base64WithType =
                            await BlobImageHelper.DownloadImageAsBase64WithContentTypeAsync(
                                photosContainer,
                                photo.ImageId
                            );
                        var fileExtension = BlobImageHelper.GetFileExtensionFromBase64(
                            base64WithType
                        );
                        var fileName = $"{photo.Index:D3}.{fileExtension}";

                        var zipEntry = archive.CreateEntry(fileName);
                        using var zipStream = zipEntry.Open();

                        // Extract bytes from base64 data URL
                        var base64Data = base64WithType.Split(',')[1];
                        var bytes = Convert.FromBase64String(base64Data);
                        await zipStream.WriteAsync(bytes);
                    }
                }
            }

            memoryStream.Position = 0;
            var archiveName = $"{SanitizeFileName(filmEntity.Name)}.zip";

            return File(memoryStream.ToArray(), "application/zip", archiveName);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity($"Error creating archive: {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePhoto(string id)
    {
        var entity = await databaseService.GetByIdAsync<PhotoEntity>(id);
        if (entity == null)
            return NotFound();

        // Delete image blob and preview blob (photos always have real images, no default)
        if (entity.ImageId != Guid.Empty)
        {
            await photosContainer.DeleteBlobAsync(entity.ImageId.ToString());
            await photosContainer.DeleteBlobAsync($"preview/{entity.ImageId}");
            imageCacheService.RemovePreview(entity.ImageId);
        }

        await databaseService.DeleteAsync(entity);
        return NoContent();
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "photo" : sanitized;
    }
}
