using AnalogAgenda.EmailSender;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AnalogAgenda.Functions
{
    public class DevKitExpirationChecker(ILoggerFactory loggerFactory, ITableService tablesService, IEmailSender emailSender)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<DevKitExpirationChecker>();

        //On the 1st, 15th of the month at 11:00 AM
        [Function("DevKitExpirationChecker")]
        public async Task Run([TimerTrigger("0 0 11 1,15 * *")] TimerInfo myTimer)
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation($"DevKitExpirationChecker function executed at: {now}");
            var oneMonthLater = now.AddMonths(1);

            var entities = await tablesService.GetTableEntriesAsync<DevKitEntity>(kit => !kit.Expired);
            foreach (var entity in entities)
            {
                var kitExpirationDate = entity.GetExpirationDate();
                if (kitExpirationDate > oneMonthLater) continue;

                var daysLeft = (kitExpirationDate - now).Days;
                _logger.LogInformation($"Development Kit: {entity.Name} will expire in {daysLeft} days");

                var html = EmailTemplateGenerator.GetExpiringDevKit(daysLeft, entity.Name, entity.Type.ToString(), entity.PurchasedOn, entity.PurchasedBy.ToString(), entity.ValidForFilms - entity.FilmsDeveloped, entity.ImageId, entity.RowKey);
                var receivers = (await tablesService.GetTableEntriesAsync<UserEntity>(user => user.IsSubscraibed)).Select(userEntity => userEntity.Email);
                if (!receivers.Any()) throw new Exception("No receivers for DevKit Expiration mail!");

                await emailSender.SendEmailAsync(
                    receivers,
                    "Your Film Development Kit is about to expire!",
                    html
                );
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
