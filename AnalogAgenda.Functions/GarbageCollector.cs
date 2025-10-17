using Database.Entities;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AnalogAgenda.Functions;

public class GarbageCollector(ILoggerFactory loggerFactory, ITableService tablesService)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<GarbageCollector>();

    //Every 7th day of the month at 7:00 AM
    [Function("GarbageCollector")]
    public async Task Run([TimerTrigger("0 0 7 7,14,21,28 * *")] TimerInfo myTimer)
    {
        _logger.LogInformation($"DevKitExpirationSetter function executed at: {DateTime.UtcNow}");

        var cleanupTasks = new List<Task>
        {
            DeleteUnusedThumbnails<UsedFilmThumbnailEntity, FilmEntity>(),
            DeleteUnusedThumbnails<UsedDevKitThumbnailEntity, DevKitEntity>()
        };

        await Task.WhenAll(cleanupTasks);

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }
    }

    public async Task DeleteUnusedThumbnails<Thumbnail, Entity>() where Thumbnail : BaseEntity, IImageEntity where Entity : BaseEntity, IImageEntity
    {
        var thumbnailsTable = await tablesService.GetTableEntriesAsync<Thumbnail>();
        var thumbnailsTableName = TableService.GetTableName<Thumbnail>();

        foreach (var thumbnail in thumbnailsTable)
        {
            var thumnailId = thumbnail.ImageId;

            var exists = await tablesService.EntryExistsAsync<Entity>(entity => entity.ImageId == thumnailId);

            if (!exists)
            {
                await tablesService.DeleteTableEntryAsync<Thumbnail>(thumbnail.RowKey);
                _logger.LogInformation($"Deleted unused thumbnail from Table: {thumbnailsTableName} with RowKey: {thumbnail.RowKey}");
            }
        }
    }
}
