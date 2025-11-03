using Azure.Containers.ContainerRegistry;
using Configuration.Sections;
using Database.Entities;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AnalogAgenda.Functions;

public class GarbageCollector(
    ILoggerFactory loggerFactory,
    IDatabaseService databaseService,
    IContainerRegistryService containerRegistryService,
    ContainerRegistry containerRegistryConfig)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GarbageCollector>();
    private readonly IDatabaseService _databaseService = databaseService;
    private readonly IContainerRegistryService _containerRegistryService = containerRegistryService;
    private readonly ContainerRegistry _containerRegistryConfig = containerRegistryConfig;

    //Every 7th day of the month at 7:00 AM
    [Function("GarbageCollector")]
    public async Task Run([TimerTrigger("0 0 7 7,14,21,28 * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"GarbageCollector function executed at: {DateTime.UtcNow}");

        var cleanupTasks = new List<Task>
        {
            DeleteUnusedThumbnails<UsedFilmThumbnailEntity, FilmEntity>(),
            DeleteUnusedThumbnails<UsedDevKitThumbnailEntity, DevKitEntity>()
        };

        // Add Docker image cleanup tasks for each repository
        cleanupTasks.AddRange(_containerRegistryConfig.RepositoryNames.Select(DeleteOldDockerImagesForRepository));

        await Task.WhenAll(cleanupTasks);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }
    }

    public async Task DeleteUnusedThumbnails<Thumbnail, Entity>() where Thumbnail : BaseEntity, IImageEntity where Entity : BaseEntity, IImageEntity
    {
        var thumbnails = await _databaseService.GetAllAsync<Thumbnail>();
        var entityTypeName = typeof(Thumbnail).Name;

        foreach (var thumbnail in thumbnails)
        {
            var thumbnailId = thumbnail.ImageId;

            var exists = await _databaseService.ExistsAsync<Entity>(entity => entity.ImageId == thumbnailId);

            if (!exists)
            {
                await _databaseService.DeleteAsync<Thumbnail>(thumbnail.Id);
                _logger.LogInformation($"Deleted unused thumbnail of type: {entityTypeName} with Id: {thumbnail.Id}");
            }
        }
    }

    private async Task DeleteOldDockerImagesForRepository(string repositoryName)
    {
        _logger.LogInformation($"Cleaning up repository: {repositoryName}");

        try
        {
            var repository = _containerRegistryService.GetRepository(repositoryName);

            // Get all manifest properties from the repository
            var manifestList = new List<ArtifactManifestProperties>();

            await foreach (var manifest in repository.GetAllManifestPropertiesAsync())
            {
                manifestList.Add(manifest);
            }

            // Order by last update time descending (newest first)
            var orderedManifests = manifestList
                .OrderByDescending(m => m.LastUpdatedOn)
                .ToList();

            _logger.LogInformation($"Found {orderedManifests.Count} total manifest(s) in repository '{repositoryName}'");

            // Counter for non-latest images
            int keptCount = 0;
            int deletedCount = 0;

            foreach (var manifest in orderedManifests)
            {
                // Check if the manifest has the tag "latest"
                bool hasLatestTag = manifest.Tags?.Any(tag =>
                    tag.Equals("latest", StringComparison.OrdinalIgnoreCase)) ?? false;

                if (hasLatestTag)
                {
                    _logger.LogInformation($"Skipping image with tag 'latest' (digest: {manifest.Digest})");
                    continue;
                }

                // Keep only the last 3 non-latest images
                if (keptCount < 3)
                {
                    var tags = manifest.Tags?.Any() == true ? string.Join(", ", manifest.Tags) : "untagged";
                    _logger.LogInformation($"Keeping image {keptCount + 1}/3: {manifest.Digest} (tags: {tags})");
                    keptCount++;
                }
                else
                {
                    // Delete older images beyond the 3 most recent
                    await _containerRegistryService.DeleteArtifactAsync(repositoryName, manifest.Digest);

                    var tags = manifest.Tags?.Any() == true ? string.Join(", ", manifest.Tags) : "untagged";
                    _logger.LogInformation($"Deleted old image: {manifest.Digest} (tags: {tags})");
                    deletedCount++;
                }
            }

            _logger.LogInformation($"Repository '{repositoryName}' cleanup completed. Kept {keptCount} recent images, deleted {deletedCount} old images (excluding 'latest' tag).");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Exception occurred while cleaning up repository '{repositoryName}': {ex.Message}");
        }
    }
}
