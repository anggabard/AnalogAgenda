using Database.Entities;
using Database.Helpers;
using Database.DBObjects.Enums;
using Xunit;

namespace Database.Tests;

public class EntitySortingExtensionsTests
{
    [Fact]
    public void ApplyStandardSorting_DevKitEntities_SortsByPurchasedOn()
    {
        // Arrange
        var entities = new List<DevKitEntity>
        {
            new() { RowKey = "3", PurchasedOn = DateTime.Parse("2023-03-01"), Name = "Kit C", Url = "http://example.com" },
            new() { RowKey = "1", PurchasedOn = DateTime.Parse("2023-01-01"), Name = "Kit A", Url = "http://example.com" },
            new() { RowKey = "2", PurchasedOn = DateTime.Parse("2023-02-01"), Name = "Kit B", Url = "http://example.com" }
        };

        // Act
        var sorted = entities.ApplyStandardSorting().ToList();

        // Assert - Check sorting by PurchasedOn date (oldest first)
        Assert.Equal(DateTime.Parse("2023-01-01"), sorted[0].PurchasedOn); // Oldest first
        Assert.Equal(DateTime.Parse("2023-02-01"), sorted[1].PurchasedOn);
        Assert.Equal(DateTime.Parse("2023-03-01"), sorted[2].PurchasedOn);
    }

    [Fact]
    public void ApplyStandardSorting_FilmEntities_SortsByPurchasedByThenDate()
    {
        // Arrange
        var entities = new List<FilmEntity>
        {
            new() { RowKey = "1", Name = "Film 1", PurchasedBy = EUsernameType.Tudor, PurchasedOn = DateTime.Parse("2023-01-15") },
            new() { RowKey = "2", Name = "Film 2", PurchasedBy = EUsernameType.Angel, PurchasedOn = DateTime.Parse("2023-01-20") },
            new() { RowKey = "3", Name = "Film 3", PurchasedBy = EUsernameType.Angel, PurchasedOn = DateTime.Parse("2023-01-10") },
            new() { RowKey = "4", Name = "Film 4", PurchasedBy = EUsernameType.Tudor, PurchasedOn = DateTime.Parse("2023-01-25") }
        };

        // Act
        var sorted = entities.ApplyStandardSorting().ToList();

        // Assert - First by owner (Angel comes before Tudor alphabetically), then by date (newest first within same owner)
        Assert.Equal(EUsernameType.Angel, sorted[0].PurchasedBy); // Angel first
        Assert.Equal(DateTime.Parse("2023-01-20"), sorted[0].PurchasedOn); // Angel, newest first
        Assert.Equal(EUsernameType.Angel, sorted[1].PurchasedBy); // Angel second
        Assert.Equal(DateTime.Parse("2023-01-10"), sorted[1].PurchasedOn); // Angel, older
        Assert.Equal(EUsernameType.Tudor, sorted[2].PurchasedBy); // Tudor first
        Assert.Equal(DateTime.Parse("2023-01-25"), sorted[2].PurchasedOn); // Tudor, newest first
        Assert.Equal(EUsernameType.Tudor, sorted[3].PurchasedBy); // Tudor second
        Assert.Equal(DateTime.Parse("2023-01-15"), sorted[3].PurchasedOn); // Tudor, older
    }

    [Fact]
    public void ApplyUserFilteredSorting_FilmEntities_SortsByDateNewestFirst()
    {
        // Arrange
        var entities = new List<FilmEntity>
        {
            new() { RowKey = "2", Name = "Film A", PurchasedOn = DateTime.Parse("2023-01-10") },
            new() { RowKey = "1", Name = "Film B", PurchasedOn = DateTime.Parse("2023-01-20") },
            new() { RowKey = "3", Name = "Film C", PurchasedOn = DateTime.Parse("2023-01-15") }
        };

        // Act
        var sorted = entities.ApplyUserFilteredSorting().ToList();

        // Assert - Newest first
        Assert.Equal(DateTime.Parse("2023-01-20"), sorted[0].PurchasedOn); // 2023-01-20
        Assert.Equal(DateTime.Parse("2023-01-15"), sorted[1].PurchasedOn); // 2023-01-15
        Assert.Equal(DateTime.Parse("2023-01-10"), sorted[2].PurchasedOn); // 2023-01-10
    }

    [Fact]
    public void ApplyStandardSorting_PhotoEntities_SortsByIndex()
    {
        // Arrange
        var entities = new List<PhotoEntity>
        {
            new() { RowKey = "photo3", FilmRowId = "film1", Index = 3 },
            new() { RowKey = "photo1", FilmRowId = "film1", Index = 1 },
            new() { RowKey = "photo2", FilmRowId = "film1", Index = 2 }
        };

        // Act
        var sorted = entities.ApplyStandardSorting().ToList();

        // Assert
        Assert.Equal(1, sorted[0].Index); // Index 1
        Assert.Equal(2, sorted[1].Index); // Index 2
        Assert.Equal(3, sorted[2].Index); // Index 3
    }

    [Fact]
    public void ApplyStandardSorting_EmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var entities = new List<DevKitEntity>();

        // Act
        var sorted = entities.ApplyStandardSorting().ToList();

        // Assert
        Assert.Empty(sorted);
    }

    [Fact]
    public void ApplyStandardSorting_SingleEntity_ReturnsSameEntity()
    {
        // Arrange
        var entities = new List<FilmEntity>
        {
            new() { RowKey = "single", Name = "Single Film", PurchasedBy = EUsernameType.Angel, PurchasedOn = DateTime.Parse("2023-01-01") }
        };

        // Act
        var sorted = entities.ApplyStandardSorting().ToList();

        // Assert
        Assert.Single(sorted);
        Assert.Equal("Single Film", sorted[0].Name);
    }
}
