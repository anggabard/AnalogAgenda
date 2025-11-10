namespace AnalogAgenda.Server.Services.Interfaces;

/// <summary>
/// Service for caching preview images. Can be swapped for Redis/IDistributedCache in the future.
/// </summary>
public interface IImageCacheService
{
    /// <summary>
    /// Try to get a cached preview image by its ImageId
    /// </summary>
    /// <param name="imageId">The unique identifier of the image</param>
    /// <param name="cachedImage">The cached image data (bytes and content type) if found</param>
    /// <returns>True if found in cache, false otherwise</returns>
    bool TryGetPreview(Guid imageId, out (byte[] imageBytes, string contentType)? cachedImage);

    /// <summary>
    /// Store a preview image in the cache
    /// </summary>
    /// <param name="imageId">The unique identifier of the image</param>
    /// <param name="imageBytes">The image bytes to cache</param>
    /// <param name="contentType">The MIME type of the image</param>
    void SetPreview(Guid imageId, byte[] imageBytes, string contentType);

    /// <summary>
    /// Remove a preview image from the cache
    /// </summary>
    /// <param name="imageId">The unique identifier of the image</param>
    void RemovePreview(Guid imageId);

    /// <summary>
    /// Clear all cached preview images
    /// </summary>
    void ClearAll();
}

