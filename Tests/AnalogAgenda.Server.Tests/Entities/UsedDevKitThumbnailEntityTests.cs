using Database.DBObjects.Enums;
using Database.Entities;

namespace AnalogAgenda.Server.Tests.Entities;

public class UsedDevKitThumbnailEntityTests
{
    [Fact]
    public void Constructor_SetsCorrectTableName()
    {
        // Act
        var entity = new UsedDevKitThumbnailEntity();

        // Assert
        Assert.Equal(TableName.UsedDevKitThumbnails, entity.TableName);
    }

    [Fact]
    public void ToDTO_WithValidData_ReturnsCorrectDto()
    {
        // Arrange
        var accountName = "teststorage";
        var imageId = Guid.NewGuid();
        var entity = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Bellini E6",
            ImageId = imageId,
            RowKey = "test-row-key"
        };

        // Act
        var dto = entity.ToDTO(accountName);

        // Assert
        Assert.Equal("test-row-key", dto.RowKey);
        Assert.Equal("Bellini E6", dto.DevKitName);
        Assert.Equal(imageId.ToString(), dto.ImageId);
        Assert.Contains("teststorage", dto.ImageUrl);
        Assert.Contains("devkits", dto.ImageUrl);
        Assert.Contains(imageId.ToString(), dto.ImageUrl);
    }

    [Fact]
    public void ToDTO_WithEmptyImageId_ReturnsEmptyImageUrl()
    {
        // Arrange
        var accountName = "teststorage";
        var entity = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Bellini E6",
            ImageId = Guid.Empty,
            RowKey = "test-row-key"
        };

        // Act
        var dto = entity.ToDTO(accountName);

        // Assert
        Assert.Equal("test-row-key", dto.RowKey);
        Assert.Equal("Bellini E6", dto.DevKitName);
        Assert.Equal(Guid.Empty.ToString(), dto.ImageId);
        Assert.Empty(dto.ImageUrl);
    }

    [Fact]
    public void RowKeyLength_ReturnsCorrectLength()
    {
        // Arrange
        var entity = new UsedDevKitThumbnailEntity();

        // Act
        var length = entity.RowKeyLenght();

        // Assert
        Assert.Equal(6, length);
    }
}
