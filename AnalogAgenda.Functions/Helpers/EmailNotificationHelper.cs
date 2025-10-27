using AnalogAgenda.EmailSender;
using Database.Entities;
using Database.Services.Interfaces;

namespace AnalogAgenda.Functions.Helpers;

/// <summary>
/// Helper class for email notification patterns
/// </summary>
public static class EmailNotificationHelper
{
    /// <summary>
    /// Sends an email notification to all subscribers
    /// </summary>
    public static async Task SendNotificationToSubscribersAsync(
        ITableService tablesService,
        IEmailSender emailSender,
        string subject,
        string htmlContent)
    {
        var users = await tablesService.GetTableEntriesAsync<UserEntity>(user => user.IsSubscraibed);
        var receivers = users.Select(userEntity => userEntity.Email);

        if (!receivers.Any())
            throw new Exception("No receivers for email notification!");

        await emailSender.SendEmailAsync(receivers, subject, htmlContent);
    }
}
