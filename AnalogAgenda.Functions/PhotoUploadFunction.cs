using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace AnalogAgenda.Functions;

public class PhotoUploadFunction(
    ILoggerFactory loggerFactory,
    IDatabaseService databaseService,
    IBlobService blobService,
    Storage storageCfg,
    Security securityCfg,
    HttpClient httpClient)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<PhotoUploadFunction>();
    private readonly BlobContainerClient photosContainer = blobService.GetBlobContainer(ContainerName.photos);
    private readonly string backendApiUrl = securityCfg.BackendApiUrl ?? throw new ArgumentNullException(nameof(securityCfg.BackendApiUrl));
    private const int MaxPreviewDimension = 1200;
    private const int PreviewQuality = 80;

    [Function("PhotoUpload")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "photo/upload")] HttpRequestData req)
    {
        _logger.LogInformation("Photo upload HTTP trigger function executed");

        if (req.Method == "OPTIONS")
        {
            var optionsResponse = req.CreateResponse(HttpStatusCode.OK);
            await optionsResponse.WriteStringAsync(string.Empty);
            return optionsResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        try
        {
            // Extract Key query parameter
            var key = req.Query["Key"].ToString() ?? string.Empty;

            // Read and deserialize request body
            // Read body into memory to avoid stream consumption issues
            string requestBody;
            using (var memoryStream = new MemoryStream())
            {
                await req.Body.CopyToAsync(memoryStream);
                memoryStream.Position = 0;
                using var reader = new StreamReader(memoryStream);
                requestBody = await reader.ReadToEndAsync();
            }
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Request body is required." }));
                return response;
            }

            PhotoCreateDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<PhotoCreateDto>(requestBody, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize request body");
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Invalid JSON format." }));
                return response;
            }

            if (dto == null || string.IsNullOrWhiteSpace(dto.ImageBase64))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Image data is required." }));
                return response;
            }

            // Validate key with backend
            if (string.IsNullOrWhiteSpace(key))
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Unauthorized." }));
                return response;
            }

            try
            {
                var validationUrl = $"{backendApiUrl}/api/Photo/ValidateUploadKey?key={Uri.EscapeDataString(key)}&filmId={Uri.EscapeDataString(dto.FilmId)}";
                var validationResponse = await httpClient.GetAsync(validationUrl);
                
                if (!validationResponse.IsSuccessStatusCode)
                {
                    response.StatusCode = HttpStatusCode.Forbidden;
                    await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Unauthorized." }));
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating upload key");
                response.StatusCode = HttpStatusCode.Forbidden;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Unauthorized." }));
                return response;
            }

            // Check if film exists
            var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(dto.FilmId);
            if (filmEntity == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Film not found." }));
                return response;
            }

            // Calculate photo index
            int photoIndex;
            if (dto.Index.HasValue && dto.Index.Value >= 0 && dto.Index.Value <= 999)
            {
                photoIndex = dto.Index.Value;
            }
            else
            {
                var existingPhotos = await databaseService.GetAllAsync<PhotoEntity>(p => p.FilmId == dto.FilmId);
                photoIndex = existingPhotos.Count != 0 ? existingPhotos.Max(p => p.Index) + 1 : 1;
            }

            // Check if a photo with this index already exists and delete it (to maintain index uniqueness)
            var existingPhoto = await databaseService.GetAllAsync<PhotoEntity>(p =>
                p.FilmId == dto.FilmId && p.Index == photoIndex
            );
            if (existingPhoto.Count > 0)
            {
                var photoToReplace = existingPhoto.First();

                // Delete old image blob
                if (photoToReplace.ImageId != Guid.Empty)
                {
                    await photosContainer.DeleteBlobAsync(photoToReplace.ImageId.ToString());
                    // Delete old preview blob
                    await photosContainer.DeleteBlobAsync($"preview/{photoToReplace.ImageId}");
                }

                // Delete old photo entity
                await databaseService.DeleteAsync(photoToReplace);
            }

            var imageId = Guid.NewGuid();

            try
            {
                // Upload full image
                await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(
                    photosContainer,
                    dto.ImageBase64,
                    imageId
                );

                // Generate and upload preview
                await BlobImageHelper.UploadPreviewImageAsync(
                    photosContainer,
                    dto.ImageBase64,
                    imageId,
                    MaxPreviewDimension,
                    PreviewQuality
                );

                // Create photo entity
                var photoDto = new PhotoDto
                {
                    FilmId = dto.FilmId,
                    Index = photoIndex,
                    ImageBase64 = dto.ImageBase64,
                };

                var entity = photoDto.ToEntity();
                entity.ImageId = imageId;

                await databaseService.AddAsync(entity);

                // Auto-mark film as developed when photo is uploaded
                if (!filmEntity.Developed)
                {
                    filmEntity.Developed = true;
                    await databaseService.UpdateAsync(filmEntity);
                }

                // Return the created entity as DTO
                var createdDto = entity.ToDTO(storageCfg.AccountName);
                var jsonResponse = JsonSerializer.Serialize(createdDto, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await response.WriteStringAsync(jsonResponse);

                _logger.LogInformation($"Photo uploaded successfully: {entity.Id}, ImageId: {imageId}");
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading photo");
                // Clean up uploaded blob if entity creation failed
                await photosContainer.GetBlobClient(imageId.ToString()).DeleteIfExistsAsync();
                await photosContainer.GetBlobClient($"preview/{imageId}").DeleteIfExistsAsync();
                
                response.StatusCode = HttpStatusCode.UnprocessableEntity;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = ex.Message }));
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in PhotoUpload function");
            response.StatusCode = HttpStatusCode.InternalServerError;
            await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "An unexpected error occurred." }));
            return response;
        }
    }
}

