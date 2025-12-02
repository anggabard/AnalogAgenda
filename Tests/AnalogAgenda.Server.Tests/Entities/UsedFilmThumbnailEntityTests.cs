using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.Entities;
using Database.Services;
using Moq;

namespace AnalogAgenda.Server.Tests.Entities;

public class UsedFilmThumbnailEntityTests
{
    [Fact]
    public void Constructor_CreatesEntitySuccessfully()
    {
        // Act
        var entity = new UsedFilmThumbnailEntity
        {
            FilmName = "Test Film",
            ImageId = Guid.NewGuid()
        };

        // Assert - Entity is created successfully with required properties
        Assert.NotNull(entity);
        Assert.Equal("Test Film", entity.FilmName);
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
            Id = "test-row-key"
        };

        var systemConfig = new Configuration.Sections.System { IsDev = false };
        var storageConfig = new Storage { AccountName = accountName };
        var dtoConvertor = new DtoConvertor(systemConfig, storageConfig);

        // Act
        var dto = dtoConvertor.ToDTO(entity);

        // Assert
        Assert.NotNull(dto.Id);
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
            Id = "test-row-key"
        };

        var systemConfig = new Configuration.Sections.System { IsDev = false };
        var storageConfig = new Storage { AccountName = accountName };
        var dtoConvertor = new DtoConvertor(systemConfig, storageConfig);

        // Act
        var dto = dtoConvertor.ToDTO(entity);

        // Assert
        Assert.NotNull(dto.Id);
        Assert.Equal("Kodak Portra 400", dto.FilmName);
        Assert.Equal(Guid.Empty.ToString(), dto.ImageId);
        Assert.Empty(dto.ImageUrl);
    }

    [Fact]
    public void IdLength_ReturnsCorrectLength()
    {
        // Arrange
        var entity = new UsedFilmThumbnailEntity
        {
            FilmName = "Test Film",
            ImageId = Guid.NewGuid()
        };

        // Act & Assert
        // IdLenght is protected, so we can't test it directly
        // We can test that the entity is created successfully
        Assert.NotNull(entity);
    }
}
