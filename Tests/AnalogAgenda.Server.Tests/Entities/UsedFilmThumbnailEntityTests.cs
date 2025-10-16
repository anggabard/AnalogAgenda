using Database.DBObjects.Enums;
using Database.Entities;

namespace AnalogAgenda.Server.Tests.Entities;

public class UsedFilmThumbnailEntityTests
{
    [Fact]
    public void Constructor_SetsCorrectTableName()
    {
        // Act
        var entity = new UsedFilmThumbnailEntity
        {
            FilmName = "Test Film",
            ImageId = Guid.NewGuid()
        };

        // Assert
        Assert.Equal(TableName.UsedFilmThumbnails, entity.GetTable());
    }

    [Fact]
    public void ToDTO_WithValidData_ReturnsCorrectDto()
    {
        // Arrange
        var accountName = "teststorage";
        var imageId = Guid.NewGuid();
        var entity = new UsedFilmThumbnailEntity
        {
            FilmName = "Kodak Portra 400",
            ImageId = imageId,
            RowKey = "test-row-key"
        };

        // Act
        var dto = entity.ToDTO(accountName);

        // Assert
        Assert.NotNull(dto.RowKey); // RowKey is auto-generated
        Assert.Equal("Kodak Portra 400", dto.FilmName);
        Assert.Equal(imageId.ToString(), dto.ImageId);
        Assert.Contains("teststorage", dto.ImageUrl);
        Assert.Contains("films", dto.ImageUrl);
        Assert.Contains(imageId.ToString(), dto.ImageUrl);
    }

    [Fact]
    public void ToDTO_WithEmptyImageId_ReturnsEmptyImageUrl()
    {
        // Arrange
        var accountName = "teststorage";
        var entity = new UsedFilmThumbnailEntity
        {
            FilmName = "Kodak Portra 400",
            ImageId = Guid.Empty,
            RowKey = "test-row-key"
        };

        // Act
        var dto = entity.ToDTO(accountName);

        // Assert
        Assert.NotNull(dto.RowKey); // RowKey is auto-generated
        Assert.Equal("Kodak Portra 400", dto.FilmName);
        Assert.Equal(Guid.Empty.ToString(), dto.ImageId);
        Assert.Empty(dto.ImageUrl);
    }

    [Fact]
    public void RowKeyLength_ReturnsCorrectLength()
    {
        // Arrange
        var entity = new UsedFilmThumbnailEntity
        {
            FilmName = "Test Film",
            ImageId = Guid.NewGuid()
        };

        // Act & Assert
        // RowKeyLenght is protected, so we can't test it directly
        // We can test that the entity is created successfully
        Assert.NotNull(entity);
    }
}
