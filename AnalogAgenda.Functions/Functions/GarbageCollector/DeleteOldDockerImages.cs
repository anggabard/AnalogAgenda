using AnalogAgenda.Functions.Constants;
using Azure.Containers.ContainerRegistry;
using Configuration.Sections;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace AnalogAgenda.Functions.Functions.GarbageCollector;

public class DeleteOldDockerImages(ILoggerFactory loggerFactory, 
    IContainerRegistryService containerRegistryService, 
    ContainerRegistry containerRegistryConfig)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<DeleteOldDockerImages>();
    private readonly IContainerRegistryService _containerRegistryService = containerRegistryService;
    private readonly ContainerRegistry _containerRegistryConfig = containerRegistryConfig;

    [Function("GarbageCollector-DeleteOldDockerImages")]
    public async Task Run([TimerTrigger(TimeTriggers.Every7DaysAt7AM)] TimerInfo myTimer)
    {
        _logger.LogInformation($"GarbageCollector - DeleteOldDockerImages function executed at: {DateTime.UtcNow}");

        var cleanupTasks = _containerRegistryConfig.RepositoryNames.Select(DeleteOldDockerImagesForRepository);

        await Task.WhenAll(cleanupTasks);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
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
