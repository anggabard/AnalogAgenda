using Database.Entities;
using Database.Helpers;
using Database.DBObjects.Enums;

namespace AnalogAgenda.Server.Tests.Database;

public class EntitySortingExtensionsTests
{
    [Fact]
    public void DevKitStandardSorting_OrdersByPurchasedOnOldestFirst()
    {
        // Arrange
        var entities = new List<DevKitEntity>
        {
            new() { Id = "3", PurchasedOn = DateTime.Parse("2023-03-01"), Name = "Kit C", Url = "http://example.com" },
            new() { Id = "1", PurchasedOn = DateTime.Parse("2023-01-01"), Name = "Kit A", Url = "http://example.com" },
            new() { Id = "2", PurchasedOn = DateTime.Parse("2023-02-01"), Name = "Kit B", Url = "http://example.com" }
        };

        // Act
        var sorted = entities.ApplyStandardSorting().ToList();

        // Assert
        Assert.Equal(DateTime.Parse("2023-01-01"), sorted[0].PurchasedOn); // Oldest first
        Assert.Equal(DateTime.Parse("2023-02-01"), sorted[1].PurchasedOn);
        Assert.Equal(DateTime.Parse("2023-03-01"), sorted[2].PurchasedOn); // Newest last
    }

    [Fact]
    public void FilmStandardSorting_OrdersByOwnerThenDate()
    {
        // Arrange
        var entities = new List<FilmEntity>
        {
            new() { Id = "1", Name = "Film 1", Iso = "400", PurchasedBy = EUsernameType.Tudor, PurchasedOn = DateTime.Parse("2023-01-15") },
            new() { Id = "2", Name = "Film 2", Iso = "400", PurchasedBy = EUsernameType.Angel, PurchasedOn = DateTime.Parse("2023-01-20") },
            new() { Id = "3", Name = "Film 3", Iso = "400", PurchasedBy = EUsernameType.Angel, PurchasedOn = DateTime.Parse("2023-01-10") },
        };

        // Act
        var sorted = entities.ApplyStandardSorting().ToList();

        // Assert - Angel comes before Tudor alphabetically, then by date (newest first within same owner)
        Assert.Equal(EUsernameType.Angel, sorted[0].PurchasedBy);
        Assert.Equal(DateTime.Parse("2023-01-20"), sorted[0].PurchasedOn); // Angel, newest first
        Assert.Equal(EUsernameType.Angel, sorted[1].PurchasedBy);
        Assert.Equal(DateTime.Parse("2023-01-10"), sorted[1].PurchasedOn); // Angel, older
        Assert.Equal(EUsernameType.Tudor, sorted[2].PurchasedBy);
        Assert.Equal(DateTime.Parse("2023-01-15"), sorted[2].PurchasedOn); // Tudor
    }

    [Fact]
    public void FilmUserFilteredSorting_OrdersByDateNewestFirst()
    {
        // Arrange
        var entities = new List<FilmEntity>
        {
            new() { Id = "2", Name = "Film A", Iso = "400", PurchasedOn = DateTime.Parse("2023-01-10") },
            new() { Id = "1", Name = "Film B", Iso = "400", PurchasedOn = DateTime.Parse("2023-01-20") },
            new() { Id = "3", Name = "Film C", Iso = "400", PurchasedOn = DateTime.Parse("2023-01-15") }
        };

        // Act
        var sorted = entities.ApplyUserFilteredSorting().ToList();

        // Assert - Newest first
        Assert.Equal(DateTime.Parse("2023-01-20"), sorted[0].PurchasedOn); // Newest
        Assert.Equal(DateTime.Parse("2023-01-15"), sorted[1].PurchasedOn);
        Assert.Equal(DateTime.Parse("2023-01-10"), sorted[2].PurchasedOn); // Oldest
    }

    [Fact]
    public void PhotoStandardSorting_OrdersByIndex()
    {
        // Arrange
        var entities = new List<PhotoEntity>
        {
            new() { Id = "photo3", FilmId = "film1", Index = 3 },
            new() { Id = "photo1", FilmId = "film1", Index = 1 },
            new() { Id = "photo2", FilmId = "film1", Index = 2 }
        };

        // Act
        var sorted = entities.ApplyStandardSorting().ToList();

        // Assert
        Assert.Equal(1, sorted[0].Index); // Lowest index first
        Assert.Equal(2, sorted[1].Index);
        Assert.Equal(3, sorted[2].Index); // Highest index last
    }

    [Fact]
    public void SortingWithEmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var entities = new List<DevKitEntity>();

        // Act
        var sorted = entities.ApplyStandardSorting().ToList();

        // Assert
        Assert.Empty(sorted);
    }

    [Fact]
    public void SortingWithSingleEntity_ReturnsSameEntity()
    {
        // Arrange
        var entities = new List<FilmEntity>
        {
            new() { Id = "single", Name = "Single Film", Iso = "400", PurchasedBy = EUsernameType.Angel, PurchasedOn = DateTime.Parse("2023-01-01") }
        };

        // Act
        var sorted = entities.ApplyStandardSorting().ToList();

        // Assert
        Assert.Single(sorted);
        Assert.Equal("Single Film", sorted[0].Name);
    }
}

