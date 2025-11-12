using System.Net;
using System.Text.Json;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask.Client;
using Microsoft.DurableTask.Entities;
using Microsoft.Extensions.Logging;

namespace AnalogAgenda.Functions.DurableFunction.PhotoUpload;

public class PhotoUploadFunction(
    ILoggerFactory loggerFactory,
    IDatabaseService databaseService,
    IBlobService blobService,
    Storage storageCfg,
    Security securityCfg,
    HttpClient httpClient)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<PhotoUploadFunction>();
    private readonly BlobContainerClient photosContainer = blobService.GetBlobContainer(
        ContainerName.photos
    );
    private readonly string backendApiUrl =
        securityCfg.BackendApiUrl
        ?? throw new ArgumentNullException(nameof(securityCfg.BackendApiUrl));
    private const int MaxPreviewDimension = 1200;
    private const int PreviewQuality = 80;

    [Function("PhotoUpload")]
    public async Task<HttpResponseData> PhotoUpload(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "photo/upload")]
            HttpRequestData req,
        [DurableClient] DurableTaskClient client
    )
    {
        _logger.LogInformation("Photo upload HTTP trigger function executed");

        if (req.Method == "OPTIONS")
        {
            return await Helpers.CorsHelper.HandlePreflightRequestAsync(req);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        Helpers.CorsHelper.AddCorsHeaders(response, req);

        try
        {
            // Read Key and KeyId from query parameters
            var query = QueryHelpers.ParseQuery(req.Url.Query);
            var key = query.ContainsKey("Key") ? query["Key"].FirstOrDefault() : null;
            var keyId = query.ContainsKey("KeyId") ? query["KeyId"].FirstOrDefault() : null;

            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(keyId))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                Helpers.CorsHelper.AddCorsHeaders(response, req);
                await response.WriteStringAsync(
                    JsonSerializer.Serialize(new { error = "Key and KeyId are required." })
                );
                return response;
            }

            // Deserialize request body - single photo
            PhotoCreateDto? photoDto;
            try
            {
                using var reader = new StreamReader(req.Body);
                var bodyString = await reader.ReadToEndAsync();
                photoDto = JsonSerializer.Deserialize<PhotoCreateDto>(
                    bodyString,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize request body");
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(
                    JsonSerializer.Serialize(new { error = "Invalid JSON format." })
                );
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading request body");
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(
                    JsonSerializer.Serialize(new { error = "Error reading request body." })
                );
                return response;
            }

            if (photoDto == null || string.IsNullOrWhiteSpace(photoDto.ImageBase64))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(
                    JsonSerializer.Serialize(new { error = "Photo data is required." })
                );
                return response;
            }

            // Validate key with backend
            try
            {
                var validationUrl =
                    $"{backendApiUrl}/api/Photo/ValidateUploadKey?key={Uri.EscapeDataString(key)}&keyId={Uri.EscapeDataString(keyId)}&filmId={Uri.EscapeDataString(photoDto.FilmId)}";

                if (!Uri.TryCreate(validationUrl, UriKind.Absolute, out var uri))
                {
                    _logger.LogError($"Invalid validation URL: {validationUrl}");
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    await response.WriteStringAsync(
                        JsonSerializer.Serialize(
                            new { error = "Invalid backend API configuration." }
                        )
                    );
                    return response;
                }

                var validationResponse = await httpClient.GetAsync(uri);

                if (!validationResponse.IsSuccessStatusCode)
                {
                    response.StatusCode = HttpStatusCode.Forbidden;
                    await response.WriteStringAsync(
                        JsonSerializer.Serialize(new { error = "Unauthorized." })
                    );
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating upload key");
                response.StatusCode = HttpStatusCode.Forbidden;
                await response.WriteStringAsync(
                    JsonSerializer.Serialize(new { error = "Unauthorized." })
                );
                return response;
            }

            // Check if film exists
            var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(photoDto.FilmId);
            if (filmEntity == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync(
                    JsonSerializer.Serialize(new { error = "Film not found." })
                );
                return response;
            }

            // Process photo immediately
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

            // Process photo using activity function
            var activityInput = new PhotoUploadActivityInput
            {
                FilmId = photoDto.FilmId,
                ImageBase64 = photoDto.ImageBase64,
                Index = photoIndex,
                StorageAccountName = storageCfg.AccountName,
            };

            // Call activity function to process the photo
            var activityResult = await ProcessPhotoUploadAsync(activityInput);

            // Clear base64 from memory as soon as possible to reduce memory pressure
            // Note: Setting to empty string helps GC, but can't set to null due to required property
            photoDto.ImageBase64 = string.Empty;
            activityInput.ImageBase64 = string.Empty;

            // Signal entity for tracking/aggregation
            // Entity ID format: entity name and instance key
            var entityId = new EntityInstanceId("PhotoUploadBatchEntity", photoDto.FilmId);
            var entityInput = new RecordPhotoProcessedInput
            {
                Success = activityResult.Success,
                FilmId = photoDto.FilmId,
            };
            await client.Entities.SignalEntityAsync(entityId, "RecordPhotoProcessed", entityInput);

            if (activityResult.Success)
            {
                _logger.LogInformation($"Photo uploaded successfully at index {photoIndex}");
                response.StatusCode = HttpStatusCode.OK;
                var result = new { success = true, photo = activityResult.Photo };
                var jsonResponse = JsonSerializer.Serialize(
                    result,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                );
                await response.WriteStringAsync(jsonResponse);
            }
            else
            {
                _logger.LogError($"Photo upload failed: {activityResult.ErrorMessage}");
                response.StatusCode = HttpStatusCode.InternalServerError;
                var result = new { success = false, error = activityResult.ErrorMessage };
                var jsonResponse = JsonSerializer.Serialize(
                    result,
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                );
                await response.WriteStringAsync(jsonResponse);
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in PhotoUpload function");
            response.StatusCode = HttpStatusCode.InternalServerError;
            Helpers.CorsHelper.AddCorsHeaders(response, req);
            await response.WriteStringAsync(
                JsonSerializer.Serialize(new { error = "An unexpected error occurred." })
            );
            return response;
        }
    }

    [Function("PhotoUploadBatchEntity")]
    public static Task RunEntityAsync([EntityTrigger] TaskEntityDispatcher dispatcher)
    {
        return dispatcher.DispatchAsync<PhotoUploadBatchEntity>();
    }

    // Helper method to process photo upload (extracted from activity for reuse)
    private async Task<PhotoUploadActivityResult> ProcessPhotoUploadAsync(
        PhotoUploadActivityInput input
    )
    {
        try
        {
            // Check if a photo with this index already exists and delete it
            var existingPhotos = await databaseService.GetAllAsync<PhotoEntity>(
                p => p.FilmId == input.FilmId && p.Index == input.Index
            );

            if (existingPhotos.Count > 0)
            {
                var photoToReplace = existingPhotos.First();
                _logger.LogInformation($"Replacing existing photo at index {input.Index}");

                // Delete old image blob
                if (photoToReplace.ImageId != Guid.Empty)
                {
                    await photosContainer
                        .GetBlobClient(photoToReplace.ImageId.ToString())
                        .DeleteIfExistsAsync();
                    // Delete old preview blob
                    await photosContainer
                        .GetBlobClient($"preview/{photoToReplace.ImageId}")
                        .DeleteIfExistsAsync();
                }

                // Delete old photo entity
                await databaseService.DeleteAsync(photoToReplace);
            }

            var imageId = Guid.NewGuid();

            // Store base64 temporarily for processing
            var base64Image = input.ImageBase64;

            // Upload full image
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(
                photosContainer,
                base64Image,
                imageId
            );

            // Generate and upload preview
            await BlobImageHelper.UploadPreviewImageAsync(
                photosContainer,
                base64Image,
                imageId,
                MaxPreviewDimension,
                PreviewQuality
            );

            // Clear base64 from memory immediately after processing to reduce memory pressure
            base64Image = string.Empty;
            input.ImageBase64 = string.Empty;

            // Create photo entity (without base64 - image is already in blob storage)
            var photoDto = new PhotoDto
            {
                FilmId = input.FilmId,
                Index = input.Index,
                ImageBase64 = string.Empty, // Don't store base64 in entity - it's in blob storage
            };

            var entity = photoDto.ToEntity();
            entity.ImageId = imageId;

            await databaseService.AddAsync(entity);

            // Auto-mark film as developed when photo is uploaded
            var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(input.FilmId);
            if (filmEntity != null && !filmEntity.Developed)
            {
                filmEntity.Developed = true;
                await databaseService.UpdateAsync(filmEntity);
            }

            _logger.LogInformation($"Photo uploaded successfully: {entity.Id}, ImageId: {imageId}");

            var createdDto = entity.ToDTO(input.StorageAccountName);
            return new PhotoUploadActivityResult
            {
                Success = true,
                Photo = createdDto,
                ErrorMessage = null,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading photo at index {input.Index}");
            return new PhotoUploadActivityResult
            {
                Success = false,
                Photo = null,
                ErrorMessage = ex.Message,
            };
        }
    }

    [Function("GetExistingPhotosActivity")]
    public async Task<PhotoEntity[]> GetExistingPhotosActivity(
        [ActivityTrigger] string filmId,
        FunctionContext executionContext
    )
    {
        var logger = executionContext.GetLogger<PhotoUploadFunction>();
        logger.LogInformation($"Getting existing photos for film: {filmId}");

        var existingPhotos = await databaseService.GetAllAsync<PhotoEntity>(
            p => p.FilmId == filmId
        );
        return existingPhotos.ToArray();
    }
}