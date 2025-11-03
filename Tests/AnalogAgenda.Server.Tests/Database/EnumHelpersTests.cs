using Database.Helpers;
using Database.DBObjects.Enums;

namespace AnalogAgenda.Server.Tests.Database;

public class EnumHelpersTests
{
    [Theory]
    [InlineData("Users", TableName.Users)]
    [InlineData("Notes", TableName.Notes)]
    [InlineData("NotesEntries", TableName.NotesEntries)]
    [InlineData("DevKits", TableName.DevKits)]
    public void ToEnum_WithValidStringValue_ReturnsCorrectEnum(string value, TableName expected)
    {
        // Act
        var result = value.ToEnum<TableName>();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("users")]  // lowercase
    [InlineData("USERS")]  // uppercase
    [InlineData("UsErS")]  // mixed case
    public void ToEnum_WithDifferentCasing_ReturnsCorrectEnum(string value)
    {
        // Act
        var result = value.ToEnum<TableName>();

        // Assert
        Assert.Equal(TableName.Users, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ToEnum_WithNullOrEmptyString_ThrowsInvalidCastException(string? value)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidCastException>(() => value!.ToEnum<TableName>());
        Assert.Contains("Value cannot be null", exception.Message);
    }

    [Fact]
    public void ToEnum_WithInvalidEnumValue_ThrowsInvalidCastException()
    {
        // Arrange
        var invalidValue = "InvalidEnumValue";

        // Act & Assert
        var exception = Assert.Throws<InvalidCastException>(() => invalidValue.ToEnum<TableName>());
        Assert.Contains("could not be casted", exception.Message);
        Assert.Contains(invalidValue, exception.Message);
    }

    [Theory]
    [InlineData("C41", EDevKitType.C41)]
    [InlineData("BW", EDevKitType.BW)]
    [InlineData("E6", EDevKitType.E6)]
    [InlineData("Other", EDevKitType.Other)]
    public void ToEnum_WithDevKitType_ReturnsCorrectEnum(string value, EDevKitType expected)
    {
        // Act
        var result = value.ToEnum<EDevKitType>();

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Angel", EUsernameType.Angel)]
    [InlineData("Tudor", EUsernameType.Tudor)]
    [InlineData("Cristiana", EUsernameType.Cristiana)]
    public void ToEnum_WithUsernameType_ReturnsCorrectEnum(string value, EUsernameType expected)
    {
        // Act
        var result = value.ToEnum<EUsernameType>();

        // Assert
        Assert.Equal(expected, result);
    }
}

