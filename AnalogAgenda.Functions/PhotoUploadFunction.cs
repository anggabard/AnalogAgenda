using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.IO;
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

    [Function("PhotoUploadBatch")]
    public async Task<HttpResponseData> PhotoUploadBatch(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", "options", Route = "photo/upload")] HttpRequestData req,
        [DurableClient] DurableTaskClient client)
    {
        _logger.LogInformation("Photo upload batch HTTP trigger function executed");

        if (req.Method == "OPTIONS")
        {
            return await Helpers.CorsHelper.HandlePreflightRequestAsync(req);
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");
        Helpers.CorsHelper.AddCorsHeaders(response, req);

        try
        {
            // Deserialize request body - read stream manually to avoid stream consumption issues
            PhotoBatchUploadDto? batchDto;
            try
            {
                using var reader = new StreamReader(req.Body);
                var bodyString = await reader.ReadToEndAsync();
                batchDto = JsonSerializer.Deserialize<PhotoBatchUploadDto>(bodyString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize request body");
                response.StatusCode = HttpStatusCode.BadRequest;
                Helpers.CorsHelper.AddCorsHeaders(response, req);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Invalid JSON format." }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading request body");
                response.StatusCode = HttpStatusCode.BadRequest;
                Helpers.CorsHelper.AddCorsHeaders(response, req);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Error reading request body." }));
                return response;
            }

            if (batchDto == null || batchDto.Photos == null || batchDto.Photos.Length == 0)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                Helpers.CorsHelper.AddCorsHeaders(response, req);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "At least one photo is required." }));
                return response;
            }

            // Validate all photos have image data
            if (batchDto.Photos.Any(p => string.IsNullOrWhiteSpace(p.ImageBase64)))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                Helpers.CorsHelper.AddCorsHeaders(response, req);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "All photos must have image data." }));
                return response;
            }

            // Validate key with backend
            if (string.IsNullOrWhiteSpace(batchDto.Key) || string.IsNullOrWhiteSpace(batchDto.KeyId))
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                Helpers.CorsHelper.AddCorsHeaders(response, req);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Unauthorized." }));
                return response;
            }

            try
            {
                var validationUrl = $"{backendApiUrl}/api/Photo/ValidateUploadKey?key={Uri.EscapeDataString(batchDto.Key)}&keyId={Uri.EscapeDataString(batchDto.KeyId)}&filmId={Uri.EscapeDataString(batchDto.FilmId)}";
                
                // Validate that the URL is absolute
                if (!Uri.TryCreate(validationUrl, UriKind.Absolute, out var uri))
                {
                    _logger.LogError($"Invalid validation URL: {validationUrl}");
                    response.StatusCode = HttpStatusCode.InternalServerError;
                    Helpers.CorsHelper.AddCorsHeaders(response, req);
                    await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Invalid backend API configuration." }));
                    return response;
                }
                
                var validationResponse = await httpClient.GetAsync(uri);
                
                if (!validationResponse.IsSuccessStatusCode)
                {
                    response.StatusCode = HttpStatusCode.Forbidden;
                    Helpers.CorsHelper.AddCorsHeaders(response, req);
                    await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Unauthorized." }));
                    return response;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating upload key");
                response.StatusCode = HttpStatusCode.Forbidden;
                Helpers.CorsHelper.AddCorsHeaders(response, req);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Unauthorized." }));
                return response;
            }

            // Check if film exists
            var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(batchDto.FilmId);
            if (filmEntity == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                Helpers.CorsHelper.AddCorsHeaders(response, req);
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Film not found." }));
                return response;
            }

            // Store batch data in blob storage to avoid orchestration input size limits
            // Durable Functions have a ~60KB limit on orchestration inputs, but we're sending ~175MB
            var batchDataBlobId = Guid.NewGuid();
            var batchDataJson = JsonSerializer.Serialize(batchDto, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            // Upload batch data to temporary blob
            var batchDataBlobClient = photosContainer.GetBlobClient($"temp-batch/{batchDataBlobId}");
            using var batchDataStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(batchDataJson));
            await batchDataBlobClient.UploadAsync(batchDataStream, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" }
            });

            // Prepare orchestration input with blob reference instead of full data
            var orchestrationInput = new PhotoUploadOrchestrationInput
            {
                FilmId = batchDto.FilmId,
                Key = batchDto.Key,
                KeyId = batchDto.KeyId,
                BatchDataBlobId = batchDataBlobId.ToString(),
                StorageAccountName = storageCfg.AccountName
            };

            // Start orchestration
            var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(
                nameof(PhotoUploadOrchestrator),
                orchestrationInput);

            _logger.LogInformation($"Started photo upload orchestration with instance ID: {instanceId}");

            // Construct status query URL
            var requestUri = new Uri(req.Url.ToString());
            var baseUrl = $"{requestUri.Scheme}://{requestUri.Authority}";
            var statusQueryGetUri = $"{baseUrl}/runtime/webhooks/durabletask/instances/{instanceId}";

            // Return immediately with instance ID
            var result = new
            {
                instanceId = instanceId,
                statusQueryGetUri = statusQueryGetUri
            };

            var jsonResponse = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            await response.WriteStringAsync(jsonResponse);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in PhotoUploadBatch function");
            response.StatusCode = HttpStatusCode.InternalServerError;
            Helpers.CorsHelper.AddCorsHeaders(response, req);
            await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "An unexpected error occurred." }));
            return response;
        }
    }

    [Function("PhotoUploadOrchestrator")]
    public async Task<PhotoUploadOrchestrationResult> PhotoUploadOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var input = context.GetInput<PhotoUploadOrchestrationInput>() ?? throw new ArgumentNullException(nameof(context));
        
        var logger = context.CreateReplaySafeLogger<PhotoUploadFunction>();
        logger.LogInformation($"Starting orchestration with batch data blob ID: {input.BatchDataBlobId}");

        // Download batch data from blob storage
        var batchDtoTask = context.CallActivityAsync<PhotoBatchUploadDto>(
            nameof(LoadBatchDataActivity),
            input.BatchDataBlobId);

        var batchDto = await batchDtoTask;
        logger.LogInformation($"Loaded batch data with {batchDto.Photos.Length} photos");

        // Get existing photos to calculate indices
        var existingPhotosTask = context.CallActivityAsync<PhotoEntity[]>(
            nameof(GetExistingPhotosActivity),
            input.FilmId);

        var existingPhotos = await existingPhotosTask;

        // Calculate indices for photos that don't have explicit indices
        int nextAvailableIndex = existingPhotos.Length == 0 
            ? 1 
            : existingPhotos.Max(p => p.Index) + 1;

        // Prepare activity inputs with calculated indices
        var activityInputs = new List<PhotoUploadActivityInput>();
        int currentAutoIndex = nextAvailableIndex;

        foreach (var photo in batchDto.Photos)
        {
            int photoIndex;
            if (photo.Index.HasValue && photo.Index.Value >= 0 && photo.Index.Value <= 999)
            {
                photoIndex = photo.Index.Value;
            }
            else
            {
                photoIndex = currentAutoIndex++;
            }

            activityInputs.Add(new PhotoUploadActivityInput
            {
                FilmId = input.FilmId,
                ImageBase64 = photo.ImageBase64,
                Index = photoIndex,
                StorageAccountName = input.StorageAccountName
            });
        }

        // Process all photos in parallel
        var tasks = activityInputs.Select(input => 
            context.CallActivityAsync<PhotoUploadActivityResult>(
                nameof(PhotoUploadActivity),
                input));

        var results = await Task.WhenAll(tasks);

        // Count successes and failures
        var successCount = results.Count(r => r.Success);
        var failureCount = results.Count(r => !r.Success);

        logger.LogInformation($"Orchestration completed: {successCount} successful, {failureCount} failed");

        // Clean up temporary batch data blob
        await context.CallActivityAsync(
            nameof(CleanupBatchDataActivity),
            input.BatchDataBlobId);

        return new PhotoUploadOrchestrationResult
        {
            TotalPhotos = batchDto.Photos.Length,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results
        };
    }

    [Function("LoadBatchDataActivity")]
    public async Task<PhotoBatchUploadDto> LoadBatchDataActivity(
        [ActivityTrigger] string batchDataBlobId,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger<PhotoUploadFunction>();
        logger.LogInformation($"Loading batch data from blob: {batchDataBlobId}");

        // Download batch data from blob storage
        var batchDataBlobClient = photosContainer.GetBlobClient($"temp-batch/{batchDataBlobId}");
        
        if (!await batchDataBlobClient.ExistsAsync())
        {
            throw new FileNotFoundException($"Batch data blob not found: {batchDataBlobId}");
        }

        var downloadResponse = await batchDataBlobClient.DownloadAsync();
        using var memoryStream = new MemoryStream();
        await downloadResponse.Value.Content.CopyToAsync(memoryStream);
        memoryStream.Position = 0;
        
        var jsonString = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
        var batchDto = JsonSerializer.Deserialize<PhotoBatchUploadDto>(jsonString, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (batchDto == null)
        {
            throw new InvalidOperationException("Failed to deserialize batch data");
        }

        return batchDto;
    }

    [Function("CleanupBatchDataActivity")]
    public async Task CleanupBatchDataActivity(
        [ActivityTrigger] string batchDataBlobId,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger<PhotoUploadFunction>();
        logger.LogInformation($"Cleaning up batch data blob: {batchDataBlobId}");

        try
        {
            var batchDataBlobClient = photosContainer.GetBlobClient($"temp-batch/{batchDataBlobId}");
            await batchDataBlobClient.DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, $"Failed to cleanup batch data blob: {batchDataBlobId}");
            // Don't throw - cleanup failures shouldn't fail the orchestration
        }
    }

    [Function("GetExistingPhotosActivity")]
    public async Task<PhotoEntity[]> GetExistingPhotosActivity(
        [ActivityTrigger] string filmId,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger<PhotoUploadFunction>();
        logger.LogInformation($"Getting existing photos for film: {filmId}");

        var existingPhotos = await databaseService.GetAllAsync<PhotoEntity>(p => p.FilmId == filmId);
        return existingPhotos.ToArray();
    }

    [Function("PhotoUploadActivity")]
    public async Task<PhotoUploadActivityResult> PhotoUploadActivity(
        [ActivityTrigger] PhotoUploadActivityInput input,
        FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger<PhotoUploadFunction>();
        logger.LogInformation($"Processing photo upload for film {input.FilmId}, index {input.Index}");

        try
        {
            // Check if a photo with this index already exists and delete it
            var existingPhotos = await databaseService.GetAllAsync<PhotoEntity>(p =>
                p.FilmId == input.FilmId && p.Index == input.Index
            );

            if (existingPhotos.Count > 0)
            {
                var photoToReplace = existingPhotos.First();
                logger.LogInformation($"Replacing existing photo at index {input.Index}");

                // Delete old image blob
                if (photoToReplace.ImageId != Guid.Empty)
                {
                    await photosContainer.GetBlobClient(photoToReplace.ImageId.ToString()).DeleteIfExistsAsync();
                    // Delete old preview blob
                    await photosContainer.GetBlobClient($"preview/{photoToReplace.ImageId}").DeleteIfExistsAsync();
                }

                // Delete old photo entity
                await databaseService.DeleteAsync(photoToReplace);
            }

            var imageId = Guid.NewGuid();

            // Upload full image
            await BlobImageHelper.UploadBase64ImageWithContentTypeAsync(
                photosContainer,
                input.ImageBase64,
                imageId
            );

            // Generate and upload preview
            await BlobImageHelper.UploadPreviewImageAsync(
                photosContainer,
                input.ImageBase64,
                imageId,
                MaxPreviewDimension,
                PreviewQuality
            );

            // Create photo entity
            var photoDto = new PhotoDto
            {
                FilmId = input.FilmId,
                Index = input.Index,
                ImageBase64 = input.ImageBase64,
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

            logger.LogInformation($"Photo uploaded successfully: {entity.Id}, ImageId: {imageId}");

            var createdDto = entity.ToDTO(input.StorageAccountName);
            return new PhotoUploadActivityResult
            {
                Success = true,
                Photo = createdDto,
                ErrorMessage = null
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error uploading photo at index {input.Index}");
            
            // Clean up uploaded blob if entity creation failed
            // Note: imageId might not be set if error occurred before upload
            try
            {
                // We can't easily track the imageId here if upload failed, so we'll just log
                // In a production scenario, you might want to track this differently
            }
            catch
            {
                // Ignore cleanup errors
            }

            return new PhotoUploadActivityResult
            {
                Success = false,
                Photo = null,
                ErrorMessage = ex.Message
            };
        }
    }
}

// DTOs for orchestration
public class PhotoUploadOrchestrationInput
{
    public required string FilmId { get; set; }
    public required string Key { get; set; }
    public required string KeyId { get; set; }
    public required string BatchDataBlobId { get; set; } // Reference to blob containing PhotoBatchUploadDto
    public required string StorageAccountName { get; set; }
}

public class PhotoUploadOrchestrationResult
{
    public int TotalPhotos { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public required PhotoUploadActivityResult[] Results { get; set; }
}

public class PhotoUploadActivityInput
{
    public required string FilmId { get; set; }
    public required string ImageBase64 { get; set; }
    public required int Index { get; set; }
    public required string StorageAccountName { get; set; }
}

public class PhotoUploadActivityResult
{
    public bool Success { get; set; }
    public PhotoDto? Photo { get; set; }
    public string? ErrorMessage { get; set; }
}
