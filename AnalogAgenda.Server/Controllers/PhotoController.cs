using AnalogAgenda.Server.Helpers;
using Azure.Storage.Blobs;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]"), ApiController, Authorize]
public class PhotoController(
    IDatabaseService databaseService,
    IBlobService blobsService,
    DtoConvertor dtoConvertor,
    EntityConvertor entityConvertor
) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly DtoConvertor dtoConvertor = dtoConvertor;
    private readonly EntityConvertor entityConvertor = entityConvertor;
    private readonly BlobContainerClient photosContainer = blobsService.GetBlobContainer(ContainerName.photos);

    [HttpPost]
    [RequestSizeLimit(RequestBodySizeLimits.PhotoUpload)]
    public async Task<IActionResult> UploadPhoto([FromBody] PhotoCreateDto photoDto)
    {
        if (photoDto == null || string.IsNullOrWhiteSpace(photoDto.ImageBase64))
        {
            return BadRequest("Photo data is required.");
        }

        try
        {
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
                Restricted = false
            };

            var entity = entityConvertor.ToEntity(photoEntityDto);
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

            var createdDto = dtoConvertor.ToDTO(entity);
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
        var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(filmId);
        if (filmEntity == null)
            return NotFound("Film not found.");

        var photos = await databaseService.GetAllAsync<PhotoEntity>(p => p.FilmId == filmId);
        var isOwner = FilmOwnerHelper.IsCurrentUserFilmOwner(User, filmEntity);
        if (!isOwner)
            photos = photos.Where(p => !p.Restricted).ToList();

        var sortedPhotos = photos
            .ApplyStandardSorting()
            .Select(dtoConvertor.ToDTO)
            .ToList();

        return Ok(sortedPhotos);
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

        if (photoEntity.Restricted && !FilmOwnerHelper.IsCurrentUserFilmOwner(User, filmEntity))
            return NotFound("Photo not found.");

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
    public async Task<IActionResult> DownloadAllPhotos(string filmId, [FromQuery] bool small = false)
    {
        var filmEntity = await databaseService.GetByIdWithIncludesAsync<FilmEntity>(
            filmId,
            f => f.ExposureDates
        );
        if (filmEntity == null)
            return NotFound("Film not found.");

        var allPhotos = await databaseService.GetAllAsync<PhotoEntity>(p => p.FilmId == filmId);
        var isOwner = FilmOwnerHelper.IsCurrentUserFilmOwner(User, filmEntity);
        var photos = isOwner ? allPhotos : allPhotos.Where(p => !p.Restricted).ToList();
        var ordered = photos.OrderBy(p => p.Index).ToList();
        return await BuildPhotosZipResponseAsync(filmEntity, ordered, small, archiveLabelSuffix: string.Empty);
    }

    [HttpPost("download-selected")]
    public async Task<IActionResult> DownloadSelectedPhotos([FromBody] PhotoDownloadSelectionDto? body)
    {
        if (body == null || body.Ids == null || body.Ids.Count == 0)
            return BadRequest("Photo ids are required.");
        if (string.IsNullOrWhiteSpace(body.FilmId))
            return BadRequest("Film id is required.");

        var filmEntity = await databaseService.GetByIdWithIncludesAsync<FilmEntity>(
            body.FilmId,
            f => f.ExposureDates
        );
        if (filmEntity == null)
            return NotFound();

        var distinctIds = body.Ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
        if (distinctIds.Count == 0)
            return BadRequest("Photo ids are required.");

        var loaded = (await databaseService.GetAllAsync<PhotoEntity>(photo => distinctIds.Contains(photo.Id))).ToList();

        var loadedIds = loaded.Select(photo => photo.Id).ToHashSet();
        if (distinctIds.Any(photoId => !loadedIds.Contains(photoId)))
            return NotFound();

        if (loaded.Any(photo => photo.FilmId != body.FilmId))
            return NotFound();
        var isOwner = FilmOwnerHelper.IsCurrentUserFilmOwner(User, filmEntity);
        if (loaded.Any(p => p.Restricted && !isOwner))
            return NotFound();

        var ordered = loaded.OrderBy(p => p.Index).ToList();
        return await BuildPhotosZipResponseAsync(filmEntity, ordered, body.Small, archiveLabelSuffix: "-selected");
    }

    private async Task<IActionResult> BuildPhotosZipResponseAsync(
        FilmEntity filmEntity,
        IReadOnlyList<PhotoEntity> photosOrdered,
        bool small,
        string archiveLabelSuffix)
    {
        if (photosOrdered.Count == 0)
            return NotFound("No photos found for this film.");

        try
        {
            var filmDto = dtoConvertor.ToDTO(filmEntity);
            var formattedDate = string.IsNullOrEmpty(filmDto.FormattedExposureDate)
                ? string.Empty
                : $"-{SanitizeFileName(filmDto.FormattedExposureDate)}";

            var archiveName = small
                ? $"{SanitizeFileName(filmEntity.Name)}{formattedDate}{archiveLabelSuffix}-small.zip"
                : $"{SanitizeFileName(filmEntity.Name)}{formattedDate}{archiveLabelSuffix}.zip";

            var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
            FileStream? tempFileStream = null;

            try
            {
                tempFileStream = new FileStream(
                    tempFilePath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    4096,
                    FileOptions.DeleteOnClose);

                using (var archive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, leaveOpen: true))
                {
                    foreach (var photo in photosOrdered)
                    {
                        var blobPath = small
                            ? $"preview/{photo.ImageId}"
                            : photo.ImageId.ToString();

                        var contentType = await BlobImageHelper.GetBlobContentTypeAsync(photosContainer, blobPath);
                        var fileExtension = BlobImageHelper.GetFileExtensionFromContentType(contentType);
                        var fileName = $"{photo.Index:D3}.{fileExtension}";

                        var zipEntry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                        await using (var zipStream = zipEntry.Open())
                        {
                            await BlobImageHelper.CopyBlobToAsync(photosContainer, blobPath, zipStream);
                        }
                    }
                }

                tempFileStream.Position = 0;
                return File(tempFileStream, "application/zip", archiveName);
            }
            catch
            {
                tempFileStream?.Dispose();
                throw;
            }
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

        var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(entity.FilmId);
        if (filmEntity == null || !FilmOwnerHelper.IsCurrentUserFilmOwner(User, filmEntity))
            return Forbid();

        // Collection card image stores the photo blob id; reset so we do not point at deleted blobs.
        var imageId = entity.ImageId;
        if (imageId != Guid.Empty)
        {
            var collectionsWithCardImage = await databaseService.GetAllAsync<CollectionEntity>(c => c.ImageId == imageId);
            foreach (var col in collectionsWithCardImage)
            {
                col.ImageId = Constants.DefaultCollectionImageId;
            }
        }

        // Delete image blob and preview blob (photos always have real images, no default)
        if (entity.ImageId != Guid.Empty)
        {
            await photosContainer.GetBlobClient(entity.ImageId.ToString()).DeleteIfExistsAsync();
            await photosContainer.GetBlobClient($"preview/{entity.ImageId}").DeleteIfExistsAsync();
        }

        await databaseService.DeleteAsync(entity);
        return NoContent();
    }

    [HttpPatch("{id}/restricted")]
    public async Task<IActionResult> SetPhotoRestricted(string id, [FromBody] SetRestrictedDto dto)
    {
        var photoEntity = await databaseService.GetByIdAsync<PhotoEntity>(id);
        if (photoEntity == null)
            return NotFound();

        var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(photoEntity.FilmId);
        if (filmEntity == null || !FilmOwnerHelper.IsCurrentUserFilmOwner(User, filmEntity))
            return Forbid();

        photoEntity.Restricted = dto.Restricted;
        await databaseService.UpdateAsync(photoEntity);
        return Ok(dtoConvertor.ToDTO(photoEntity));
    }

    [HttpPost("{id}/rotate")]
    public async Task<IActionResult> RotatePhoto90Clockwise(string id)
    {
        var photoEntity = await databaseService.GetByIdAsync<PhotoEntity>(id);
        if (photoEntity == null)
            return NotFound();

        var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(photoEntity.FilmId);
        if (filmEntity == null || !FilmOwnerHelper.IsCurrentUserFilmOwner(User, filmEntity))
            return Forbid();

        if (photoEntity.ImageId == Guid.Empty)
            return BadRequest("Photo has no image.");

        try
        {
            var (imageBytes, contentType) = await BlobImageHelper.DownloadImageAsBytesAsync(
                photosContainer,
                photoEntity.ImageId.ToString());

            var (rotatedBytes, outputContentType) =
                await BlobImageHelper.RotateImageBytes90ClockwiseAsync(imageBytes, contentType);

            await BlobImageHelper.UploadImageFromBytesAsync(
                photosContainer,
                rotatedBytes,
                outputContentType,
                photoEntity.ImageId,
                overwriteExisting: true);

            await BlobImageHelper.UploadPreviewImageFromBytesAsync(
                photosContainer,
                rotatedBytes,
                photoEntity.ImageId,
                maxDimension: 1200,
                quality: 80,
                overwriteExisting: true);

            await databaseService.UpdateAsync(photoEntity);

            // Bump collection rows that use this blob as the card image so their UpdatedDate matches new pixels.
            var collectionsUsingImage = await databaseService.GetAllAsync<CollectionEntity>(
                c => c.ImageId == photoEntity.ImageId);
            foreach (var coll in collectionsUsingImage)
            {
                await databaseService.UpdateAsync(coll);
            }

            return Ok(dtoConvertor.ToDTO(photoEntity));
        }
        catch (FileNotFoundException)
        {
            return NotFound("Image not found in storage.");
        }
        catch (Exception ex)
        {
            return UnprocessableEntity($"Error rotating photo: {ex.Message}");
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "photo" : sanitized;
    }
}
