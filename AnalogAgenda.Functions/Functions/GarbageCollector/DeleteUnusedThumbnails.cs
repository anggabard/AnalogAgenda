using AnalogAgenda.Functions.Constants;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AnalogAgenda.Functions.Functions.GarbageCollector;

public class DeleteUnusedThumbnails(
    ILoggerFactory loggerFactory,
    IDatabaseService databaseService)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<DeleteUnusedThumbnails>();
    private readonly IDatabaseService _databaseService = databaseService;

    [Function("GarbageCollector-DeleteUnusedThumbnails")]
    public async Task Run([TimerTrigger(TimeTriggers.Every7DaysAt7AM)] TimerInfo myTimer)
    {
        _logger.LogInformation($"GarbageCollector - DeleteUnusedThumbnails function executed at: {DateTime.UtcNow}");

        var cleanupTasks = new List<Task>
        {
            DeleteUnusedThumbnailsAsync<UsedFilmThumbnailEntity, FilmEntity>(),
            DeleteUnusedThumbnailsAsync<UsedDevKitThumbnailEntity, DevKitEntity>()
        };

        await Task.WhenAll(cleanupTasks);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }
    }

    public async Task DeleteUnusedThumbnailsAsync<Thumbnail, Entity>() where Thumbnail : BaseEntity, IImageEntity where Entity : BaseEntity, IImageEntity
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
}
