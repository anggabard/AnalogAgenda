using AnalogAgenda.EmailSender.Resources;

namespace AnalogAgenda.EmailSender;

public class EmailTemplateGenerator
{
    public static string GetExpiringDevKit(int daysUntilExpiry, string name, string type, DateTime purchasedOn, string purchasedBy, int remainingFilms, Guid imageId, string rowKey)
    {
        var template = ResourceLoader.GetText(EmbededResources.DevKitExpiringTemplate);

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

    public static string GetExpiredDevKit(string name, string type, DateTime purchasedOn, string purchasedBy, DateTime kitExpirationDate, int remainingFilms, Guid imageId, string rowKey)
    {
        var template = ResourceLoader.GetText(EmbededResources.DevKitExpiredTemplate);

        return template
            .Replace("{{Name}}", name)
            .Replace("{{Type}}", type)
            .Replace("{{PurchasedOn}}", purchasedOn.ToString("MMMM dd, yyyy"))
            .Replace("{{PurchasedBy}}", purchasedBy)
            .Replace("{{ExpirationDate}}", kitExpirationDate.ToString("MMMM dd, yyyy"))
            .Replace("{{RemainingFilms}}", remainingFilms.ToString())
            .Replace("{{ImageId}}", imageId.ToString())
            .Replace("{{RowKey}}", rowKey)
            .Replace("{{Year}}", DateTime.UtcNow.Year.ToString());
    }
}
