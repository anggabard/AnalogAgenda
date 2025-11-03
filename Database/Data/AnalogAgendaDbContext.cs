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
            entity.Property(e => e.Process).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Film).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Details).HasMaxLength(1000);
            entity.HasIndex(e => e.NoteId);
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

            entity.HasOne(e => e.DevelopedInSession)
                .WithMany(s => s.DevelopedFilms)
                .HasForeignKey(e => e.DevelopedInSessionId)
                .OnDelete(DeleteBehavior.SetNull);

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

            // Many-to-many relationship with DevKitEntity
            entity.HasMany(e => e.UsedDevKits)
                .WithMany(d => d.UsedInSessions)
                .UsingEntity<Dictionary<string, object>>(
                    "SessionDevKit",
                    j => j.HasOne<DevKitEntity>().WithMany().HasForeignKey("DevKitId"),
                    j => j.HasOne<SessionEntity>().WithMany().HasForeignKey("SessionId")
                );
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

