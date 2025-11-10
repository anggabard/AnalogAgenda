using System.Collections.Concurrent;
using AnalogAgenda.Server.Services.Interfaces;

namespace AnalogAgenda.Server.Services.Implementations;

/// <summary>
/// In-memory implementation of IImageCacheService using ConcurrentDictionary.
/// This can be replaced with a Redis-backed implementation in the future.
/// </summary>
public class InMemoryImageCacheService : IImageCacheService
{
    private readonly ConcurrentDictionary<Guid, (byte[] imageBytes, string contentType)> _cache = new();
    private readonly ILogger<InMemoryImageCacheService> _logger;

    public InMemoryImageCacheService(ILogger<InMemoryImageCacheService> logger)
    {
        _logger = logger;
    }

    public bool TryGetPreview(Guid imageId, out (byte[] imageBytes, string contentType)? cachedImage)
    {
        var result = _cache.TryGetValue(imageId, out var image);
        cachedImage = result ? image : null;
        
        if (result)
        {
            _logger.LogDebug("Cache hit for image {ImageId}", imageId);
        }
        else
        {
            _logger.LogDebug("Cache miss for image {ImageId}", imageId);
        }
        
        return result;
    }

    public void SetPreview(Guid imageId, byte[] imageBytes, string contentType)
    {
        _cache[imageId] = (imageBytes, contentType);
        _logger.LogDebug("Cached preview for image {ImageId}, size: {Size} bytes, type: {ContentType}", 
            imageId, imageBytes.Length, contentType);
    }

    public void RemovePreview(Guid imageId)
    {
        _cache.TryRemove(imageId, out _);
        _logger.LogDebug("Removed preview from cache for image {ImageId}", imageId);
    }

    public void ClearAll()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.LogInformation("Cleared all preview cache, removed {Count} items", count);
    }
}

