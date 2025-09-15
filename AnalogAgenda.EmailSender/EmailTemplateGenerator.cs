namespace AnalogAgenda.EmailSender;

public class EmailTemplateGenerator
{
    public static string GetExpiringDevKit(int daysUntilExpiry, string name, string type, DateTime purchasedOn, string purchasedBy, int remainingFilms, Guid imageId, string rowKey)
    {
        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "EmailTemplates", "DevKitExpiringTemplate.html");
        string template = File.ReadAllText(templatePath);

        return template
            .Replace("{{Name}}", name)
            .Replace("{{Type}}", type)
            .Replace("{{PurchasedOn}}", purchasedOn.ToString("MMMM dd, yyyy"))
            .Replace("{{PurchasedBy}}", purchasedBy)
            .Replace("{{RemainingFilms}}", remainingFilms.ToString())
            .Replace("{{ImageId}}", imageId.ToString())
            .Replace("{{RowKey}}", rowKey)
            .Replace("{{DaysUntilExpiry}}", daysUntilExpiry.ToString())
            .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());
    }
}
