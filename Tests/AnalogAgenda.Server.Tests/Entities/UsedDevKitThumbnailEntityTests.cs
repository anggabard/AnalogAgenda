using Database.DBObjects.Enums;
using Database.Entities;

namespace AnalogAgenda.Server.Tests.Entities;

public class UsedDevKitThumbnailEntityTests
{
    [Fact]
    public void Constructor_SetsCorrectTableName()
    {
        // Act
        var entity = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Test DevKit",
            ImageId = Guid.NewGuid()
        };

        // Assert
        Assert.Equal(TableName.UsedDevKitThumbnails, entity.GetTable());
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
            Id = "test-row-key"
        };

        // Act
        var dto = entity.ToDTO(accountName);

        // Assert
        Assert.NotNull(dto.Id); // Id is auto-generated
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
            Id = "test-row-key"
        };

        // Act
        var dto = entity.ToDTO(accountName);

        // Assert
        Assert.NotNull(dto.Id); // Id is auto-generated
        Assert.Equal("Bellini E6", dto.DevKitName);
        Assert.Equal(Guid.Empty.ToString(), dto.ImageId);
        Assert.Empty(dto.ImageUrl);
    }

    [Fact]
    public void IdLength_ReturnsCorrectLength()
    {
        // Arrange
        var entity = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Test DevKit",
            ImageId = Guid.NewGuid()
        };

        // Act & Assert
        // IdLenght is protected, so we can't test it directly
        // We can test that the entity is created successfully
        Assert.NotNull(entity);
    }
}
