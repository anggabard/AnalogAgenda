namespace AnalogAgenda.EmailSender;

/// <summary>
/// No-op implementation when SMTP is not configured (e.g. Docker deploy on laptop).
/// </summary>
public sealed class NoOpEmailSender : IEmailSender
{
    public Task SendEmailAsync(string to, string subject, string htmlBody) => Task.CompletedTask;

    public Task SendEmailAsync(IEnumerable<string> to, string subject, string htmlBody) => Task.CompletedTask;
}
