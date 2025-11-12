using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;
using System.Text.RegularExpressions;

namespace Database.Helpers;

public static class BlobImageHelper
{
    public static async Task UploadBase64ImageWithContentTypeAsync(BlobContainerClient blobContainerClient, string base64Image, Guid blobName)
    {
        (var imageBytes, var contentType) = ParseBase64Image(base64Image);

        var blobClient = blobContainerClient.GetBlobClient(blobName.ToString());

        // Use using to ensure MemoryStream is disposed immediately after upload
        using var stream = new MemoryStream(imageBytes, writable: false);

        var headers = new BlobHttpHeaders
        {
            ContentType = contentType
        };

        await blobClient.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = headers
        });
        
        // Clear reference to help GC (though it will be disposed by using)
        imageBytes = null;
    }

    public static async Task UploadPreviewImageAsync(
        BlobContainerClient blobContainerClient, 
        string base64Image, 
        Guid blobName,
        int maxDimension = 1200,
        int quality = 80)
    {
        (var imageBytes, var _) = ParseBase64Image(base64Image);

        // Resize using ImageSharp - dispose input stream immediately
        byte[]? previewBytes = null;
        using (var inputStream = new MemoryStream(imageBytes, writable: false))
        using (var image = await Image.LoadAsync(inputStream))
        {
            // Calculate new dimensions maintaining aspect ratio
            var width = image.Width;
            var height = image.Height;
            
            if (width > maxDimension || height > maxDimension)
            {
                if (width > height)
                {
                    height = (int)((double)height / width * maxDimension);
                    width = maxDimension;
                }
                else
                {
                    width = (int)((double)width / height * maxDimension);
                    height = maxDimension;
                }
                
                image.Mutate(x => x.Resize(width, height));
            }

            // Convert to JPEG with specified quality
            using var outputStream = new MemoryStream();
            await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = quality });
            previewBytes = outputStream.ToArray();
        }
        
        // Clear original image bytes from memory before uploading preview
        imageBytes = null;
        
        // Upload preview to preview subfolder
        var previewBlobName = $"preview/{blobName}";
        var previewBlobClient = blobContainerClient.GetBlobClient(previewBlobName);

        using var previewStream = new MemoryStream(previewBytes, writable: false);
        var headers = new BlobHttpHeaders
        {
            ContentType = "image/jpeg"
        };

        await previewBlobClient.UploadAsync(previewStream, new BlobUploadOptions
        {
            HttpHeaders = headers
        });
        
        // Clear preview bytes from memory
        previewBytes = null;
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

    /// <summary>
    /// Downloads an image from blob storage, resizes it to a preview size, and returns as JPEG bytes with content type
    /// </summary>
    /// <param name="blobContainerClient">The blob container client</param>
    /// <param name="blobName">The blob name (ImageId)</param>
    /// <param name="maxDimension">Maximum width or height in pixels</param>
    /// <param name="quality">JPEG quality (1-100)</param>
    /// <returns>Tuple of (resized image bytes, content type)</returns>
    public static async Task<(byte[] imageBytes, string contentType)> DownloadAndResizeImageAsync(
        BlobContainerClient blobContainerClient, 
        Guid blobName, 
        int maxDimension = 1200, 
        int quality = 80)
    {
        var blobClient = blobContainerClient.GetBlobClient(blobName.ToString());

        if (!await blobClient.ExistsAsync())
            throw new FileNotFoundException($"Blob with name {blobName} does not exist.");

        // Download the full image
        using var memoryStream = new MemoryStream();
        await blobClient.DownloadToAsync(memoryStream);
        memoryStream.Position = 0;

        // Resize using ImageSharp
        using var image = await Image.LoadAsync(memoryStream);
        
        // Calculate new dimensions maintaining aspect ratio
        var width = image.Width;
        var height = image.Height;
        
        if (width > maxDimension || height > maxDimension)
        {
            if (width > height)
            {
                height = (int)((double)height / width * maxDimension);
                width = maxDimension;
            }
            else
            {
                width = (int)((double)width / height * maxDimension);
                height = maxDimension;
            }
            
            image.Mutate(x => x.Resize(width, height));
        }

        // Convert to JPEG with specified quality and return with content type
        using var outputStream = new MemoryStream();
        await image.SaveAsJpegAsync(outputStream, new JpegEncoder { Quality = quality });
        return (outputStream.ToArray(), "image/jpeg");
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
}

