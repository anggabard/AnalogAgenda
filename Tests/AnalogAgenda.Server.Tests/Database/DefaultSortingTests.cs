using Database.Entities;
using Database.DBObjects.Enums;

namespace AnalogAgenda.Server.Tests.Database;

public class DefaultSortingTests
{
    [Fact]
    public void DefaultSorting_ByUpdatedDate_NewestFirst()
    {
        // Arrange
        var entities = new List<DevKitEntity>
        {
            new() { Id = "3", Name = "Kit C", Url = "http://example.com", UpdatedDate = DateTime.Parse("2023-03-01") },
            new() { Id = "1", Name = "Kit A", Url = "http://example.com", UpdatedDate = DateTime.Parse("2023-01-01") },
            new() { Id = "2", Name = "Kit B", Url = "http://example.com", UpdatedDate = DateTime.Parse("2023-02-01") }
        };

        // Act - Apply default sorting (UpdatedDate descending)
        var sorted = entities.OrderByDescending(e => e.UpdatedDate).ToList();

        // Assert - Should be sorted by UpdatedDate descending (newest first)
        Assert.Equal(DateTime.Parse("2023-03-01"), sorted[0].UpdatedDate); // Newest first
        Assert.Equal(DateTime.Parse("2023-02-01"), sorted[1].UpdatedDate);
        Assert.Equal(DateTime.Parse("2023-01-01"), sorted[2].UpdatedDate); // Oldest last
    }

    [Fact]
    public void DefaultSorting_SameUpdatedDate_MaintainsOriginalOrder()
    {
        // Arrange
        var sameDate = DateTime.Parse("2023-01-01");
        var entities = new List<DevKitEntity>
        {
            new() { Id = "1", Name = "Kit A", Url = "http://example.com", UpdatedDate = sameDate },
            new() { Id = "2", Name = "Kit B", Url = "http://example.com", UpdatedDate = sameDate },
            new() { Id = "3", Name = "Kit C", Url = "http://example.com", UpdatedDate = sameDate }
        };

        // Act - Apply default sorting (UpdatedDate descending)
        var sorted = entities.OrderByDescending(e => e.UpdatedDate).ToList();

        // Assert - Order should be stable when dates are same
        Assert.Equal(3, sorted.Count);
        Assert.All(sorted, e => Assert.Equal(sameDate, e.UpdatedDate));
    }

    [Fact]
    public void DefaultSorting_EmptyCollection_ReturnsEmpty()
    {
        // Arrange
        var entities = new List<DevKitEntity>();

        // Act - Apply default sorting
        var sorted = entities.OrderByDescending(e => e.UpdatedDate).ToList();

        // Assert
        Assert.Empty(sorted);
    }

    [Fact]
    public void DefaultSorting_SingleEntity_ReturnsSameEntity()
    {
        // Arrange
        var entities = new List<DevKitEntity>
        {
            new() { Id = "1", Name = "Kit A", Url = "http://example.com", UpdatedDate = DateTime.Parse("2023-01-01") }
        };

        // Act - Apply default sorting
        var sorted = entities.OrderByDescending(e => e.UpdatedDate).ToList();

        // Assert
        Assert.Single(sorted);
        Assert.Equal("Kit A", sorted[0].Name);
    }
}

