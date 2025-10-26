using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using AnalogAgenda.EmailSender;

namespace AnalogAgenda.Functions;

public class EmailTestFunction
{
    private readonly ILogger<EmailTestFunction> _logger;
    private readonly IEmailSender _emailSender;

    public EmailTestFunction(ILogger<EmailTestFunction> logger, IEmailSender emailSender)
    {
        _logger = logger;
        _emailSender = emailSender;
    }

    [Function("EmailTestFunction")]
    public async Task Run([TimerTrigger("0 * * * * *")] TimerInfo timer)
    {
        _logger.LogInformation($"Email test function executed at: {DateTime.Now}");

        try
        {
            var emailContent = $@"
                <html>
                <body>
                    <h2>Test Email from AnalogAgenda Functions</h2>
                    <p>This is a test email sent every minute to verify the Functions app is working correctly.</p>
                    <p><strong>Timestamp:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
                    <p><strong>Function Name:</strong> EmailTestFunction</p>
                    <p><strong>Timer Schedule:</strong> Every minute</p>
                    <hr>
                    <p><em>This is an automated test email from your AnalogAgenda Functions app.</em></p>
                </body>
                </html>";

            await _emailSender.SendEmailAsync(
                to: "gabrielangel.ardeleanu@gmail.com",
                subject: $"AnalogAgenda Functions Test - {DateTime.Now:HH:mm:ss}",
                htmlBody: emailContent
            );

            _logger.LogInformation("Test email sent successfully to gabrielangel.ardeleanu@gmail.com");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send test email");
            throw;
        }
    }
}
