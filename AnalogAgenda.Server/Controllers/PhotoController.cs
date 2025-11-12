using System.IO.Compression;
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
    IDatabaseService databaseService,
    IBlobService blobsService
) : ControllerBase
{
    private readonly Storage storageCfg = storageCfg;
    private readonly IDatabaseService databaseService = databaseService;
    private readonly IBlobService blobsService = blobsService;
    private readonly BlobContainerClient photosContainer = blobsService.GetBlobContainer(
        ContainerName.photos
    );

    [HttpPost]
    public async Task<IActionResult> UploadPhoto([FromBody] PhotoCreateDto photoDto)
    {
        if (photoDto == null || string.IsNullOrWhiteSpace(photoDto.ImageBase64))
        {
            return BadRequest("Photo data is required.");
        }

        try
        {
            // Validate film exists
            var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(photoDto.FilmId);
            if (filmEntity == null)
            {
                return NotFound("Film not found.");
            }

            // Get existing photos to calculate index
            var existingPhotos = await databaseService.GetAllAsync<PhotoEntity>(
                p => p.FilmId == photoDto.FilmId
            );
            int nextAvailableIndex =
                existingPhotos.Count == 0 ? 1 : existingPhotos.Max(p => p.Index) + 1;

            int photoIndex;
            if (photoDto.Index.HasValue && photoDto.Index.Value >= 0 && photoDto.Index.Value <= 999)
            {
                photoIndex = photoDto.Index.Value;
            }
            else
            {
                photoIndex = nextAvailableIndex;
            }

            // Check if a photo with this index already exists and delete it
            var photosAtIndex = existingPhotos.Where(p => p.Index == photoIndex).ToList();
            if (photosAtIndex.Count > 0)
            {
                var photoToReplace = photosAtIndex.First();

                // Delete old image blob
                if (photoToReplace.ImageId != Guid.Empty)
                {
                    await photosContainer
                        .GetBlobClient(photoToReplace.ImageId.ToString())
                        .DeleteIfExistsAsync();
                    await photosContainer
                        .GetBlobClient($"preview/{photoToReplace.ImageId}")
                        .DeleteIfExistsAsync();
                }

                // Delete old photo entity
                await databaseService.DeleteAsync(photoToReplace);
            }

            var imageId = Guid.NewGuid();

            // Parse base64 once and clear immediately to reduce memory pressure
            var base64Image = photoDto.ImageBase64;
            var (imageBytes, contentType) = BlobImageHelper.ParseBase64Image(base64Image);
            
            // Clear base64 string immediately after parsing to free memory
            base64Image = string.Empty;
            photoDto.ImageBase64 = string.Empty;

            // Upload full image using bytes (no need to convert back to base64)
            await BlobImageHelper.UploadImageFromBytesAsync(
                photosContainer,
                imageBytes,
                contentType,
                imageId
            );
            
            // Force GC after full image upload to free memory before preview processing
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();

            // Generate and upload preview using the same bytes (no need to parse base64 again)
            await BlobImageHelper.UploadPreviewImageFromBytesAsync(
                photosContainer,
                imageBytes,
                imageId,
                maxDimension: 1200,
                quality: 80
            );
            
            // Clear image bytes from memory
            imageBytes = null;
            
            // Force GC after preview upload to free memory immediately
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();

            // Create photo entity (without base64 - image is already in blob storage)
            var photoEntityDto = new PhotoDto
            {
                FilmId = photoDto.FilmId,
                Index = photoIndex,
                ImageBase64 = string.Empty, // Don't store base64 in entity - it's in blob storage
            };

            var entity = photoEntityDto.ToEntity();
            entity.ImageId = imageId;

            await databaseService.AddAsync(entity);

            // Auto-mark film as developed when photo is uploaded
            if (!filmEntity.Developed)
            {
                filmEntity.Developed = true;
                await databaseService.UpdateAsync(filmEntity);
            }

            // Final aggressive GC after all operations complete
            // This ensures memory is freed before the next request
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);

            var createdDto = entity.ToDTO(storageCfg.AccountName);
            return Ok(createdDto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error uploading photo: {ex.Message}");
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
            await photosContainer.GetBlobClient(entity.ImageId.ToString()).DeleteIfExistsAsync();
            await photosContainer.GetBlobClient($"preview/{entity.ImageId}").DeleteIfExistsAsync();
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
