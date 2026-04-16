using Azure.Storage.Blobs;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.IO.Compression;
using System.Net;
using System.Text;

namespace AnalogAgenda.Server.Controllers;

/// <summary>Anonymous access to public collections (password + cookie).</summary>
[Route("api/public/collections"), ApiController, AllowAnonymous]
public class PublicCollectionController(
    IDatabaseService databaseService,
    IBlobService blobsService,
    DtoConvertor dtoConvertor,
    IDataProtectionProvider dataProtectionProvider,
    IMemoryCache memoryCache
) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly BlobContainerClient photosContainer = blobsService.GetBlobContainer(ContainerName.photos);
    private readonly DtoConvertor dtoConvertor = dtoConvertor;
    private readonly IDataProtector protector = dataProtectionProvider.CreateProtector("AnalogAgenda.PublicCollection.v1");
    private readonly IMemoryCache memoryCache = memoryCache;

    private const string CookiePrefix = "AACollPub_";
    private const int MaxVerifyAttemptsPerWindow = 20;
    private static readonly TimeSpan VerifyWindow = TimeSpan.FromMinutes(15);

    [HttpGet("{collectionId}")]
    public async Task<IActionResult> GetPage(string collectionId)
    {
        var entity = await databaseService.GetByIdWithQueryAsync<CollectionEntity>(collectionId, q =>
            q.Include(c => c.CollectionPhotoLinks).ThenInclude(l => l.Photo));
        if (entity == null || !entity.IsPublic)
            return NotFound();

        if (!await HasValidAccessCookieAsync(collectionId))
        {
            return Ok(new PublicCollectionPageDto
            {
                RequiresPassword = true,
                Id = entity.Id,
            });
        }

        return Ok(await BuildFullPageDtoAsync(entity, loadComments: true));
    }

    [HttpPost("{collectionId}/verify")]
    public async Task<IActionResult> Verify(string collectionId, [FromBody] CollectionPublicVerifyDto? body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.Password))
            return BadRequest("Password is required.");

        var cacheKey = $"pcv:{GetClientIp()}:{collectionId}";
        memoryCache.TryGetValue(cacheKey, out int attempts);
        if (attempts >= MaxVerifyAttemptsPerWindow)
            return StatusCode(StatusCodes.Status429TooManyRequests, "Too many attempts. Try again later.");

        var entity = await databaseService.GetByIdAsync<CollectionEntity>(collectionId);
        if (entity == null || !entity.IsPublic || string.IsNullOrEmpty(entity.PublicPasswordHash))
            return NotFound();

        var plain = body.Password.Trim();
        if (plain.Length > 32)
            return BadRequest("Password must be at most 32 characters.");

        if (!PasswordHasher.VerifyPassword(plain, entity.PublicPasswordHash))
        {
            memoryCache.Set(cacheKey, attempts + 1, VerifyWindow);
            return StatusCode(StatusCodes.Status401Unauthorized, new { message = "Incorrect password." });
        }

        memoryCache.Remove(cacheKey);

        var payload = $"{collectionId}|{DateTime.UtcNow.AddDays(7):O}|{entity.PublicPasswordHash}";
        var token = protector.Protect(Encoding.UTF8.GetBytes(payload));
        var cookieName = CookiePrefix + WebUtility.UrlEncode(collectionId);
        Response.Cookies.Append(cookieName, Convert.ToBase64String(token), BuildCookieOptions());

        return Ok(new { ok = true });
    }

    [HttpPost("{collectionId}/comments")]
    public async Task<IActionResult> PostComment(string collectionId, [FromBody] CollectionPublicCommentPostDto? body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.AuthorName) || string.IsNullOrWhiteSpace(body.Body))
            return BadRequest("Name and comment are required.");

        var name = body.AuthorName.Trim();
        var text = body.Body.Trim();
        if (name.Length > 100 || text.Length > 2000)
            return BadRequest("Comment exceeds allowed length.");

        if (!await HasValidAccessCookieAsync(collectionId))
            return Unauthorized();

        var collection = await databaseService.GetByIdAsync<CollectionEntity>(collectionId);
        if (collection == null || !collection.IsPublic)
            return NotFound();

        var comment = new CollectionPublicCommentEntity
        {
            CollectionId = collectionId,
            AuthorName = name,
            Body = text,
        };
        comment.Id = comment.GetId();
        await databaseService.AddAsync(comment);

        return Ok(new CollectionPublicCommentDto
        {
            Id = comment.Id,
            AuthorName = comment.AuthorName,
            Body = comment.Body,
            CreatedAt = ToUtcKind(comment.CreatedDate),
        });
    }

    [HttpGet("{collectionId}/download")]
    public async Task<IActionResult> DownloadAll(string collectionId, [FromQuery] bool small = false)
    {
        if (!await HasValidAccessCookieAsync(collectionId))
            return Unauthorized();

        var entity = await databaseService.GetByIdAsync<CollectionEntity>(collectionId);
        if (entity == null || !entity.IsPublic)
            return NotFound();

        var photos = await LoadPublicPhotosOrderedAsync(collectionId);
        if (photos.Count == 0)
            return BadRequest("No photos.");

        return await BuildZipAsync(entity.Name, photos, small, fullArchive: true);
    }

    [HttpPost("{collectionId}/download/selected")]
    public async Task<IActionResult> DownloadSelected(string collectionId, [FromBody] CollectionDownloadSelectedDto? body)
    {
        if (!await HasValidAccessCookieAsync(collectionId))
            return Unauthorized();

        var ids = body?.Ids ?? [];
        if (ids.Count == 0)
            return BadRequest("At least one photo id is required.");

        var entity = await databaseService.GetByIdAsync<CollectionEntity>(collectionId);
        if (entity == null || !entity.IsPublic)
            return NotFound();

        var all = await LoadPublicPhotosOrderedAsync(collectionId);
        var idSet = ids.Select(x => x.Trim()).Where(x => x.Length > 0).ToHashSet();
        var photos = all.Where(p => idSet.Contains(p.Id)).ToList();
        if (photos.Count == 0)
            return BadRequest("No matching photos.");

        return await BuildZipAsync(entity.Name, photos, body!.Small, fullArchive: false);
    }

    /// <summary>Download one full-resolution photo from the collection (requires access cookie).</summary>
    [HttpGet("{collectionId}/photos/{photoId}/download")]
    public async Task<IActionResult> DownloadPhoto(string collectionId, string photoId)
    {
        if (!await HasValidAccessCookieAsync(collectionId))
            return Unauthorized();

        var collection = await databaseService.GetByIdAsync<CollectionEntity>(collectionId);
        if (collection == null || !collection.IsPublic)
            return NotFound();

        var links = await databaseService.GetEntitiesAsync<CollectionPhotoEntity>(cp =>
            cp.CollectionsId == collectionId && cp.PhotosId == photoId);
        if (links.Count == 0)
            return NotFound();

        var photo = await databaseService.GetByIdWithIncludesAsync<PhotoEntity>(photoId, p => p.Film);
        if (photo == null || photo.Restricted)
            return NotFound();

        try
        {
            var base64WithType = await BlobImageHelper.DownloadImageAsBase64WithContentTypeAsync(
                photosContainer,
                photo.ImageId);
            var contentType = BlobImageHelper.GetContentTypeFromBase64(base64WithType);
            var fileExtension = BlobImageHelper.GetFileExtensionFromBase64(base64WithType);
            var film = photo.Film;
            var filmLabel = film != null
                ? SanitizeFileName(
                    !string.IsNullOrWhiteSpace(film.Name?.Trim()) ? $"{film.Name} - {film.Brand}" : film.Brand)
                : "photo";
            var fileName = $"{photo.Index:D3}-{filmLabel}.{fileExtension}";

            var base64Data = base64WithType.Split(',')[1];
            var bytes = Convert.FromBase64String(base64Data);

            return File(bytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            return UnprocessableEntity($"Error downloading photo: {ex.Message}");
        }
    }

    private async Task<bool> HasValidAccessCookieAsync(string collectionId)
    {
        var cookieName = CookiePrefix + WebUtility.UrlEncode(collectionId);
        if (!Request.Cookies.TryGetValue(cookieName, out var raw) || string.IsNullOrEmpty(raw))
            return false;
        try
        {
            var bytes = Convert.FromBase64String(raw);
            var unprotected = protector.Unprotect(bytes);
            var s = Encoding.UTF8.GetString(unprotected);
            var parts = s.Split('|', 3, StringSplitOptions.None);
            if (parts.Length != 3 || parts[0] != collectionId)
                return false;
            if (!DateTime.TryParse(parts[1], null, System.Globalization.DateTimeStyles.RoundtripKind, out var exp))
                return false;
            if (exp <= DateTime.UtcNow)
                return false;

            var entity = await databaseService.GetByIdAsync<CollectionEntity>(collectionId);
            if (entity == null || !entity.IsPublic || string.IsNullOrEmpty(entity.PublicPasswordHash))
                return false;

            return parts[2] == entity.PublicPasswordHash;
        }
        catch
        {
            return false;
        }
    }

    private CookieOptions BuildCookieOptions()
    {
        // SPA (e.g. localhost:4200) → API (e.g. localhost:7125) is cross-site; credentialed requests need
        // SameSite=None, which requires Secure. Without Secure, browsers drop the cookie and GET stays gated.
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            MaxAge = TimeSpan.FromDays(7),
            Path = "/",
        };
    }

    private string GetClientIp()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private async Task<PublicCollectionPageDto> BuildFullPageDtoAsync(CollectionEntity entity, bool loadComments)
    {
        var photoDtos = new List<PhotoDto>();
        foreach (var link in (entity.CollectionPhotoLinks ?? []).OrderBy(l => l.CollectionIndex))
        {
            if (link.Photo == null || link.Photo.Restricted)
                continue;
            photoDtos.Add(dtoConvertor.ToCollectionContextDto(link.Photo, link.CollectionIndex));
        }

        var comments = loadComments
            ? await databaseService.GetAllWithQueryAsync<CollectionPublicCommentEntity>(q =>
                q.Where(c => c.CollectionId == entity.Id).OrderBy(c => c.CreatedDate))
            : [];

        return new PublicCollectionPageDto
        {
            RequiresPassword = false,
            Id = entity.Id,
            Name = entity.Name,
            FromDate = entity.FromDate,
            ToDate = entity.ToDate,
            Location = entity.Location,
            Description = string.IsNullOrWhiteSpace(entity.Description) ? null : entity.Description.Trim(),
            FeaturedImageUrl = null,
            Photos = photoDtos,
            Comments = comments.Select(c => new CollectionPublicCommentDto
            {
                Id = c.Id,
                AuthorName = c.AuthorName,
                Body = c.Body,
                CreatedAt = ToUtcKind(c.CreatedDate),
            }).ToList(),
        };
    }

    /// <summary>EF returns <see cref="DateTimeKind.Unspecified"/> for SQL datetime2; JSON then omits Z and clients mis-parse. Force UTC for API.</summary>
    private static DateTime ToUtcKind(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc);

    private async Task<List<PhotoEntity>> LoadPublicPhotosOrderedAsync(string collectionId)
    {
        var c = await databaseService.GetByIdWithQueryAsync<CollectionEntity>(collectionId, q =>
            q.Include(x => x.CollectionPhotoLinks).ThenInclude(l => l.Photo));
        if (c?.CollectionPhotoLinks == null || c.CollectionPhotoLinks.Count == 0)
            return [];
        return c.CollectionPhotoLinks
            .OrderBy(l => l.CollectionIndex)
            .Select(l => l.Photo)
            .Where(p => p != null && !p.Restricted)
            .Cast<PhotoEntity>()
            .ToList();
    }

    private async Task<IActionResult> BuildZipAsync(string collectionName, List<PhotoEntity> photos, bool small, bool fullArchive)
    {
        var filmIds = photos.Select(p => p.FilmId).Distinct().ToList();
        var films = new Dictionary<string, FilmEntity>();
        foreach (var filmId in filmIds)
        {
            var film = await databaseService.GetByIdAsync<FilmEntity>(filmId);
            if (film != null)
                films[filmId] = film;
        }

        var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.zip");
        FileStream? tempFileStream = null;

        try
        {
            tempFileStream = new FileStream(
                tempFilePath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                4096,
                FileOptions.DeleteOnClose);

            var totalEntries = 0;
            using (var archive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var filmId in filmIds.OrderBy(fid => films.TryGetValue(fid, out var f) ? f.Name : fid))
                {
                    if (!films.TryGetValue(filmId, out var film))
                        continue;

                    var rollPhotos = photos.Where(p => p.FilmId == filmId).OrderBy(p => p.Index).ToList();
                    if (rollPhotos.Count == 0)
                        continue;

                    var folder = SanitizeFolderName(FilmFolderLabel(film));
                    foreach (var photo in rollPhotos)
                    {
                        var blobPath = small ? $"preview/{photo.ImageId}" : photo.ImageId.ToString();
                        var contentType = await BlobImageHelper.GetBlobContentTypeAsync(photosContainer, blobPath);
                        var fileExtension = BlobImageHelper.GetFileExtensionFromContentType(contentType);
                        var fileName = $"{photo.Index:D3}.{fileExtension}";
                        var entryPath = $"{folder}/{fileName}";
                        var zipEntry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
                        await using var zipStream = zipEntry.Open();
                        await BlobImageHelper.CopyBlobToAsync(photosContainer, blobPath, zipStream);
                        totalEntries++;
                    }
                }
            }

            if (totalEntries == 0)
            {
                tempFileStream.Dispose();
                return NotFound("No photos found.");
            }

            tempFileStream.Position = 0;
            var suffix = fullArchive
                ? $"-collection{(small ? "-small" : "")}"
                : $"-collection-selected{(small ? "-small" : "")}";
            var archiveName = $"{SanitizeFileName(collectionName)}{suffix}.zip";
            return File(tempFileStream, "application/zip", archiveName);
        }
        catch
        {
            tempFileStream?.Dispose();
            throw;
        }
    }

    private static string FilmFolderLabel(FilmEntity film)
    {
        var name = film.Name?.Trim();
        if (!string.IsNullOrEmpty(name))
            return $"{name} - {film.Brand}";
        return film.Brand;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "collection" : sanitized;
    }

    private static string SanitizeFolderName(string name)
    {
        var s = SanitizeFileName(name);
        return s.Length > 120 ? s[..120] : s;
    }
}
