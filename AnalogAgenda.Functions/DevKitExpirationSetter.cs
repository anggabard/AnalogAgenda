using AnalogAgenda.EmailSender;
using Azure.Data.Tables;
using Database.DBObjects.Enums;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AnalogAgenda.Functions;

public class DevKitExpirationSetter(ILoggerFactory loggerFactory, ITableService tablesService, IEmailSender emailSender)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<DevKitExpirationSetter>();
    private readonly TableClient devKitsTable = tablesService.GetTable(TableName.DevKits);

    //Every day at 10:00 AM
    [Function("DevKitExpirationSetter")]
    public async Task Run([TimerTrigger("0 0 10 * * *")] TimerInfo myTimer)
    {
        var now = DateTime.UtcNow;
        _logger.LogInformation($"DevKitExpirationSetter function executed at: {now}");

        var entities = await tablesService.GetTableEntriesAsync<DevKitEntity>(kit => !kit.Expired);
        foreach (var entity in entities)
        {
            var kitExpirationDate = entity.GetExpirationDate();
            if (kitExpirationDate > now) continue;

            entity.Expired = true;
            await devKitsTable.UpdateEntityAsync(entity, entity.ETag);
            _logger.LogInformation($"Development Kit: {entity.Name} has expired");

            var html = EmailTemplateGenerator.GetExpiredDevKit(entity.Name, entity.Type.ToString(), entity.PurchasedOn, entity.PurchasedBy.ToString(), kitExpirationDate, entity.ValidForFilms - entity.FilmsDeveloped, entity.ImageId, entity.RowKey);
            var receivers = (await tablesService.GetTableEntriesAsync<UserEntity>(user => user.IsSubscraibed)).Select(userEntity => userEntity.Email);
            if (!receivers.Any()) throw new Exception("No receivers for DevKit Expiration mail!");

            await emailSender.SendEmailAsync(
                receivers,
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