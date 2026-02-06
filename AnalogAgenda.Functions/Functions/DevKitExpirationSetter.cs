using AnalogAgenda.EmailSender;
using AnalogAgenda.Functions.Constants;
using AnalogAgenda.Functions.Helpers;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AnalogAgenda.Functions.Functions;

public class DevKitExpirationSetter(ILoggerFactory loggerFactory, IDatabaseService databaseService, IEmailSender emailSender)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<DevKitExpirationSetter>();

    [Function("DevKitExpirationSetter")]
    public async Task Run([TimerTrigger(TimeTriggers.EveryDayAt10AM)] TimerInfo myTimer)
    {
        var now = DateTime.UtcNow;
        _logger.LogInformation($"DevKitExpirationSetter function executed at: {now}");

        var entities = await databaseService.GetAllAsync<DevKitEntity>(kit => !kit.Expired);
        foreach (var entity in entities)
        {
            if (!entity.MixedOn.HasValue) continue;
            var kitExpirationDate = entity.GetExpirationDate();
            if (kitExpirationDate > now && entity.FilmsDeveloped < entity.ValidForFilms) continue;

            entity.Expired = true;
            await databaseService.UpdateAsync(entity);
            _logger.LogInformation($"Development Kit: {entity.Name} has expired");

            var html = EmailTemplateGenerator.GetExpiredDevKit(entity.Name, entity.Type.ToString(), entity.PurchasedOn, entity.PurchasedBy.ToString(), kitExpirationDate, entity.ValidForFilms - entity.FilmsDeveloped, entity.ImageId, entity.Id);

            await EmailNotificationHelper.SendNotificationToSubscribersAsync(
                databaseService,
                emailSender,
                "Your Film Development Kit has expired!",
                html
            );
        }

        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
        }
    }
}