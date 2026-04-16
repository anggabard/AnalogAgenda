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
using Microsoft.EntityFrameworkCore;
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
                c => c.CollectionPhotoLinks))
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
            c => c.CollectionPhotoLinks);
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
        var entity = await databaseService.GetByIdWithQueryAsync<CollectionEntity>(id, q =>
            q.Include(c => c.CollectionPhotoLinks).ThenInclude(l => l.Photo));
        if (entity == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, entity))
            return Forbid();

        var list = (entity.CollectionPhotoLinks ?? [])
            .OrderBy(l => l.CollectionIndex)
            .Select(l => dtoConvertor.ToCollectionContextDto(l.Photo, l.CollectionIndex))
            .ToList();
        return Ok(list);
    }

    /// <summary>Suggested share password for UI (matches <see cref="Database.Helpers.IdGenerator"/> rules server-side).</summary>
    [HttpGet("public-password-suggestion")]
    public IActionResult GetPublicPasswordSuggestion()
    {
        return Ok(new { password = IdGenerator.Get(32) });
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

        if (dto.Description != null && dto.Description.Trim().Length > 2000)
            return BadRequest("Description must be at most 2000 characters.");

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
            IsPublic = false,
            Location = dto.Location ?? string.Empty,
            FromDate = dto.FromDate,
            ToDate = dto.ToDate,
            ImageId = cardImageId,
        };
        entity.Update(dto);
        entity.IsOpen = true;
        entity.IsPublic = false;

        await databaseService.AddAsync(entity);
        await SyncCollectionPhotosAsync(entity.Id, dto.PhotoIds ?? [], currentUserEnum);

        var reloaded = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            entity.Id,
            c => c.CollectionPhotoLinks);
        return Created(string.Empty, dtoConvertor.ToDTO(reloaded!));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] CollectionDto dto)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest("Name is required.");

        var entity = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.CollectionPhotoLinks);
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

        if (dto.IsPublic && string.IsNullOrEmpty(entity.PublicPasswordHash) && string.IsNullOrWhiteSpace(dto.PublicPassword))
            return BadRequest("Password is required when making a collection public for the first time.");

        if (!string.IsNullOrWhiteSpace(dto.PublicPassword) && dto.PublicPassword.Trim().Length > 32)
            return BadRequest("Public password must be at most 32 characters.");

        if (dto.Description != null && dto.Description.Trim().Length > 2000)
            return BadRequest("Description must be at most 2000 characters.");

        entity.Update(dto);
        entity.ImageId = ResolveFinalCardImageId(dto.ImageId, resolved);
        ApplyPublicPasswordState(entity, dto);

        await databaseService.UpdateAsync(entity);
        await SyncCollectionPhotosAsync(id, dto.PhotoIds ?? [], currentUserEnum);

        var finalReload = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.CollectionPhotoLinks);
        if (finalReload == null)
            return NotFound();

        return Ok(dtoConvertor.ToDTO(finalReload));
    }

    /// <summary>
    /// Set or rotate the public share password for an already-public collection.
    /// Does not touch photo membership — avoids accidental clears from partial client state.
    /// </summary>
    [HttpPut("{id}/public-password")]
    public async Task<IActionResult> SetPublicPassword(string id, [FromBody] CollectionSetPublicPasswordDto? body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.PublicPassword))
            return BadRequest("Password is required.");

        var pwd = body.PublicPassword.Trim();
        if (pwd.Length > 32)
            return BadRequest("Public password must be at most 32 characters.");

        var entity = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.CollectionPhotoLinks);
        if (entity == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, entity))
            return Forbid();

        if (!entity.IsPublic)
            return BadRequest("Collection must be public to set a share password.");

        entity.PublicPasswordHash = PasswordHasher.HashPassword(pwd);
        await databaseService.UpdateAsync(entity);

        var reloaded = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.CollectionPhotoLinks);
        if (reloaded == null)
            return NotFound();

        return Ok(dtoConvertor.ToDTO(reloaded));
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

        var collection = await databaseService.GetByIdAsync<CollectionEntity>(id);
        if (collection == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, collection))
            return Forbid();

        var currentUser = User.Name();
        if (string.IsNullOrEmpty(currentUser))
            return Unauthorized();
        var owner = currentUser.ToEnum<EUsernameType>();

        var existingLinks = await databaseService.GetEntitiesAsync<CollectionPhotoEntity>(cp => cp.CollectionsId == id);
        var maxIndex = existingLinks.Count == 0 ? 0 : existingLinks.Max(cp => cp.CollectionIndex);
        var existing = existingLinks.Select(cp => cp.PhotosId).ToHashSet();

        var distinctIncoming = DistinctPreserveOrder(ids);
        var missingIncoming = distinctIncoming.Where(photoId => !existing.Contains(photoId)).ToList();

        if (missingIncoming.Count > 0)
        {
            var candidatePhotos = await databaseService.GetAllWhereWithIncludesAsync<PhotoEntity>(
                p => missingIncoming.Contains(p.Id),
                p => p.Film);

            var eligible = candidatePhotos
                .Where(p => p.Film != null && p.Film.PurchasedBy == owner)
                .ToList();

            var orderedNew = BuildOrderedNewPhotosForAppend(distinctIncoming, eligible, existing);
            var newRows = new List<CollectionPhotoEntity>();
            var next = maxIndex;
            foreach (var photo in orderedNew)
            {
                next++;
                newRows.Add(new CollectionPhotoEntity
                {
                    CollectionsId = id,
                    PhotosId = photo.Id,
                    CollectionIndex = next,
                    FilmId = photo.FilmId,
                });
            }

            if (newRows.Count > 0)
                await databaseService.AddEntitiesAsync(newRows);
        }

        var reloaded = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.CollectionPhotoLinks);
        if (reloaded == null)
            return NotFound();

        var photos = await LoadCollectionPhotosOrderedAsync(id);
        if (!IsPlaceholderOrMemberPhotoImageId(reloaded.ImageId, photos))
        {
            reloaded.ImageId = Constants.DefaultCollectionImageId;
            await databaseService.UpdateAsync(reloaded);
            reloaded = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(id, c => c.CollectionPhotoLinks);
        }

        return Ok(dtoConvertor.ToDTO(reloaded!));
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadArchive(string id, [FromQuery] bool small = false)
    {
        var collection = await databaseService.GetByIdAsync<CollectionEntity>(id);
        if (collection == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, collection))
            return Forbid();

        var photos = await LoadCollectionPhotosOrderedAsync(id);
        if (photos.Count == 0)
            return BadRequest("Collection has no photos.");

        return await BuildCollectionZipFileResult(collection.Name, photos, small, fullArchive: true);
    }

    /// <summary>ZIP of a subset of collection photos (same structure as full archive).</summary>
    [HttpPost("{id}/download/selected")]
    public async Task<IActionResult> DownloadSelected(string id, [FromBody] CollectionDownloadSelectedDto? body)
    {
        var ids = body?.Ids ?? [];
        if (ids.Count == 0)
            return BadRequest("At least one photo id is required.");

        var collection = await databaseService.GetByIdAsync<CollectionEntity>(id);
        if (collection == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, collection))
            return Forbid();

        var allOrdered = await LoadCollectionPhotosOrderedAsync(id);
        var idSet = ids.Select(x => x.Trim()).Where(x => x.Length > 0).ToHashSet();
        var photos = allOrdered.Where(p => idSet.Contains(p.Id)).ToList();
        if (photos.Count == 0)
            return BadRequest("No matching photos in this collection.");

        return await BuildCollectionZipFileResult(collection.Name, photos, body!.Small, fullArchive: false);
    }

    /// <summary>Remove photos from collection membership only; reindexes remaining links.</summary>
    [HttpPost("{id}/photos/remove")]
    public async Task<IActionResult> RemovePhotos(string id, [FromBody] IdListDto? body)
    {
        var ids = body?.Ids ?? [];
        if (ids.Count == 0)
            return BadRequest("At least one photo id is required.");

        var collection = await databaseService.GetByIdAsync<CollectionEntity>(id);
        if (collection == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, collection))
            return Forbid();

        var trimmed = ids.Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToHashSet();
        await databaseService.DeleteEntitiesAsync<CollectionPhotoEntity>(cp =>
            cp.CollectionsId == id && trimmed.Contains(cp.PhotosId));

        var remaining = (await databaseService.GetEntitiesAsync<CollectionPhotoEntity>(cp => cp.CollectionsId == id))
            .OrderBy(cp => cp.CollectionIndex)
            .ToList();
        for (var i = 0; i < remaining.Count; i++)
            remaining[i].CollectionIndex = i + 1;
        await databaseService.SaveChangesAsync();

        var reloaded = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(
            id,
            c => c.CollectionPhotoLinks);
        if (reloaded == null)
            return NotFound();

        var photos = await LoadCollectionPhotosOrderedAsync(id);
        if (!IsPlaceholderOrMemberPhotoImageId(reloaded.ImageId, photos))
        {
            reloaded.ImageId = Constants.DefaultCollectionImageId;
            await databaseService.UpdateAsync(reloaded);
            reloaded = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(id, c => c.CollectionPhotoLinks);
        }

        return Ok(dtoConvertor.ToDTO(reloaded!));
    }

    /// <summary>Set collection card image to the blob of an existing member photo.</summary>
    [HttpPost("{id}/featured")]
    public async Task<IActionResult> SetFeaturedPhoto(string id, [FromBody] CollectionSetFeaturedDto? body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.PhotoId))
            return BadRequest("Photo id is required.");

        var entity = await databaseService.GetByIdAsync<CollectionEntity>(id);
        if (entity == null)
            return NotFound();
        if (!FilmOwnerHelper.IsCurrentUserCollectionOwner(User, entity))
            return Forbid();

        var photoId = body.PhotoId.Trim();
        var photos = await LoadCollectionPhotosOrderedAsync(id);
        var photo = photos.FirstOrDefault(p => p.Id == photoId);
        if (photo == null)
            return BadRequest("Photo is not in this collection.");

        entity.ImageId = photo.ImageId;
        await databaseService.UpdateAsync(entity);
        var reloaded = await databaseService.GetByIdWithIncludesAsync<CollectionEntity>(id, c => c.CollectionPhotoLinks);
        return Ok(dtoConvertor.ToDTO(reloaded!));
    }

    private void ApplyPublicPasswordState(CollectionEntity entity, CollectionDto dto)
    {
        if (!dto.IsPublic)
        {
            entity.PublicPasswordHash = null;
            return;
        }

        if (string.IsNullOrWhiteSpace(dto.PublicPassword))
            return;

        entity.PublicPasswordHash = PasswordHasher.HashPassword(dto.PublicPassword.Trim());
    }

    private async Task<IActionResult> BuildCollectionZipFileResult(string collectionName, List<PhotoEntity> photos, bool small, bool fullArchive)
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
        var collection = await databaseService.GetByIdAsync<CollectionEntity>(collectionId);
        if (collection == null)
            return;

        var orderedDistinct = DistinctPreserveOrder(photoIds);

        List<PhotoEntity> orderedOwned = [];
        if (orderedDistinct.Count > 0)
        {
            var photos = await databaseService.GetAllWhereWithIncludesAsync<PhotoEntity>(
                p => orderedDistinct.Contains(p.Id),
                p => p.Film);
            var byId = photos
                .Where(p => p.Film != null && p.Film.PurchasedBy == owner)
                .ToDictionary(p => p.Id);
            foreach (var pid in orderedDistinct)
            {
                if (byId.TryGetValue(pid, out var photo))
                    orderedOwned.Add(photo);
            }
        }

        var newLinks = new List<CollectionPhotoEntity>();
        for (var i = 0; i < orderedOwned.Count; i++)
        {
            var photo = orderedOwned[i];
            newLinks.Add(new CollectionPhotoEntity
            {
                CollectionsId = collectionId,
                PhotosId = photo.Id,
                CollectionIndex = i + 1,
                FilmId = photo.FilmId,
            });
        }

        await databaseService.ReplaceEntitiesAsync<CollectionPhotoEntity>(
            cp => cp.CollectionsId == collectionId,
            newLinks);
    }

    private async Task<List<PhotoEntity>> LoadCollectionPhotosOrderedAsync(string collectionId)
    {
        var c = await databaseService.GetByIdWithQueryAsync<CollectionEntity>(collectionId, q =>
            q.Include(x => x.CollectionPhotoLinks).ThenInclude(l => l.Photo));
        if (c?.CollectionPhotoLinks == null || c.CollectionPhotoLinks.Count == 0)
            return [];
        return c.CollectionPhotoLinks
            .OrderBy(l => l.CollectionIndex)
            .Select(l => l.Photo)
            .ToList();
    }

    /// <summary>Distinct ids preserving first occurrence order in <paramref name="ids"/>.</summary>
    private static List<string> DistinctPreserveOrder(IEnumerable<string> ids)
    {
        var result = new List<string>();
        var seen = new HashSet<string>();
        foreach (var raw in ids)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;
            var t = raw.Trim();
            if (!seen.Add(t))
                continue;
            result.Add(t);
        }

        return result;
    }

    /// <summary>
    /// Cross-film: film groups ordered by first appearance in <paramref name="idsRequestOrder"/>;
    /// within each film, sort by <see cref="PhotoEntity.Index"/> ascending.
    /// </summary>
    private static List<PhotoEntity> BuildOrderedNewPhotosForAppend(
        IReadOnlyList<string> idsRequestOrder,
        IReadOnlyList<PhotoEntity> eligibleOwned,
        HashSet<string> existingIds)
    {
        var eligibleById = eligibleOwned.ToDictionary(p => p.Id);
        var orderedNewIds = new List<string>();
        var seen = new HashSet<string>();
        foreach (var id in idsRequestOrder)
        {
            if (string.IsNullOrWhiteSpace(id))
                continue;
            var t = id.Trim();
            if (existingIds.Contains(t))
                continue;
            if (!eligibleById.ContainsKey(t))
                continue;
            if (!seen.Add(t))
                continue;
            orderedNewIds.Add(t);
        }

        if (orderedNewIds.Count == 0)
            return [];

        var filmFirstPos = new Dictionary<string, int>();
        for (var i = 0; i < orderedNewIds.Count; i++)
        {
            var fid = eligibleById[orderedNewIds[i]].FilmId;
            if (!filmFirstPos.ContainsKey(fid))
                filmFirstPos[fid] = i;
        }

        var filmOrder = filmFirstPos.OrderBy(kv => kv.Value).Select(kv => kv.Key).ToList();
        var result = new List<PhotoEntity>();
        foreach (var fid in filmOrder)
        {
            var batch = orderedNewIds
                .Where(pid => eligibleById[pid].FilmId == fid)
                .Select(pid => eligibleById[pid])
                .OrderBy(p => p.Index)
                .ToList();
            result.AddRange(batch);
        }

        return result;
    }
}
