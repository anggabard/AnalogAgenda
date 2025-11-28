namespace Database.Helpers;

public static class BlobUrlHelper
{
    public static (string AccountName, string ContainerName, Guid ImageId) GetImageInfoFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        var uri = new Uri(url);

        var hostParts = uri.Host.Split('.');
        if (hostParts.Length == 0)
            throw new ArgumentException("Invalid URL format. Could not parse account name.", nameof(url));

        string accountName = hostParts[0];

        var segments = uri.AbsolutePath.Trim('/').Split('/');
        if (segments.Length != 2)
            throw new ArgumentException("Invalid URL format. Expected container and image ID.", nameof(url));

        string containerName = segments[0];

        if (!Guid.TryParse(segments[1], out var imageId))
            throw new ArgumentException("Invalid image ID format. Expected a valid GUID.", nameof(url));

        return (accountName, containerName, imageId);
    }
}
