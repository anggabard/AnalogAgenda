using AnalogAgenda.Server.Helpers;
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
using System.IO.Compression;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]"), ApiController, Authorize]
public class PhotoController(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService, IImageCacheService imageCacheService) : ControllerBase
{
    private readonly Storage storageCfg = storageCfg;
    private readonly IDatabaseService databaseService = databaseService;
    private readonly IBlobService blobsService = blobsService;
    private readonly IImageCacheService imageCacheService = imageCacheService;
    private readonly BlobContainerClient photosContainer = blobsService.GetBlobContainer(ContainerName.photos);
    
    private const int MaxPreviewDimension = 1200;
    private const int PreviewQuality = 80;

    [HttpPost]
    [RequestSizeLimit(150 * 1024 * 1024)] // 150MB limit to support base64-encoded 50MB image
    public async Task<IActionResult> CreatePhoto([FromBody] PhotoCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ImageBase64))
            return BadRequest("Image data is required.");

        if (!await FilmExists(dto.FilmId))
            return NotFound("Film not found.");

        // Use provided index if valid (0-999), otherwise get next available
        int photoIndex;
        if (dto.Index.HasValue && dto.Index.Value >= 0 && dto.Index.Value <= 999)
        {
            photoIndex = dto.Index.Value;
        }
        else
        {
            photoIndex = await GetNextPhotoIndexAsync(dto.FilmId);
        }

        var imageId = Guid.NewGuid();
        try
        {
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(photosContainer, dto.ImageBase64, imageId);

            var photoDto = new PhotoDto
            {
                FilmId = dto.FilmId,
                Index = photoIndex,
                ImageBase64 = dto.ImageBase64
            };
            
            var entity = photoDto.ToEntity();
            entity.ImageId = imageId;

            await databaseService.AddAsync(entity);
            
            // Auto-mark film as developed when photo is uploaded
            await MarkFilmAsDeveloped(dto.FilmId);
            
            // Return the created entity as DTO
            var createdDto = entity.ToDTO(storageCfg.AccountName);
            return Created(string.Empty, createdDto);
        }
        catch (Exception ex)
        {
            await photosContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();
            return UnprocessableEntity(ex.Message);
        }
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
        if (imageCacheService.TryGetPreview(photoEntity.ImageId, out var cachedImage) && cachedImage != null)
        {
            var (imageBytes, contentType) = cachedImage.Value;
            return File(imageBytes, contentType);
        }

        try
        {
            // Download and resize image using helper
            var (previewBytes, contentType) = await BlobImageHelper.DownloadAndResizeImageAsync(
                photosContainer, 
                photoEntity.ImageId, 
                MaxPreviewDimension, 
                PreviewQuality);

            // Cache the preview with content type
            imageCacheService.SetPreview(photoEntity.ImageId, previewBytes, contentType);

            return File(previewBytes, contentType);
        }
        catch (FileNotFoundException)
        {
            return NotFound("Image not found in storage.");
        }
        catch (Exception ex)
        {
            return UnprocessableEntity($"Error generating preview: {ex.Message}");
        }
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadPhoto(string id)
    {
        var photoEntity = await databaseService.GetByIdAsync<PhotoEntity>(id);
        if (photoEntity == null)
            return NotFound("Photo not found.");

        if (!await FilmExists(photoEntity.FilmId))
            return NotFound("Associated film not found.");

        var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(photoEntity.FilmId);

        try
        {
            var base64WithType = await BlobImageHelper.DownloadImageAsBase64WithContentTypeAsync(photosContainer, photoEntity.ImageId);
            var contentType = BlobImageHelper.GetContentTypeFromBase64(base64WithType);
            var fileExtension = BlobImageHelper.GetFileExtensionFromBase64(base64WithType);
            var fileName = $"{photoEntity.Index:D3}-{SanitizeFileName(filmEntity!.Name)}.{fileExtension}";

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
                        var base64WithType = await BlobImageHelper.DownloadImageAsBase64WithContentTypeAsync(photosContainer, photo.ImageId);
                        var fileExtension = BlobImageHelper.GetFileExtensionFromBase64(base64WithType);
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
        
        // Delete image blob (photos always have real images, no default)
        if (entity.ImageId != Guid.Empty)
        {
            await photosContainer.DeleteBlobAsync(entity.ImageId.ToString());
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

    private async Task<bool> FilmExists(string filmId)
    {
        return await databaseService.GetByIdAsync<FilmEntity>(filmId) != null;
    }

    private async Task<int> GetNextPhotoIndexAsync(string filmId)
    {
        var existingPhotos = await databaseService.GetAllAsync<PhotoEntity>(p => p.FilmId == filmId);
        return existingPhotos.Count != 0 ? existingPhotos.Max(p => p.Index) + 1 : 1;
    }

    private async Task MarkFilmAsDeveloped(string filmId)
    {
        // Load existing entity
        var existingEntity = await databaseService.GetByIdAsync<FilmEntity>(filmId);
        
        if (existingEntity != null && !existingEntity.Developed)
        {
            // Update the entity
            existingEntity.Developed = true;
            
            // UpdateAsync will handle UpdatedDate automatically
            await databaseService.UpdateAsync(existingEntity);
        }
    }
}

