namespace AnalogAgenda.EmailSender;

public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
    Task SendEmailAsync(IEnumerable<string> to, string subject, string htmlBody);
}