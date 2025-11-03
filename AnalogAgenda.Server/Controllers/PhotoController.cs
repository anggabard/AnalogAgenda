using AnalogAgenda.Server.Helpers;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.Data;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
public class PhotoController(Storage storageCfg, IDatabaseService databaseService, IBlobService blobsService, AnalogAgendaDbContext dbContext) : BaseEntityController<PhotoEntity, PhotoDto>(storageCfg, databaseService, blobsService, dbContext)
{
    private readonly BlobContainerClient photosContainer = blobsService.GetBlobContainer(ContainerName.photos);

    protected override BlobContainerClient GetBlobContainer() => photosContainer;
    protected override Guid GetDefaultImageId() => Guid.Empty; // Photos always have real images, no default needed
    protected override PhotoEntity DtoToEntity(PhotoDto dto) => dto.ToEntity();
    protected override PhotoDto EntityToDto(PhotoEntity entity) => entity.ToDTO(storageCfg.AccountName);

    [HttpPost]
    public async Task<IActionResult> CreatePhoto([FromBody] PhotoCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ImageBase64))
            return BadRequest("Image data is required.");

        if (!await FilmExists(dto.FilmId))
            return NotFound("Film not found.");

        int nextIndex = await GetNextPhotoIndexAsync(dto.FilmId);

        var result = await CreateEntityWithImageAsync(new PhotoDto
        {
            FilmId = dto.FilmId,
            Index = nextIndex,
            ImageBase64 = dto.ImageBase64
        }, photoDto => photoDto.ImageBase64);

        // Auto-mark film as developed when photo is uploaded
        if (result is CreatedResult)
        {
            await MarkFilmAsDeveloped(dto.FilmId);
        }

        return result;
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> UploadPhotos([FromBody] PhotoBulkUploadDto bulkDto)
    {
        if (bulkDto?.Photos == null || bulkDto.Photos.Count == 0)
            return BadRequest("No photos provided.");

        // Validate all photos have base64 data
        if (bulkDto.Photos.Any(p => string.IsNullOrWhiteSpace(p.ImageBase64)))
            return BadRequest("All photos must have image data.");

        if (!await FilmExists(bulkDto.FilmId))
            return NotFound("Film not found.");

        int nextIndex = await GetNextPhotoIndexAsync(bulkDto.FilmId);

        var uploadedImageIds = new List<Guid>();

        try
        {
            foreach (var photoDto in bulkDto.Photos)
            {
                var imageId = Guid.NewGuid();
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(photosContainer, photoDto.ImageBase64, imageId);
                uploadedImageIds.Add(imageId);

                var photoEntity = new PhotoEntity
                {
                    FilmId = bulkDto.FilmId,
                    Index = nextIndex++,
                    ImageId = imageId
                };

                await databaseService.AddAsync(photoEntity);
            }

            // Auto-mark film as developed when photos are uploaded
            await MarkFilmAsDeveloped(bulkDto.FilmId);

            return NoContent();
        }
        catch (Exception ex)
        {
            // Cleanup uploaded blobs on failure
            foreach (var imageId in uploadedImageIds)
            {
                await photosContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();
            }

            return UnprocessableEntity(ex.Message);
        }
    }

    [HttpGet("film/{filmId}")]
    public async Task<IActionResult> GetPhotosByFilmId(string filmId)
    {
        var photos = await databaseService.GetAllAsync<PhotoEntity>(p => p.FilmId == filmId);
        var sortedPhotos = photos
            .ApplyStandardSorting()
            .Select(EntityToDto)
            .ToList();

        return Ok(sortedPhotos);
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
        return await DeleteEntityWithImageAsync(id);
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
        // Load entity without tracking to avoid conflicts
        var existingEntity = await dbContext.Set<FilmEntity>()
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == filmId);
        
        if (existingEntity != null && !existingEntity.Developed)
        {
            // Create updated entity
            var updatedEntity = new FilmEntity
            {
                Id = existingEntity.Id,
                CreatedDate = existingEntity.CreatedDate,
                UpdatedDate = DateTime.UtcNow,
                Name = existingEntity.Name,
                Iso = existingEntity.Iso,
                Type = existingEntity.Type,
                NumberOfExposures = existingEntity.NumberOfExposures,
                Cost = existingEntity.Cost,
                PurchasedBy = existingEntity.PurchasedBy,
                PurchasedOn = existingEntity.PurchasedOn,
                ImageId = existingEntity.ImageId,
                Description = existingEntity.Description,
                Developed = true, // Mark as developed
                DevelopedInSessionId = existingEntity.DevelopedInSessionId,
                DevelopedWithDevKitId = existingEntity.DevelopedWithDevKitId,
                ExposureDates = existingEntity.ExposureDates
            };
            
            // Attach and update
            dbContext.Set<FilmEntity>().Attach(updatedEntity);
            dbContext.Entry(updatedEntity).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
            
            // Clear navigation properties
            dbContext.Entry(updatedEntity).Reference(f => f.DevelopedWithDevKit).CurrentValue = null;
            dbContext.Entry(updatedEntity).Reference(f => f.DevelopedInSession).CurrentValue = null;
            dbContext.Entry(updatedEntity).Collection(f => f.Photos).IsLoaded = false;
            
            await dbContext.SaveChangesAsync();
        }
    }
}

