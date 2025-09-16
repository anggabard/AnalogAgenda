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

        //At 09:00 AM, only on Monday
        [Function("DevKitExpirationChecker")]
        public async Task Run([TimerTrigger("* * * * *")] TimerInfo myTimer)
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation($"C# Timer trigger function executed at: {now}");
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
                    "Your C41 Dev Kit is about to expire!",
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
