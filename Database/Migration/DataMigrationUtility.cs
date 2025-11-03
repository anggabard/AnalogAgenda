using Azure.Data.Tables;
using Azure.Identity;
using Configuration.Sections;
using Database.Data;
using Database.Entities;
using Database.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Database.DataMigration;

/// <summary>
/// Utility to migrate data from Azure Table Storage to Azure SQL Database
/// </summary>
public class DataMigrationUtility
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly bool _dryRun;

    public DataMigrationUtility(AzureAd azureAdConfig, Storage storageConfig, AnalogAgendaDbContext dbContext, bool dryRun = false)
    {
        var credential = azureAdConfig.GetClientSecretCredential();
        var accountUri = new Uri($"https://{storageConfig.AccountName}.table.core.windows.net");
        _tableServiceClient = new TableServiceClient(accountUri, credential);
        _dbContext = dbContext;
        _dryRun = dryRun;
    }

    public async Task MigrateAllDataAsync()
    {
        Console.WriteLine("Starting data migration from Azure Table Storage to SQL Database...");
        Console.WriteLine($"Mode: {(_dryRun ? "DRY RUN" : "LIVE")}");
        Console.WriteLine();

        try
        {
            // Migrate in order of dependencies
            await MigrateEntitiesAsync<UserEntity>("Users");
            await MigrateEntitiesAsync<DevKitEntity>("DevKits");
            await MigrateEntitiesAsync<UsedDevKitThumbnailEntity>("UsedDevKitThumbnails");
            await MigrateEntitiesAsync<SessionEntity>("Sessions");
            await MigrateEntitiesAsync<FilmEntity>("Films");
            await MigrateEntitiesAsync<UsedFilmThumbnailEntity>("UsedFilmThumbnails");
            await MigrateEntitiesAsync<NoteEntity>("Notes");
            await MigrateEntitiesAsync<NoteEntryEntity>("NotesEntries");
            await MigrateEntitiesAsync<PhotoEntity>("Photos");

            // Migrate many-to-many relationships for Sessions
            await MigrateSessionDevKitRelationshipsAsync();

            if (!_dryRun)
            {
                Console.WriteLine("\nMigration completed successfully!");
            }
            else
            {
                Console.WriteLine("\nDry run completed. No data was written to the database.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nERROR: Migration failed: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private async Task MigrateEntitiesAsync<T>(string tableName) where T : BaseEntity
    {
        Console.WriteLine($"Migrating {tableName}...");
        
        var tableClient = _tableServiceClient.GetTableClient(tableName);
        var entities = new List<T>();

        try
        {
            await foreach (var entity in tableClient.QueryAsync<TableEntity>())
            {
                var migratedEntity = MapTableEntityToModel<T>(entity);
                entities.Add(migratedEntity);
            }

            Console.WriteLine($"  Found {entities.Count} entities in {tableName}");

            if (!_dryRun && entities.Count > 0)
            {
                await _dbContext.Set<T>().AddRangeAsync(entities);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"  ✓ Migrated {entities.Count} entities");
            }
            else if (_dryRun && entities.Count > 0)
            {
                Console.WriteLine($"  [DRY RUN] Would migrate {entities.Count} entities");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR migrating {tableName}: {ex.Message}");
            throw;
        }

        Console.WriteLine();
    }

    private async Task MigrateSessionDevKitRelationshipsAsync()
    {
        Console.WriteLine("Migrating Session-DevKit many-to-many relationships...");
        
        var tableClient = _tableServiceClient.GetTableClient("Sessions");
        var relationshipCount = 0;

        try
        {
            await foreach (var entity in tableClient.QueryAsync<TableEntity>())
            {
                var sessionId = entity.RowKey;
                
                // Get the UsedSubstances JSON array
                if (entity.TryGetValue("UsedSubstances", out var usedSubstancesObj) && usedSubstancesObj != null)
                {
                    var usedSubstancesJson = usedSubstancesObj.ToString();
                    if (!string.IsNullOrEmpty(usedSubstancesJson))
                    {
                        try
                        {
                            var devKitIds = JsonSerializer.Deserialize<List<string>>(usedSubstancesJson);
                            if (devKitIds != null && devKitIds.Count > 0)
                            {
                                if (!_dryRun)
                                {
                                    var session = await _dbContext.Sessions.Include(s => s.UsedDevKits).FirstOrDefaultAsync(s => s.Id == sessionId);
                                    if (session != null)
                                    {
                                        foreach (var devKitId in devKitIds)
                                        {
                                            var devKit = await _dbContext.DevKits.FirstOrDefaultAsync(d => d.Id == devKitId);
                                            if (devKit != null && !session.UsedDevKits.Contains(devKit))
                                            {
                                                session.UsedDevKits.Add(devKit);
                                                relationshipCount++;
                                            }
                                        }
                                        await _dbContext.SaveChangesAsync();
                                    }
                                }
                                else
                                {
                                    relationshipCount += devKitIds.Count;
                                }
                            }
                        }
                        catch (JsonException)
                        {
                            // Skip if JSON is invalid
                        }
                    }
                }
            }

            if (!_dryRun)
            {
                Console.WriteLine($"  ✓ Migrated {relationshipCount} Session-DevKit relationships");
            }
            else
            {
                Console.WriteLine($"  [DRY RUN] Would migrate {relationshipCount} Session-DevKit relationships");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ERROR migrating Session-DevKit relationships: {ex.Message}");
            throw;
        }

        Console.WriteLine();
    }

    private T MapTableEntityToModel<T>(TableEntity tableEntity) where T : BaseEntity
    {
        // Use Activator to create instances since entities have required members
        var entity = (T)Activator.CreateInstance(typeof(T))!;
        
        // Map common properties
        entity.Id = tableEntity.RowKey;
        
        if (tableEntity.TryGetValue("CreatedDate", out var createdDateObj) && createdDateObj != null)
        {
            entity.CreatedDate = DateTime.SpecifyKind(Convert.ToDateTime(createdDateObj), DateTimeKind.Utc);
        }
        
        if (tableEntity.TryGetValue("UpdatedDate", out var updatedDateObj) && updatedDateObj != null)
        {
            entity.UpdatedDate = DateTime.SpecifyKind(Convert.ToDateTime(updatedDateObj), DateTimeKind.Utc);
        }

        // Map specific properties based on type
        MapSpecificProperties(entity, tableEntity);

        return entity;
    }

    private void MapSpecificProperties<T>(T entity, TableEntity tableEntity) where T : BaseEntity
    {
        switch (entity)
        {
            case UserEntity user:
                MapUserEntity(user, tableEntity);
                break;
            case NoteEntity note:
                MapNoteEntity(note, tableEntity);
                break;
            case NoteEntryEntity entry:
                MapNoteEntryEntity(entry, tableEntity);
                break;
            case FilmEntity film:
                MapFilmEntity(film, tableEntity);
                break;
            case PhotoEntity photo:
                MapPhotoEntity(photo, tableEntity);
                break;
            case DevKitEntity devKit:
                MapDevKitEntity(devKit, tableEntity);
                break;
            case SessionEntity session:
                MapSessionEntity(session, tableEntity);
                break;
            case UsedFilmThumbnailEntity filmThumb:
                MapUsedFilmThumbnailEntity(filmThumb, tableEntity);
                break;
            case UsedDevKitThumbnailEntity devKitThumb:
                MapUsedDevKitThumbnailEntity(devKitThumb, tableEntity);
                break;
        }
    }

    private void MapUserEntity(UserEntity entity, TableEntity tableEntity)
    {
        entity.Name = tableEntity.GetString("Name") ?? string.Empty;
        entity.Email = tableEntity.GetString("Email") ?? string.Empty;
        entity.PasswordHash = tableEntity.GetString("PasswordHash") ?? string.Empty;
        entity.IsSubscraibed = tableEntity.GetBoolean("IsSubscraibed") ?? false;
    }

    private void MapNoteEntity(NoteEntity entity, TableEntity tableEntity)
    {
        entity.Name = tableEntity.GetString("Name") ?? string.Empty;
        entity.SideNote = tableEntity.GetString("SideNote") ?? string.Empty;
        
        if (tableEntity.TryGetValue("ImageId", out var imageIdObj) && imageIdObj != null)
        {
            if (Guid.TryParse(imageIdObj.ToString(), out var imageId))
            {
                entity.ImageId = imageId;
            }
        }
    }

    private void MapNoteEntryEntity(NoteEntryEntity entity, TableEntity tableEntity)
    {
        entity.NoteId = tableEntity.GetString("NoteRowKey") ?? string.Empty;
        entity.Time = tableEntity.GetDouble("Time") ?? 0;
        entity.Process = tableEntity.GetString("Process") ?? string.Empty;
        entity.Film = tableEntity.GetString("Film") ?? string.Empty;
        entity.Details = tableEntity.GetString("Details") ?? string.Empty;
    }

    private void MapFilmEntity(FilmEntity entity, TableEntity tableEntity)
    {
        entity.Name = tableEntity.GetString("Name") ?? string.Empty;
        entity.Iso = tableEntity.GetString("Iso") ?? string.Empty;
        entity.Type = (DBObjects.Enums.EFilmType)(tableEntity.GetInt32("Type") ?? 0);
        entity.NumberOfExposures = tableEntity.GetInt32("NumberOfExposures") ?? 36;
        entity.Cost = tableEntity.GetDouble("Cost") ?? 0;
        entity.PurchasedBy = (DBObjects.Enums.EUsernameType)(tableEntity.GetInt32("PurchasedBy") ?? 0);
        
        if (tableEntity.TryGetValue("PurchasedOn", out var purchasedOnObj) && purchasedOnObj != null)
        {
            entity.PurchasedOn = DateTime.SpecifyKind(Convert.ToDateTime(purchasedOnObj), DateTimeKind.Utc);
        }
        
        if (tableEntity.TryGetValue("ImageId", out var imageIdObj) && imageIdObj != null && Guid.TryParse(imageIdObj.ToString(), out var imageId))
        {
            entity.ImageId = imageId;
        }
        
        entity.Description = tableEntity.GetString("Description") ?? string.Empty;
        entity.Developed = tableEntity.GetBoolean("Developed") ?? false;
        entity.DevelopedInSessionId = tableEntity.GetString("DevelopedInSessionRowKey");
        entity.DevelopedWithDevKitId = tableEntity.GetString("DevelopedWithDevKitRowKey");
        entity.ExposureDates = tableEntity.GetString("ExposureDates") ?? string.Empty;
    }

    private void MapPhotoEntity(PhotoEntity entity, TableEntity tableEntity)
    {
        entity.FilmId = tableEntity.GetString("FilmRowId") ?? string.Empty;
        entity.Index = tableEntity.GetInt32("Index") ?? 0;
        
        if (tableEntity.TryGetValue("ImageId", out var imageIdObj) && imageIdObj != null && Guid.TryParse(imageIdObj.ToString(), out var imageId))
        {
            entity.ImageId = imageId;
        }
    }

    private void MapDevKitEntity(DevKitEntity entity, TableEntity tableEntity)
    {
        entity.Name = tableEntity.GetString("Name") ?? string.Empty;
        entity.Url = tableEntity.GetString("Url") ?? string.Empty;
        entity.Type = (DBObjects.Enums.EDevKitType)(tableEntity.GetInt32("Type") ?? 0);
        entity.PurchasedBy = (DBObjects.Enums.EUsernameType)(tableEntity.GetInt32("PurchasedBy") ?? 0);
        
        if (tableEntity.TryGetValue("PurchasedOn", out var purchasedOnObj) && purchasedOnObj != null)
        {
            entity.PurchasedOn = DateTime.SpecifyKind(Convert.ToDateTime(purchasedOnObj), DateTimeKind.Utc);
        }
        
        if (tableEntity.TryGetValue("MixedOn", out var mixedOnObj) && mixedOnObj != null)
        {
            entity.MixedOn = DateTime.SpecifyKind(Convert.ToDateTime(mixedOnObj), DateTimeKind.Utc);
        }
        
        entity.ValidForWeeks = tableEntity.GetInt32("ValidForWeeks") ?? 0;
        entity.ValidForFilms = tableEntity.GetInt32("ValidForFilms") ?? 0;
        entity.FilmsDeveloped = tableEntity.GetInt32("FilmsDeveloped") ?? 0;
        
        if (tableEntity.TryGetValue("ImageId", out var imageIdObj) && imageIdObj != null && Guid.TryParse(imageIdObj.ToString(), out var imageId))
        {
            entity.ImageId = imageId;
        }
        
        entity.Description = tableEntity.GetString("Description") ?? string.Empty;
        entity.Expired = tableEntity.GetBoolean("Expired") ?? false;
    }

    private void MapSessionEntity(SessionEntity entity, TableEntity tableEntity)
    {
        if (tableEntity.TryGetValue("SessionDate", out var sessionDateObj) && sessionDateObj != null)
        {
            entity.SessionDate = DateTime.SpecifyKind(Convert.ToDateTime(sessionDateObj), DateTimeKind.Utc);
        }
        
        entity.Location = tableEntity.GetString("Location") ?? string.Empty;
        entity.Participants = tableEntity.GetString("Participants") ?? string.Empty;
        
        if (tableEntity.TryGetValue("ImageId", out var imageIdObj) && imageIdObj != null && Guid.TryParse(imageIdObj.ToString(), out var imageId))
        {
            entity.ImageId = imageId;
        }
        
        entity.Description = tableEntity.GetString("Description") ?? string.Empty;
        
        // Note: UsedSubstances and DevelopedFilms will be handled by the relationship migration
    }

    private void MapUsedFilmThumbnailEntity(UsedFilmThumbnailEntity entity, TableEntity tableEntity)
    {
        entity.FilmName = tableEntity.GetString("FilmName") ?? string.Empty;
        
        if (tableEntity.TryGetValue("ImageId", out var imageIdObj) && imageIdObj != null && Guid.TryParse(imageIdObj.ToString(), out var imageId))
        {
            entity.ImageId = imageId;
        }
    }

    private void MapUsedDevKitThumbnailEntity(UsedDevKitThumbnailEntity entity, TableEntity tableEntity)
    {
        entity.DevKitName = tableEntity.GetString("DevKitName") ?? string.Empty;
        
        if (tableEntity.TryGetValue("ImageId", out var imageIdObj) && imageIdObj != null && Guid.TryParse(imageIdObj.ToString(), out var imageId))
        {
            entity.ImageId = imageId;
        }
    }
}

