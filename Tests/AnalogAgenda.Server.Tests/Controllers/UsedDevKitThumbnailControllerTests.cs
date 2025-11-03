using AnalogAgenda.Server.Controllers;
using AnalogAgenda.Server.Tests.Helpers;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.Data;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AnalogAgenda.Server.Tests.Controllers;

public class UsedDevKitThumbnailControllerTests : IDisposable
{
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly IDatabaseService _databaseService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<BlobContainerClient> _mockDevKitsContainerClient;
    private readonly Storage _storageConfig;
    private readonly UsedDevKitThumbnailController _controller;

    public UsedDevKitThumbnailControllerTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"UsedDevKitThumbnailTestDb_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_dbContext);
        _mockBlobService = new Mock<IBlobService>();
        _mockDevKitsContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.devkits))
                       .Returns(_mockDevKitsContainerClient.Object);

        _controller = new UsedDevKitThumbnailController(_storageConfig, _databaseService, _mockBlobService.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task SearchByDevKitName_WithEmptyQuery_ReturnsAllThumbnails()
    {
        // Arrange
        var thumbnail1 = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Bellini E6",
            ImageId = Guid.NewGuid(),
            Id = "thumb1"
        };
        var thumbnail2 = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Bellini C41",
            ImageId = Guid.NewGuid(),
            Id = "thumb2"
        };

        await _databaseService.AddAsync(thumbnail1);
        await _databaseService.AddAsync(thumbnail2);

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
        var thumbnail1 = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Bellini E6",
            ImageId = Guid.NewGuid(),
            Id = "thumb1"
        };
        var thumbnail2 = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Bellini C41",
            ImageId = Guid.NewGuid(),
            Id = "thumb2"
        };
        var thumbnail3 = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Kodak E6",
            ImageId = Guid.NewGuid(),
            Id = "thumb3"
        };

        await _databaseService.AddAsync(thumbnail1);
        await _databaseService.AddAsync(thumbnail2);
        await _databaseService.AddAsync(thumbnail3);

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
        var thumbnail1 = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Bellini E6",
            ImageId = Guid.NewGuid(),
            Id = "thumb1"
        };
        var thumbnail2 = new UsedDevKitThumbnailEntity
        {
            DevKitName = "Bellini C41",
            ImageId = Guid.NewGuid(),
            Id = "thumb2"
        };

        await _databaseService.AddAsync(thumbnail1);
        await _databaseService.AddAsync(thumbnail2);

        // Act
        var result = await _controller.SearchByDevKitName("bellini");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var thumbnailsList = Assert.IsAssignableFrom<List<UsedDevKitThumbnailDto>>(okResult.Value);
        Assert.Equal(2, thumbnailsList.Count);
        Assert.All(thumbnailsList, t => Assert.Contains("Bellini", t.DevKitName));
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
}

