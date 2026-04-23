using Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Database.Data;

public class AnalogAgendaDbContext(DbContextOptions<AnalogAgendaDbContext> options) : DbContext(options)
{
    // DbSets for all entities
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<NoteEntity> Notes { get; set; }
    public DbSet<NoteEntryEntity> NoteEntries { get; set; }
    public DbSet<NoteEntryRuleEntity> NoteEntryRules { get; set; }
    public DbSet<NoteEntryOverrideEntity> NoteEntryOverrides { get; set; }
    public DbSet<DevKitEntity> DevKits { get; set; }
    public DbSet<FilmEntity> Films { get; set; }
    public DbSet<PhotoEntity> Photos { get; set; }
    public DbSet<SessionEntity> Sessions { get; set; }
    public DbSet<UsedFilmThumbnailEntity> UsedFilmThumbnails { get; set; }
    public DbSet<UsedDevKitThumbnailEntity> UsedDevKitThumbnails { get; set; }
    public DbSet<ExposureDateEntity> ExposureDates { get; set; }
    public DbSet<UserSettingsEntity> UserSettings { get; set; }
    public DbSet<IdeaEntity> Ideas { get; set; }
    public DbSet<IdeaPhotoEntity> IdeaPhotos { get; set; }
    public DbSet<IdeaSessionEntity> IdeaSessions { get; set; }
    public DbSet<DevKitSessionEntity> DevKitSessions { get; set; }
    public DbSet<DevKitFilmEntity> DevKitFilms { get; set; }
    public DbSet<CollectionEntity> Collections { get; set; }
    public DbSet<CollectionPhotoEntity> CollectionPhotos { get; set; }
    public DbSet<CollectionPublicCommentEntity> CollectionPublicComments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure UserEntity
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PasswordHash).IsRequired();
        });

        // Configure NoteEntity
        modelBuilder.Entity<NoteEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SideNote).HasMaxLength(1000);
            entity.HasMany(e => e.Entries)
                .WithOne(e => e.Note)
                .HasForeignKey(e => e.NoteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure NoteEntryEntity
        modelBuilder.Entity<NoteEntryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.NoteId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Step).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Details).HasMaxLength(1000);
            entity.HasIndex(e => e.NoteId);
            
            // Relationships with rules and overrides
            entity.HasMany(e => e.Rules)
                .WithOne(r => r.NoteEntry)
                .HasForeignKey(r => r.NoteEntryId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasMany(e => e.Overrides)
                .WithOne(o => o.NoteEntry)
                .HasForeignKey(o => o.NoteEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure NoteEntryRuleEntity
        modelBuilder.Entity<NoteEntryRuleEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.NoteEntryId).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.NoteEntryId);
        });

        // Configure NoteEntryOverrideEntity
        modelBuilder.Entity<NoteEntryOverrideEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.NoteEntryId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Step).HasMaxLength(500);
            entity.Property(e => e.Details).HasMaxLength(1000);
            entity.HasIndex(e => e.NoteEntryId);
        });

        // Configure FilmEntity
        modelBuilder.Entity<FilmEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Brand).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Iso).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.CostCurrency)
                .HasConversion<string>()
                .HasMaxLength(10);
            entity.Property(e => e.DevelopedInSessionId).HasMaxLength(50);
            entity.Property(e => e.DevelopedWithDevKitId).HasMaxLength(50);

            entity.HasMany(e => e.Photos)
                .WithOne(e => e.Film)
                .HasForeignKey(e => e.FilmId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ExposureDates)
                .WithOne(e => e.Film)
                .HasForeignKey(e => e.FilmId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-many relationship: Film → Session (one film can be developed in one session)
            entity.HasOne(e => e.DevelopedInSession)
                .WithMany(s => s.DevelopedFilms)
                .HasForeignKey(e => e.DevelopedInSessionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Many-to-one relationship: Film → DevKit (one film can be developed with one devkit)
            entity.HasOne(e => e.DevelopedWithDevKit)
                .WithMany(d => d.DevelopedFilms)
                .HasForeignKey(e => e.DevelopedWithDevKitId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.Developed);
            entity.HasIndex(e => e.DevelopedInSessionId);
            entity.HasIndex(e => e.DevelopedWithDevKitId);
        });

        // Configure CollectionEntity (photo collections) — many-to-many via CollectionPhotos with Index + optional FilmId
        modelBuilder.Entity<CollectionEntity>(entity =>
        {
            entity.ToTable("Collections");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.ImageId).IsRequired();
            entity.Property(e => e.Owner)
                .HasConversion<string>()
                .HasMaxLength(50);
            entity.HasIndex(e => e.Owner);
            entity.HasIndex(e => e.IsOpen);

            entity.HasMany(e => e.Photos)
                .WithMany(p => p.Collections)
                .UsingEntity<CollectionPhotoEntity>(
                    r => r.HasOne(cp => cp.Photo)
                        .WithMany(p => p.CollectionPhotoLinks)
                        .HasForeignKey(cp => cp.PhotosId)
                        .HasPrincipalKey(p => p.Id),
                    l => l.HasOne(cp => cp.Collection)
                        .WithMany(c => c.CollectionPhotoLinks)
                        .HasForeignKey(cp => cp.CollectionsId)
                        .HasPrincipalKey(c => c.Id),
                    je =>
                    {
                        je.ToTable("CollectionPhotos");
                        je.HasKey(cp => new { cp.CollectionsId, cp.PhotosId });
                        je.Property(cp => cp.CollectionIndex).HasColumnName("Index").IsRequired();
                        je.Property(cp => cp.FilmId).HasMaxLength(50);
                        je.HasIndex(cp => cp.PhotosId);
                        je.HasIndex(cp => new { cp.CollectionsId, cp.CollectionIndex }).IsUnique();
                    });
        });

        modelBuilder.Entity<CollectionPublicCommentEntity>(entity =>
        {
            entity.ToTable("CollectionPublicComments");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.CollectionId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.AuthorName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Body).IsRequired().HasMaxLength(2000);
            entity.HasIndex(e => new { e.CollectionId, e.CreatedDate });
            entity.HasOne(e => e.Collection)
                .WithMany(c => c.PublicComments)
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure PhotoEntity
        modelBuilder.Entity<PhotoEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.FilmId).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.FilmId);
            entity.HasIndex(e => new { e.FilmId, e.Index }).IsUnique();
        });

        // Configure DevKitEntity
        modelBuilder.Entity<DevKitEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.Expired);
            entity.HasIndex(e => e.MixedOn);
        });

        // Configure SessionEntity
        modelBuilder.Entity<SessionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Location).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Participants).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Index)
                .ValueGeneratedOnAdd()
                .UseIdentityColumn(1, 1)
                .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
            entity.HasIndex(e => e.SessionDate);
            entity.HasIndex(e => e.Index).IsUnique();

            // Many-to-many relationship with DevKitEntity (multiple devkits can be used in multiple sessions)
            entity.HasMany(e => e.UsedDevKits)
                .WithMany(d => d.UsedInSessions)
                .UsingEntity<Dictionary<string, object>>(
                    "SessionDevKit",
                    j => j.HasOne<DevKitEntity>().WithMany().HasForeignKey("DevKitId"),
                    j => j.HasOne<SessionEntity>().WithMany().HasForeignKey("SessionId")
                );

            // One-to-many relationship with FilmEntity (one session can develop multiple films)
            // Films reference sessions via DevelopedInSessionId foreign key
            entity.HasMany(e => e.DevelopedFilms)
                .WithOne(f => f.DevelopedInSession)
                .HasForeignKey(f => f.DevelopedInSessionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure UsedFilmThumbnailEntity
        modelBuilder.Entity<UsedFilmThumbnailEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.FilmName).IsRequired().HasMaxLength(200);
        });

        // Configure UsedDevKitThumbnailEntity
        modelBuilder.Entity<UsedDevKitThumbnailEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.DevKitName).IsRequired().HasMaxLength(200);
        });

        // Configure ExposureDateEntity
        modelBuilder.Entity<ExposureDateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.FilmId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.HasIndex(e => e.FilmId);
        });

        // Configure UserSettingsEntity
        modelBuilder.Entity<UserSettingsEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CurrentFilmId).HasMaxLength(50);
            entity.Property(e => e.HomeSectionOrderJson).HasColumnType("nvarchar(max)");
            entity.HasIndex(e => e.UserId).IsUnique();

            // One-to-one relationship with UserEntity
            entity.HasOne(e => e.User)
                .WithOne(u => u.UserSettings)
                .HasForeignKey<UserSettingsEntity>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-one relationship with FilmEntity (nullable)
            entity.HasOne(e => e.CurrentFilm)
                .WithMany()
                .HasForeignKey(e => e.CurrentFilmId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configure IdeaEntity
        modelBuilder.Entity<IdeaEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Outcome).HasMaxLength(2000);
        });

        modelBuilder.Entity<IdeaPhotoEntity>(entity =>
        {
            entity.ToTable("IdeaPhotos");
            entity.HasKey(e => new { e.IdeaId, e.PhotoId });
            entity.HasOne(e => e.Idea)
                .WithMany()
                .HasForeignKey(e => e.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Photo)
                .WithMany()
                .HasForeignKey(e => e.PhotoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<IdeaSessionEntity>(entity =>
        {
            entity.ToTable("IdeaSessions");
            entity.HasKey(e => new { e.IdeaId, e.SessionId });
            entity.Property(e => e.IdeaId).HasMaxLength(50);
            entity.Property(e => e.SessionId).HasMaxLength(50);
            entity.HasIndex(e => e.SessionId);
            entity.HasOne(e => e.Idea)
                .WithMany(i => i.IdeaSessions)
                .HasForeignKey(e => e.IdeaId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Session)
                .WithMany(s => s.IdeaSessions)
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DevKitSessionEntity>(entity =>
        {
            entity.ToTable("DevKitSessions");
            entity.HasKey(e => new { e.DevKitId, e.SessionId });
            entity.HasOne(e => e.DevKit)
                .WithMany()
                .HasForeignKey(e => e.DevKitId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Session)
                .WithMany()
                .HasForeignKey(e => e.SessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DevKitFilmEntity>(entity =>
        {
            entity.ToTable("DevKitFilms");
            entity.HasKey(e => new { e.DevKitId, e.FilmId });
            entity.HasOne(e => e.DevKit)
                .WithMany()
                .HasForeignKey(e => e.DevKitId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Film)
                .WithMany()
                .HasForeignKey(e => e.FilmId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

