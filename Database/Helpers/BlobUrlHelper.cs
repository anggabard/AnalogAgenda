namespace Database.Helpers;

public static class BlobUrlHelper
{
    public static (string AccountName, string ContainerName, Guid ImageId) GetImageInfoFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be null or empty.", nameof(url));

        var uri = new Uri(url);

        var segments = uri.AbsolutePath.Trim('/').Split('/');
        
        bool isLocalAzurite = uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) 
            || uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase);

        string accountName;
        string containerName;
        Guid imageId;

        if (isLocalAzurite)
        {
            if (segments.Length != 3)
                throw new ArgumentException("Invalid URL format. Expected account name, container, and image ID.", nameof(url));

            accountName = segments[0];
            containerName = segments[1];
            
            if (!Guid.TryParse(segments[2], out imageId))
                throw new ArgumentException("Invalid image ID format. Expected a valid GUID.", nameof(url));
        }
        else
        {
            var hostParts = uri.Host.Split('.');
            if (hostParts.Length == 0)
                throw new ArgumentException("Invalid URL format. Could not parse account name.", nameof(url));

            accountName = hostParts[0];

            if (segments.Length != 2)
                throw new ArgumentException("Invalid URL format. Expected container and image ID.", nameof(url));

            containerName = segments[0];

            if (!Guid.TryParse(segments[1], out imageId))
                throw new ArgumentException("Invalid image ID format. Expected a valid GUID.", nameof(url));
        }

        return (accountName, containerName, imageId);
    }
}
