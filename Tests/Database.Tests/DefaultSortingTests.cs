using Database.Entities;
using Xunit;

namespace Database.Tests;

public class DefaultSortingTests
{
    [Fact]
    public void DefaultSorting_ByUpdatedDate_NewestFirst()
    {
        // Arrange
        var entities = new List<DevKitEntity>
        {
            new() { RowKey = "3", Name = "Kit C", Url = "http://example.com", UpdatedDate = DateTime.Parse("2023-03-01") },
            new() { RowKey = "1", Name = "Kit A", Url = "http://example.com", UpdatedDate = DateTime.Parse("2023-01-01") },
            new() { RowKey = "2", Name = "Kit B", Url = "http://example.com", UpdatedDate = DateTime.Parse("2023-02-01") }
        };

        // Act - Apply default sorting (UpdatedDate descending)
        var sorted = entities.OrderByDescending(e => e.UpdatedDate).ToList();

        // Assert - Should be sorted by UpdatedDate descending (newest first)
        Assert.Equal(DateTime.Parse("2023-03-01"), sorted[0].UpdatedDate); // Newest first
        Assert.Equal(DateTime.Parse("2023-02-01"), sorted[1].UpdatedDate);
        Assert.Equal(DateTime.Parse("2023-01-01"), sorted[2].UpdatedDate); // Oldest last
    }

    [Fact]
    public void DefaultSorting_WithSameUpdatedDate_MaintainsOriginalOrder()
    {
        // Arrange
        var sameDate = DateTime.Parse("2023-01-01");
        var entities = new List<DevKitEntity>
        {
            new() { RowKey = "1", Name = "Kit A", Url = "http://example.com", UpdatedDate = sameDate },
            new() { RowKey = "2", Name = "Kit B", Url = "http://example.com", UpdatedDate = sameDate },
            new() { RowKey = "3", Name = "Kit C", Url = "http://example.com", UpdatedDate = sameDate }
        };

        // Act - Apply default sorting (UpdatedDate descending)
        var sorted = entities.OrderByDescending(e => e.UpdatedDate).ToList();

        // Assert - Should maintain original order when dates are the same
        Assert.Equal("Kit A", sorted[0].Name);
        Assert.Equal("Kit B", sorted[1].Name);
        Assert.Equal("Kit C", sorted[2].Name);
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
            new() { RowKey = "1", Name = "Kit A", Url = "http://example.com", UpdatedDate = DateTime.Parse("2023-01-01") }
        };

        // Act - Apply default sorting
        var sorted = entities.OrderByDescending(e => e.UpdatedDate).ToList();

        // Assert
        Assert.Single(sorted);
        Assert.Equal("Kit A", sorted[0].Name);
    }
}
