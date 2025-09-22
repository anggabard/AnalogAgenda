using Database.Helpers;
using Database.DBObjects.Enums;

namespace Database.Tests;

public class TableExtensionTests
{
    [Theory]
    [InlineData(TableName.Users, "USER")]
    [InlineData(TableName.Notes, "NOTE")]
    [InlineData(TableName.NotesEntries, "NOTEENTRY")]
    [InlineData(TableName.DevKits, "DEVKIT")]
    public void PartitionKey_WithValidTableName_ReturnsCorrectPartitionKey(TableName tableName, string expected)
    {
        // Act
        var result = tableName.PartitionKey();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void PartitionKey_WithInvalidTableName_ThrowsException()
    {
        // Arrange
        var invalidTableName = (TableName)999;

        // Act & Assert
        var exception = Assert.Throws<Exception>(() => invalidTableName.PartitionKey());
        Assert.Contains("Partition key for", exception.Message);
        Assert.Contains("does not exist", exception.Message);
    }

    [Theory]
    [InlineData("Users", true)]
    [InlineData("Notes", true)]
    [InlineData("NotesEntries", true)]
    [InlineData("DevKits", true)]
    public void IsTable_WithValidTableName_ReturnsTrue(string tableName, bool expected)
    {
        // Act
        var result = tableName.IsTable();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("InvalidTable", false)]
    [InlineData("", false)]
    [InlineData("123", false)]
    [InlineData("users", false)]  // case sensitive
    public void IsTable_WithInvalidTableName_ReturnsFalse(string tableName, bool expected)
    {
        // Act
        var result = tableName.IsTable();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsTable_WithNullString_ReturnsFalse()
    {
        // Arrange
        string? tableName = null;

        // Act
        var result = tableName!.IsTable();

        // Assert
        Assert.False(result);
    }
}
