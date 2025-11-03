using Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Database.Data;

public class AnalogAgendaDbContext : DbContext
{
    public AnalogAgendaDbContext(DbContextOptions<AnalogAgendaDbContext> options) : base(options)
    {
    }

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
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Iso).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ExposureDates).HasMaxLength(4000);
            entity.Property(e => e.DevelopedInSessionId).HasMaxLength(50);
            entity.Property(e => e.DevelopedWithDevKitId).HasMaxLength(50);

            entity.HasMany(e => e.Photos)
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
            entity.HasIndex(e => e.SessionDate);

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
    }
}

