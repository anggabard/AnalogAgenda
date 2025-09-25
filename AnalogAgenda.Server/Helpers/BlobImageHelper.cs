using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text.RegularExpressions;

namespace AnalogAgenda.Server.Helpers;

public static class BlobImageHelper
{
    public static async Task UploadBase64ImageWithContentTypeAsync(BlobContainerClient blobContainerClient, string base64Image, Guid blobName)
    {
        (var imageBytes, var contentType) = ParseBase64Image(base64Image);

        var blobClient = blobContainerClient.GetBlobClient(blobName.ToString());

        using var stream = new MemoryStream(imageBytes);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = headers
        });
    }

    public static async Task<string> DownloadImageAsBase64WithContentTypeAsync(BlobContainerClient blobContainerClient, Guid blobName)
    {
        var blobClient = blobContainerClient.GetBlobClient(blobName.ToString());

        if (!await blobClient.ExistsAsync())
            throw new FileNotFoundException($"Blob with name {blobName} does not exists.");

        var response = await blobClient.DownloadAsync();

        using var memoryStream = new MemoryStream();
        await response.Value.Content.CopyToAsync(memoryStream);
        byte[] imageBytes = memoryStream.ToArray();

        string contentType = response.Value.Details.ContentType ?? "application/octet-stream";

        string base64 = Convert.ToBase64String(imageBytes);
        return $"data:{contentType};base64,{base64}";
    }

    private static (byte[] imageBytes, string contentType) ParseBase64Image(string base64String)
    {
        if (string.IsNullOrWhiteSpace(base64String))
            throw new ArgumentException("Base64 string is null or empty.", nameof(base64String));

        var dataParts = base64String.Split(',');

        if (dataParts.Length != 2)
            throw new FormatException("The base64 string is not in the correct format.");

        var metadata = dataParts[0];
        var base64Data = dataParts[1];

        var match = Regex.Match(metadata, @"data:(?<type>.+?);base64");

        if (!match.Success)
            throw new FormatException("Could not parse content type from base64 string.");

        var contentType = match.Groups["type"].Value;
        var data = Convert.FromBase64String(base64Data);

        return (data, contentType);
    }

    public static string GetContentTypeFromBase64(string base64WithType)
    {
        // Extract content type from "data:image/jpeg;base64,..." format
        var dataPart = base64WithType.Split(',')[0];
        var contentType = dataPart.Split(';')[0].Replace("data:", "");
        return contentType;
    }

    public static string GetFileExtensionFromBase64(string base64WithType)
    {
        var contentType = GetContentTypeFromBase64(base64WithType);
        return contentType switch
        {
            "image/jpeg" => "jpg",
            "image/jpg" => "jpg",
            "image/png" => "png",
            "image/gif" => "gif",
            "image/webp" => "webp",
            "image/bmp" => "bmp",
            "image/tiff" => "tiff",
            _ => "jpg" // Default to jpg for unknown types
        };
    }
}
