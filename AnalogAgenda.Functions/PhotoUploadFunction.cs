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
            var optionsResponse = req.CreateResponse(HttpStatusCode.OK);
            await optionsResponse.WriteStringAsync(string.Empty);
            return optionsResponse;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", "application/json; charset=utf-8");

        try
        {
            // Deserialize request body
            PhotoBatchUploadDto? batchDto;
            try
            {
                batchDto = await req.ReadFromJsonAsync<PhotoBatchUploadDto>();
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize request body");
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Invalid JSON format." }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading request body");
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Error reading request body." }));
                return response;
            }

            if (batchDto == null || batchDto.Photos == null || batchDto.Photos.Length == 0)
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "At least one photo is required." }));
                return response;
            }

            // Validate all photos have image data
            if (batchDto.Photos.Any(p => string.IsNullOrWhiteSpace(p.ImageBase64)))
            {
                response.StatusCode = HttpStatusCode.BadRequest;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "All photos must have image data." }));
                return response;
            }

            // Validate key with backend
            if (string.IsNullOrWhiteSpace(batchDto.Key) || string.IsNullOrWhiteSpace(batchDto.KeyId))
            {
                response.StatusCode = HttpStatusCode.Forbidden;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Unauthorized." }));
                return response;
            }

            try
            {
                var validationUrl = $"{backendApiUrl}/api/Photo/ValidateUploadKey?key={Uri.EscapeDataString(batchDto.Key)}&keyId={Uri.EscapeDataString(batchDto.KeyId)}&filmId={Uri.EscapeDataString(batchDto.FilmId)}";
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
            var filmEntity = await databaseService.GetByIdAsync<FilmEntity>(batchDto.FilmId);
            if (filmEntity == null)
            {
                response.StatusCode = HttpStatusCode.NotFound;
                await response.WriteStringAsync(JsonSerializer.Serialize(new { error = "Film not found." }));
                return response;
            }

            // Prepare orchestration input
            var orchestrationInput = new PhotoUploadOrchestrationInput
            {
                FilmId = batchDto.FilmId,
                Key = batchDto.Key,
                KeyId = batchDto.KeyId,
                Photos = batchDto.Photos,
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
        logger.LogInformation($"Starting orchestration for {input.Photos.Length} photos");

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

        foreach (var photo in input.Photos)
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

        return new PhotoUploadOrchestrationResult
        {
            TotalPhotos = input.Photos.Length,
            SuccessCount = successCount,
            FailureCount = failureCount,
            Results = results
        };
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
    public required PhotoCreateDto[] Photos { get; set; }
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
