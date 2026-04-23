using AnalogAgenda.Server.Tests.Helpers;
using Database.Data;
using Database.DBObjects.Enums;
using Database.Entities;
using Database.Services;

namespace AnalogAgenda.Server.Tests.Services;

public class PhotoOfTheDayServiceTests : IDisposable
{
    private const string FilmId = "film00000001";
    private readonly AnalogAgendaDbContext _dbContext;

    public PhotoOfTheDayServiceTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"PhotoOfTheDay_{Guid.NewGuid()}");
        Seed();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    private void Seed()
    {
        var film = new FilmEntity
        {
            Id = FilmId,
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Developed = false,
            Description = "",
            Name = "",
        };

        _dbContext.Films.Add(film);

        // Ids ordered: aa < bb < cc — only unrestricted count for pick.
        _dbContext.Photos.AddRange(
            new PhotoEntity
            {
                Id = "aa_photo00000001",
                FilmId = FilmId,
                Index = 1,
                ImageId = Guid.NewGuid(),
                Restricted = false,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
            },
            new PhotoEntity
            {
                Id = "bb_photo00000002",
                FilmId = FilmId,
                Index = 2,
                ImageId = Guid.NewGuid(),
                Restricted = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
            },
            new PhotoEntity
            {
                Id = "cc_photo00000003",
                FilmId = FilmId,
                Index = 3,
                ImageId = Guid.NewGuid(),
                Restricted = false,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
            });

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetCurrentOrRefreshAsync_CalledTwice_ReturnsSamePhotoId()
    {
        var sut = new PhotoOfTheDayService(_dbContext);

        var first = await sut.GetCurrentOrRefreshAsync(CancellationToken.None);
        var second = await sut.GetCurrentOrRefreshAsync(CancellationToken.None);

        Assert.NotNull(first);
        Assert.NotNull(second);
        Assert.Equal(first!.Id, second!.Id);
        Assert.NotEqual("bb_photo00000002", first.Id);
    }
}
