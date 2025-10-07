using AnalogAgenda.Server.Helpers;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]")]
public class PhotoController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) : BaseEntityController<PhotoEntity, PhotoDto>(storageCfg, tablesService, blobsService)
{
    private readonly TableClient photosTable = tablesService.GetTable(TableName.Photos);
    private readonly BlobContainerClient photosContainer = blobsService.GetBlobContainer(ContainerName.photos);

    protected override TableClient GetTable() => photosTable;
    protected override BlobContainerClient GetBlobContainer() => photosContainer;
    protected override Guid GetDefaultImageId() => Guid.Empty; // Photos always have real images, no default needed
    protected override PhotoEntity DtoToEntity(PhotoDto dto) => dto.ToEntity();
    protected override PhotoDto EntityToDto(PhotoEntity entity) => entity.ToDTO(storageCfg.AccountName);

    [HttpPost]
    public async Task<IActionResult> CreatePhoto([FromBody] PhotoCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ImageBase64))
            return BadRequest("Image data is required.");

        if (!await FilmExists(dto.FilmRowId))
            return NotFound("Film not found.");

        int nextIndex = await GetNextPhotoIndexAsync(dto.FilmRowId);

        var result = await CreateEntityWithImageAsync(new PhotoDto
        {
            FilmRowId = dto.FilmRowId,
            Index = nextIndex,
            ImageBase64 = dto.ImageBase64
        }, photoDto => photoDto.ImageBase64);

        // Auto-mark film as developed when photo is uploaded
        if (result is CreatedResult)
        {
            await MarkFilmAsDeveloped(dto.FilmRowId);
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

        if (!await FilmExists(bulkDto.FilmRowId))
            return NotFound("Film not found.");

        int nextIndex = await GetNextPhotoIndexAsync(bulkDto.FilmRowId);

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
                    FilmRowId = bulkDto.FilmRowId,
                    Index = nextIndex++,
                    ImageId = imageId
                };

                await photosTable.AddEntityAsync(photoEntity);
            }

            // Auto-mark film as developed when photos are uploaded
            await MarkFilmAsDeveloped(bulkDto.FilmRowId);

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

    [HttpGet("film/{filmRowId}")]
    public async Task<IActionResult> GetPhotosByFilmId(string filmRowId)
    {
        var photos = await tablesService.GetTableEntriesAsync<PhotoEntity>(p => p.FilmRowId == filmRowId);
        var sortedPhotos = photos
            .ApplyStandardSorting()
            .Select(EntityToDto)
            .ToList();

        return Ok(sortedPhotos);
    }

    [HttpGet("download/{rowKey}")]
    public async Task<IActionResult> DownloadPhoto(string rowKey)
    {
        var photoEntity = await tablesService.GetTableEntryIfExistsAsync<PhotoEntity>(rowKey);
        if (photoEntity == null)
            return NotFound("Photo not found.");

        if (!await FilmExists(photoEntity.FilmRowId))
            return NotFound("Associated film not found.");

        var filmEntity = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(photoEntity.FilmRowId);

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

    [HttpGet("download-all/{filmRowId}")]
    public async Task<IActionResult> DownloadAllPhotos(string filmRowId)
    {
        var filmEntity = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowId);
        if (filmEntity == null)
            return NotFound("Film not found.");

        var photos = await tablesService.GetTableEntriesAsync<PhotoEntity>(p => p.FilmRowId == filmRowId);
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

    [HttpDelete("{rowKey}")]
    public async Task<IActionResult> DeletePhoto(string rowKey)
    {
        return await DeleteEntityWithImageAsync(rowKey);
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "photo" : sanitized;
    }

    private async Task<bool> FilmExists(string filmRowId)
    {
        return await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowId) != null;
    }

    private async Task<int> GetNextPhotoIndexAsync(string filmRowId)
    {
        var existingPhotos = await tablesService.GetTableEntriesAsync<PhotoEntity>(p => p.FilmRowId == filmRowId);
        return existingPhotos.Count != 0 ? existingPhotos.Max(p => p.Index) + 1 : 1;
    }

    private async Task MarkFilmAsDeveloped(string filmRowId)
    {
        var filmEntity = await tablesService.GetTableEntryIfExistsAsync<FilmEntity>(filmRowId);
        if (filmEntity != null && !filmEntity.Developed)
        {
            filmEntity.Developed = true;
            var filmsTable = tablesService.GetTable(TableName.Films);
            await filmsTable.UpdateEntityAsync(filmEntity, filmEntity.ETag, TableUpdateMode.Replace);
        }
    }
}

