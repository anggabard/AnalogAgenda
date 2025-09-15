using System.Net;
using System.Net.Mail;

namespace AnalogAgenda.EmailSender;

public class EmailSender : IEmailSender
{
    private readonly SmtpClient _smtpClient;
    private readonly string FromEmail;
    private readonly string FromName = "AnalogAgenda";
    public EmailSender(Smtp settings)
    {
        FromEmail = settings.Username;

        _smtpClient = new SmtpClient(settings.Host, settings.Port)
        {
            Credentials = new NetworkCredential(settings.Username, settings.Password),
            UseDefaultCredentials = false,
            EnableSsl = true
        };
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        var mailMessage = GetMailMessage(subject, htmlBody);

        mailMessage.To.Add(to);

        await _smtpClient.SendMailAsync(mailMessage);
    }

    public async Task SendEmailAsync(IEnumerable<string> to, string subject, string htmlBody)
    {
        var mailMessage = GetMailMessage(subject, htmlBody);

        foreach (var email in to)
        {
            mailMessage.To.Add(email);
        }

        await _smtpClient.SendMailAsync(mailMessage);
    }

    private MailMessage GetMailMessage(string subject, string htmlBody)
    {
        return new MailMessage
        {
            From = new MailAddress(FromEmail, FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
    }
}