using AnalogAgenda.Functions.Constants;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Database.DBObjects.Enums;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AnalogAgenda.Functions.Functions.GarbageCollector;

public class DeleteOrphanPhotos(
    ILoggerFactory loggerFactory,
    IDatabaseService databaseService,
    IBlobService blobService)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<DeleteOrphanPhotos>();
    private readonly IDatabaseService _databaseService = databaseService;
    private readonly BlobContainerClient _photosContainer = blobService.GetBlobContainer(ContainerName.photos);

    [Function("GarbageCollector-DeleteOrphanPhotos")]
    public async Task Run([TimerTrigger(TimeTriggers.Every7DaysAt7AM)] TimerInfo myTimer)
    {
        _logger.LogInformation($"GarbageCollector - DeleteOrphanPhotos function executed at: {DateTime.UtcNow}");

        try
        {
            // Get all PhotoEntity records from database and extract ImageIds
            var photos = await _databaseService.GetAllAsync<PhotoEntity>();
            var validImageIds = new HashSet<Guid>(photos.Where(p => p.ImageId != Guid.Empty).Select(p => p.ImageId));
            
            _logger.LogInformation($"Found {validImageIds.Count} valid photo ImageIds in database");

            // Count main photo blobs (excluding preview/ subfolder) during enumeration
            var mainPhotoBlobs = new List<BlobItem>();
            await foreach (var blobItem in _photosContainer.GetBlobsAsync())
            {
                // Exclude preview/ blobs during enumeration
                if (blobItem.Name.StartsWith("preview/", StringComparison.OrdinalIgnoreCase))
                    continue;

                mainPhotoBlobs.Add(blobItem);
            }

            _logger.LogInformation($"Found {mainPhotoBlobs.Count} main photo blobs (excluding previews)");

            // Early exit: if database count matches main blob count, no cleanup needed
            if (validImageIds.Count == mainPhotoBlobs.Count)
            {
                _logger.LogInformation($"Database entries ({validImageIds.Count}) match main blob count ({mainPhotoBlobs.Count}). No cleanup needed.");
                
                if (myTimer.ScheduleStatus is not null)
                {
                    _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
                }
                return;
            }

            int deletedCount = 0;

            foreach (var blobItem in mainPhotoBlobs)
            {
                try
                {
                    // Parse blob name as Guid (ImageId)
                    if (!Guid.TryParse(blobItem.Name, out var imageId))
                    {
                        // Blob name is not a valid Guid - delete it and its preview
                        _logger.LogWarning($"Blob name '{blobItem.Name}' is not a valid Guid. Deleting orphaned blob and its preview.");
                        
                        await DeleteBlobAndPreviewAsync(blobItem.Name);
                        deletedCount++;
                        continue;
                    }

                    // Check if ImageId exists in database HashSet
                    if (!validImageIds.Contains(imageId))
                    {
                        // Orphaned blob - delete it and its preview
                        _logger.LogInformation($"Found orphaned photo blob: {imageId}. Deleting main blob and preview.");
                        
                        await DeleteBlobAndPreviewAsync(blobItem.Name);
                        deletedCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing blob '{blobItem.Name}': {ex.Message}");
                }
            }

            _logger.LogInformation($"Cleanup completed. Deleted {deletedCount} orphaned blobs.");

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception occurred in DeleteOrphanPhotos: {ex.Message}");
        }
    }

    private async Task DeleteBlobAndPreviewAsync(string blobName)
    {
        try
        {
            // Delete the main blob
            var mainBlobClient = _photosContainer.GetBlobClient(blobName);
            var mainDeleted = await mainBlobClient.DeleteIfExistsAsync();
            
            if (mainDeleted.Value)
            {
                _logger.LogInformation($"Deleted main blob: {blobName}");
            }

            // Delete the preview blob (if it exists)
            var previewBlobName = $"preview/{blobName}";
            var previewBlobClient = _photosContainer.GetBlobClient(previewBlobName);
            var previewDeleted = await previewBlobClient.DeleteIfExistsAsync();
            
            if (previewDeleted.Value)
            {
                _logger.LogInformation($"Deleted preview blob: {previewBlobName}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting blob '{blobName}' or its preview: {ex.Message}");
            throw;
        }
    }
}

