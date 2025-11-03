using AnalogAgenda.Server.Controllers;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AnalogAgenda.Server.Tests.Controllers;

public class UsedFilmThumbnailControllerTests
{
    private readonly Mock<IDatabaseService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<TableClient> _mockThumbnailsTableClient;
    private readonly Mock<BlobContainerClient> _mockFilmsContainerClient;
    private readonly Storage _storageConfig;
    private readonly UsedFilmThumbnailController _controller;

    public UsedFilmThumbnailControllerTests()
    {
        _mockTableService = new Mock<IDatabaseService>();
        _mockBlobService = new Mock<IBlobService>();
        _mockThumbnailsTableClient = new Mock<TableClient>();
        _mockFilmsContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        _mockTableService.Setup(x => x.GetTable(TableName.UsedFilmThumbnails))
                        .Returns(_mockThumbnailsTableClient.Object);
        
        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.films))
                       .Returns(_mockFilmsContainerClient.Object);

        _controller = new UsedFilmThumbnailController(_storageConfig, _mockTableService.Object, _mockBlobService.Object);
    }

    [Fact]
    public async Task SearchByFilmName_WithEmptyQuery_ReturnsAllThumbnails()
    {
        // Arrange
        var thumbnails = new List<UsedFilmThumbnailEntity>
        {
            new UsedFilmThumbnailEntity
            {
                FilmName = "Kodak Portra 400",
                ImageId = Guid.NewGuid(),
                Id = "thumb1"
            },
            new UsedFilmThumbnailEntity
            {
                FilmName = "Fuji Superia 200",
                ImageId = Guid.NewGuid(),
                Id = "thumb2"
            }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<UsedFilmThumbnailEntity>())
                        .ReturnsAsync(thumbnails);

        // Act
        var result = await _controller.SearchByFilmName("");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var thumbnailsList = Assert.IsAssignableFrom<List<UsedFilmThumbnailDto>>(okResult.Value);
        Assert.Equal(2, thumbnailsList.Count);
        Assert.Equal("Fuji Superia 200", thumbnailsList.First().FilmName); // Should be ordered by FilmName
    }

    [Fact]
    public async Task SearchByFilmName_WithQuery_ReturnsFilteredThumbnails()
    {
        // Arrange
        var allThumbnails = new List<UsedFilmThumbnailEntity>
        {
            new UsedFilmThumbnailEntity
            {
                FilmName = "Kodak Portra 400",
                ImageId = Guid.NewGuid(),
                Id = "thumb1"
            },
            new UsedFilmThumbnailEntity
            {
                FilmName = "Fuji Superia 200",
                ImageId = Guid.NewGuid(),
                Id = "thumb2"
            },
            new UsedFilmThumbnailEntity
            {
                FilmName = "Kodak T-Max 100",
                ImageId = Guid.NewGuid(),
                Id = "thumb3"
            }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<UsedFilmThumbnailEntity>())
                        .ReturnsAsync(allThumbnails);

        // Act
        var result = await _controller.SearchByFilmName("Kodak");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var thumbnailsList = Assert.IsAssignableFrom<List<UsedFilmThumbnailDto>>(okResult.Value);
        Assert.Equal(2, thumbnailsList.Count);
        Assert.All(thumbnailsList, t => Assert.Contains("Kodak", t.FilmName));
    }

    [Fact]
    public async Task SearchByFilmName_WithCaseInsensitiveQuery_ReturnsFilteredThumbnails()
    {
        // Arrange
        var allThumbnails = new List<UsedFilmThumbnailEntity>
        {
            new UsedFilmThumbnailEntity
            {
                FilmName = "Kodak Portra 400",
                ImageId = Guid.NewGuid(),
                Id = "thumb1"
            },
            new UsedFilmThumbnailEntity
            {
                FilmName = "Fuji Superia 200",
                ImageId = Guid.NewGuid(),
                Id = "thumb2"
            }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<UsedFilmThumbnailEntity>())
                        .ReturnsAsync(allThumbnails);

        // Act
        var result = await _controller.SearchByFilmName("kodak");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var thumbnailsList = Assert.IsAssignableFrom<List<UsedFilmThumbnailDto>>(okResult.Value);
        Assert.Single(thumbnailsList);
        Assert.Equal("Kodak Portra 400", thumbnailsList.First().FilmName);
    }

    [Fact]
    public async Task CreateThumbnailEntry_WithValidData_ReturnsCreated()
    {
        // Arrange
        var dto = new UsedFilmThumbnailDto
        {
            FilmName = "Test Film 400",
            ImageBase64 = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/8A8A"
        };

        _mockThumbnailsTableClient.Setup(x => x.AddEntityAsync(It.IsAny<UsedFilmThumbnailEntity>(), default))
                                 .Returns(Task.FromResult(It.IsAny<Azure.Response>()));

        // Mock BlobClient for image upload
        var mockBlobClient = new Mock<BlobClient>();
        _mockFilmsContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                                 .Returns(mockBlobClient.Object);

        // Act
        var result = await _controller.CreateThumbnailEntry(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        var createdDto = Assert.IsType<UsedFilmThumbnailDto>(createdResult.Value);
        Assert.Equal("Test Film 400", createdDto.FilmName);
        Assert.NotNull(createdDto.ImageId);
        Assert.NotEmpty(createdDto.ImageUrl);
    }

    [Fact]
    public async Task CreateThumbnailEntry_WithEmptyFilmName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UsedFilmThumbnailDto
        {
            FilmName = "",
            ImageBase64 = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/8A8A"
        };

        // Act
        var result = await _controller.CreateThumbnailEntry(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("FilmName is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateThumbnailEntry_WithEmptyImageBase64_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UsedFilmThumbnailDto
        {
            FilmName = "Test Film 400",
            ImageBase64 = ""
        };

        // Act
        var result = await _controller.CreateThumbnailEntry(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("ImageBase64 is required for uploading a new thumbnail.", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateThumbnailEntry_WithException_ReturnsUnprocessableEntity()
    {
        // Arrange
        var dto = new UsedFilmThumbnailDto
        {
            FilmName = "Test Film 400",
            ImageBase64 = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/8A8A"
        };

        _mockThumbnailsTableClient.Setup(x => x.AddEntityAsync(It.IsAny<UsedFilmThumbnailEntity>(), default))
                                 .ThrowsAsync(new Exception("Database error"));

        // Mock BlobClient for image upload to throw exception
        var mockBlobClient = new Mock<BlobClient>();
        mockBlobClient.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Database error"));
        _mockFilmsContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                                 .Returns(mockBlobClient.Object);

        // Act
        var result = await _controller.CreateThumbnailEntry(dto);

        // Assert
        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.Equal("Database error", unprocessableResult.Value);
    }
}
