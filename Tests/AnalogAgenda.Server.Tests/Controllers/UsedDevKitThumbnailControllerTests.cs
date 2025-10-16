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

public class UsedDevKitThumbnailControllerTests
{
    private readonly Mock<ITableService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<TableClient> _mockThumbnailsTableClient;
    private readonly Mock<BlobContainerClient> _mockDevKitsContainerClient;
    private readonly Storage _storageConfig;
    private readonly UsedDevKitThumbnailController _controller;

    public UsedDevKitThumbnailControllerTests()
    {
        _mockTableService = new Mock<ITableService>();
        _mockBlobService = new Mock<IBlobService>();
        _mockThumbnailsTableClient = new Mock<TableClient>();
        _mockDevKitsContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        _mockTableService.Setup(x => x.GetTable(TableName.UsedDevKitThumbnails))
                        .Returns(_mockThumbnailsTableClient.Object);
        
        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.devkits))
                       .Returns(_mockDevKitsContainerClient.Object);

        _controller = new UsedDevKitThumbnailController(_storageConfig, _mockTableService.Object, _mockBlobService.Object);
    }

    [Fact]
    public async Task SearchByDevKitName_WithEmptyQuery_ReturnsAllThumbnails()
    {
        // Arrange
        var thumbnails = new List<UsedDevKitThumbnailEntity>
        {
            new UsedDevKitThumbnailEntity
            {
                DevKitName = "Bellini E6",
                ImageId = Guid.NewGuid(),
                RowKey = "thumb1"
            },
            new UsedDevKitThumbnailEntity
            {
                DevKitName = "Bellini C41",
                ImageId = Guid.NewGuid(),
                RowKey = "thumb2"
            }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<UsedDevKitThumbnailEntity>())
                        .ReturnsAsync(thumbnails);

        // Act
        var result = await _controller.SearchByDevKitName("");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var thumbnailsList = Assert.IsAssignableFrom<List<UsedDevKitThumbnailDto>>(okResult.Value);
        Assert.Equal(2, thumbnailsList.Count);
        Assert.Equal("Bellini C41", thumbnailsList.First().DevKitName); // Should be ordered by DevKitName
    }

    [Fact]
    public async Task SearchByDevKitName_WithQuery_ReturnsFilteredThumbnails()
    {
        // Arrange
        var allThumbnails = new List<UsedDevKitThumbnailEntity>
        {
            new UsedDevKitThumbnailEntity
            {
                DevKitName = "Bellini E6",
                ImageId = Guid.NewGuid(),
                RowKey = "thumb1"
            },
            new UsedDevKitThumbnailEntity
            {
                DevKitName = "Bellini C41",
                ImageId = Guid.NewGuid(),
                RowKey = "thumb2"
            },
            new UsedDevKitThumbnailEntity
            {
                DevKitName = "Kodak E6",
                ImageId = Guid.NewGuid(),
                RowKey = "thumb3"
            }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<UsedDevKitThumbnailEntity>())
                        .ReturnsAsync(allThumbnails);

        // Act
        var result = await _controller.SearchByDevKitName("Bellini");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var thumbnailsList = Assert.IsAssignableFrom<List<UsedDevKitThumbnailDto>>(okResult.Value);
        Assert.Equal(2, thumbnailsList.Count);
        Assert.All(thumbnailsList, t => Assert.Contains("Bellini", t.DevKitName));
    }

    [Fact]
    public async Task SearchByDevKitName_WithCaseInsensitiveQuery_ReturnsFilteredThumbnails()
    {
        // Arrange
        var allThumbnails = new List<UsedDevKitThumbnailEntity>
        {
            new UsedDevKitThumbnailEntity
            {
                DevKitName = "Bellini E6",
                ImageId = Guid.NewGuid(),
                RowKey = "thumb1"
            },
            new UsedDevKitThumbnailEntity
            {
                DevKitName = "Bellini C41",
                ImageId = Guid.NewGuid(),
                RowKey = "thumb2"
            }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<UsedDevKitThumbnailEntity>())
                        .ReturnsAsync(allThumbnails);

        // Act
        var result = await _controller.SearchByDevKitName("bellini");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var thumbnailsList = Assert.IsAssignableFrom<List<UsedDevKitThumbnailDto>>(okResult.Value);
        Assert.Equal(2, thumbnailsList.Count);
        Assert.All(thumbnailsList, t => Assert.Contains("Bellini", t.DevKitName));
    }

    [Fact]
    public async Task CreateThumbnailEntry_WithValidData_ReturnsCreated()
    {
        // Arrange
        var dto = new UsedDevKitThumbnailDto
        {
            DevKitName = "Test DevKit E6",
            ImageBase64 = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/8A8A"
        };

        _mockThumbnailsTableClient.Setup(x => x.AddEntityAsync(It.IsAny<UsedDevKitThumbnailEntity>(), default))
                                 .Returns(Task.FromResult(It.IsAny<Azure.Response>()));

        // Mock BlobClient for image upload
        var mockBlobClient = new Mock<BlobClient>();
        _mockDevKitsContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                                 .Returns(mockBlobClient.Object);

        // Act
        var result = await _controller.CreateThumbnailEntry(dto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        var createdDto = Assert.IsType<UsedDevKitThumbnailDto>(createdResult.Value);
        Assert.Equal("Test DevKit E6", createdDto.DevKitName);
        Assert.NotNull(createdDto.ImageId);
        Assert.NotEmpty(createdDto.ImageUrl);
    }

    [Fact]
    public async Task CreateThumbnailEntry_WithEmptyDevKitName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UsedDevKitThumbnailDto
        {
            DevKitName = "",
            ImageBase64 = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/8A8A"
        };

        // Act
        var result = await _controller.CreateThumbnailEntry(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("DevKitName is required.", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateThumbnailEntry_WithEmptyImageBase64_ReturnsBadRequest()
    {
        // Arrange
        var dto = new UsedDevKitThumbnailDto
        {
            DevKitName = "Test DevKit E6",
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
        var dto = new UsedDevKitThumbnailDto
        {
            DevKitName = "Test DevKit E6",
            ImageBase64 = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/8A8A"
        };

        _mockThumbnailsTableClient.Setup(x => x.AddEntityAsync(It.IsAny<UsedDevKitThumbnailEntity>(), default))
                                 .ThrowsAsync(new Exception("Database error"));

        // Mock BlobClient for image upload to throw exception
        var mockBlobClient = new Mock<BlobClient>();
        mockBlobClient.Setup(x => x.UploadAsync(It.IsAny<Stream>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                     .ThrowsAsync(new Exception("Database error"));
        _mockDevKitsContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                                 .Returns(mockBlobClient.Object);

        // Act
        var result = await _controller.CreateThumbnailEntry(dto);

        // Assert
        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.Equal("Database error", unprocessableResult.Value);
    }
}
