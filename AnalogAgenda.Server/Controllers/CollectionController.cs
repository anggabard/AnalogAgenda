using AnalogAgenda.Server.Helpers;
using AnalogAgenda.Server.Identity;
using Azure.Storage.Blobs;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace AnalogAgenda.Server.Controllers;

[Route("api/[controller]"), ApiController, Authorize]
public class CollectionController(
    IDatabaseService databaseService,
    IBlobService blobsService,
    DtoConvertor dtoConvertor
) : ControllerBase
{
    private readonly IDatabaseService databaseService = databaseService;
    private readonly BlobContainerClient photosContainer = blobsService.GetBlobContainer(ContainerName.photos);
    private readonly DtoConvertor dtoConvertor = dtoConvertor;

    /// <summary>
    /// Current user’s collections: rows with the newest FromDate then nulls
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMine([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();
        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        var mine = (await databaseService.GetAllWhereWithIncludesAsync<CollectionEntity>(
                c => c.Owner == currentUserEnum,
                c => c.Photos))
            .OrderBy(c => c.FromDate == null && c.ToDate == null)
            .ThenByDescending(c => c.FromDate)
            .ToList();

        if (page <= 0)
            return Ok(mine.Select(dtoConvertor.ToDTO));

        var totalCount = mine.Count;
        var pageSizeClamped = pageSize < 1 ? 1 : pageSize;
        var pagedData = mine.Skip((page - 1) * pageSizeClamped).Take(pageSizeClamped).ToList();
        var pagedResults = new PagedResponseDto<CollectionDto>
        {
            Data = pagedData.Select(dtoConvertor.ToDTO).ToList(),
            TotalCount = totalCount,
            PageSize = pageSizeClamped,
            CurrentPage = page,
        };

        return Ok(pagedResults);
    }

    [HttpGet("open")]
    public async Task<IActionResult> GetOpenForAssignment()
    {
        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();
        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        var openCollections = await databaseService.GetAllAsync<CollectionEntity>(
            c => c.Owner == currentUserEnum && c.IsOpen);
        var open = openCollections
            .OrderBy(c => c.Name)
            .Select(dtoConvertor.ToOptionDto)
            .ToList();
        return Ok(open);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var entity = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.Photos);
        if (entity == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, entity))
            return Forbid();
        return Ok(dtoConvertor.ToDTO(entity));
    }

    /// <summary>All photos in the collection (for card image picker UI).</summary>
    [HttpGet("{id}/photos")]
    public async Task<IActionResult> GetPhotos(string id)
    {
        var entity = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(id, c => c.Photos);
        if (entity == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, entity))
            return Forbid();

        var list = entity.Photos
            .OrderBy(p => p.FilmId)
            .ThenBy(p => p.Index)
            .Select(dtoConvertor.ToDTO)
            .ToList();
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CollectionDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();
        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        var resolved = await ResolveValidOwnedPhotosAsync(dto.PhotoIds, currentUserEnum);
        var imageErr = ValidateDtoCardImage(dto.ImageId, resolved);
        if (imageErr != null)
            return BadRequest(imageErr);

        var cardImageId = ResolveFinalCardImageId(dto.ImageId, resolved);

        var entity = new CollectionEntity
        {
            Name = dto.Name.Trim(),
            Owner = currentUserEnum,
            IsOpen = true,
            Location = dto.Location ?? string.Empty,
            FromDate = dto.FromDate,
            ToDate = dto.ToDate,
            ImageId = cardImageId,
        };
        entity.Update(dto);
        entity.IsOpen = true;

        await databaseService.AddAsync(entity);
        await SyncCollectionPhotosAsync(entity.Id, dto.PhotoIds ?? [], currentUserEnum);

        var reloaded = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            entity.Id,
            c => c.Photos);
        return Created(string.Empty, dtoConvertor.ToDTO(reloaded!));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CollectionDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var entity = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.Photos);
        if (entity == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, entity))
            return Forbid();

        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();
        var currentUserEnum = currentUser.ToEnum<EUsernameType>();

        var resolved = await ResolveValidOwnedPhotosAsync(dto.PhotoIds, currentUserEnum);
        var imageErr = ValidateDtoCardImage(dto.ImageId, resolved);
        if (imageErr != null)
            return BadRequest(imageErr);

        entity.Update(dto);
        entity.ImageId = ResolveFinalCardImageId(dto.ImageId, resolved);

        await databaseService.UpdateAsync(entity);
        await SyncCollectionPhotosAsync(id, dto.PhotoIds ?? [], currentUserEnum);

        var finalReload = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.Photos);
        if (finalReload == null)
            return NotFound();

        return Ok(dtoConvertor.ToDTO(finalReload));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var entity = await databaseService.GetByIdAsync<CollectionEntity>(id);
        if (entity == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, entity))
            return Forbid();

        await databaseService.DeleteAsync(entity);
        return NoContent();
    }

    /// <summary>Append photos to a collection without replacing existing membership.</summary>
    [HttpPost("{id}/photos")]
    public async Task<IActionResult> AppendPhotos(string id, [FromBody] IdListDto? body)
    {
        var ids = body?.Ids ?? [];
        if (ids.Count == 0)
            return BadRequest("At least one photo id is required.");

        var collection = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.Photos);
        if (collection == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, collection))
            return Forbid();

        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();
        var owner = currentUser.ToEnum<EUsernameType>();

        var existing = collection.Photos.Select(p => p.Id).ToHashSet();
        var distinctIncoming = ids.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();
        var missingIncoming = distinctIncoming.Where(photoId => !existing.Contains(photoId)).ToList();

        if (missingIncoming.Count > 0)
        {
            var candidatePhotos = await databaseService.GetAllWhereWithIncludesAsync<PhotoEntity>(
                p => missingIncoming.Contains(p.Id),
                p => p.Film);

            foreach (var photo in candidatePhotos.Where(p => p.Film != null && p.Film.PurchasedBy == owner))
            {
                if (existing.Contains(photo.Id))
                    continue;

                collection.Photos.Add(photo);
                existing.Add(photo.Id);
            }
        }

        await databaseService.UpdateAsync(collection);

        var reloaded = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.Photos);
        if (reloaded == null)
            return NotFound();

        var photos = reloaded.Photos.ToList();
        if (!IsPlaceholderOrMemberPhotoImageId(reloaded.ImageId, photos))
        {
            reloaded.ImageId = Constants.DefaultCollectionImageId;
            await databaseService.UpdateAsync(reloaded);
            reloaded = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(id, c => c.Photos);
        }

        return Ok(dtoConvertor.ToDTO(reloaded!));
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadArchive(string id, [FromQuery] bool small = false)
    {
        var collection = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.Photos);
        if (collection == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, collection))
            return Forbid();

        var photos = collection.Photos.OrderBy(p => p.FilmId).ThenBy(p => p.Index).ToList();
        if (photos.Count == 0)
            return BadRequest("Collection has no photos.");

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
                return NotFound("No photos found in this collection.");
            }

            tempFileStream.Position = 0;
            var archiveName = $"{SanitizeFileName(collection.Name)}-collection{(small ? "-small" : "")}.zip";
            return File(tempFileStream, "application/zip", archiveName);
        }
        catch
        {
            tempFileStream?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Resolves which photos would be linked for the given ids (same rules as sync: owner, exists).
    /// Invalid ids are skipped — they are never added to the collection.
    /// </summary>
    private async Task<List<PhotoEntity>> ResolveValidOwnedPhotosAsync(IEnumerable<string>? photoIds, EUsernameType owner)
    {
        var requestedPhotoIds = (photoIds ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToHashSet();

        if (requestedPhotoIds.Count == 0)
            return [];

        return await databaseService.GetAllWhereWithIncludesAsync<PhotoEntity>(
            photo => requestedPhotoIds.Contains(photo.Id)
                && photo.Film != null
                && photo.Film.PurchasedBy == owner,
            photo => photo.Film);
    }

    /// <summary>
    /// Card image must be the placeholder (<see cref="Constants.DefaultCollectionImageId"/>) or the blob id of a photo that will be in the collection.
    /// </summary>
    private static string? ValidateDtoCardImage(string? dtoImageId, IReadOnlyList<PhotoEntity> resolvedPhotos)
    {
        if (string.IsNullOrWhiteSpace(dtoImageId))
            return null;

        if (!Guid.TryParse(dtoImageId.Trim(), out var g) || g == Guid.Empty)
            return "Invalid collection image id.";

        if (g == Constants.DefaultCollectionImageId)
            return null;

        if (!resolvedPhotos.Any(p => p.ImageId == g))
            return "Collection card image must be the default placeholder or match a photo in the collection.";

        return null;
    }

    /// <summary>Placeholder until the user picks a real frame; or a member photo blob id after validation.</summary>
    private static Guid ResolveFinalCardImageId(string? dtoImageId, IReadOnlyList<PhotoEntity> resolvedPhotos)
    {
        if (string.IsNullOrWhiteSpace(dtoImageId) || !Guid.TryParse(dtoImageId.Trim(), out var g) || g == Guid.Empty)
            return Constants.DefaultCollectionImageId;

        if (g == Constants.DefaultCollectionImageId)
            return Constants.DefaultCollectionImageId;

        var match = resolvedPhotos.FirstOrDefault(p => p.ImageId == g);
        return match != null ? match.ImageId : Constants.DefaultCollectionImageId;
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

    /// <summary>Placeholder guid, or a blob id that belongs to a photo still in the collection.</summary>
    private static bool IsPlaceholderOrMemberPhotoImageId(Guid imageId, IReadOnlyCollection<PhotoEntity> photos)
    {
        if (imageId == Guid.Empty)
            return false;
        if (imageId == Constants.DefaultCollectionImageId)
            return true;
        return photos.Any(p => p.ImageId == imageId);
    }

    private async Task SyncCollectionPhotosAsync(string collectionId, List<string> photoIds, EUsernameType owner)
    {
        var distinctIds = photoIds.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList();

        var collection = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            collectionId,
            c => c.Photos);
        if (collection == null)
            return;

        collection.Photos.Clear();

        if (distinctIds.Count > 0)
        {
            var photos = await databaseService.GetAllWhereWithIncludesAsync<PhotoEntity>(
                p => distinctIds.Contains(p.Id),
                p => p.Film);

            foreach (var photo in photos.Where(p => p.Film != null && p.Film.PurchasedBy == owner))
            {
                collection.Photos.Add(photo);
            }
        }

        await databaseService.UpdateAsync(collection);
    }
}
