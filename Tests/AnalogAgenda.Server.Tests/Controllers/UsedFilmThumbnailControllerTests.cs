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

public class UsedFilmThumbnailControllerTests : IDisposable
{
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly IDatabaseService _databaseService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<BlobContainerClient> _mockFilmsContainerClient;
    private readonly Storage _storageConfig;
    private readonly UsedFilmThumbnailController _controller;

    public UsedFilmThumbnailControllerTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"UsedFilmThumbnailTestDb_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_dbContext);
        _mockBlobService = new Mock<IBlobService>();
        _mockFilmsContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.films))
                       .Returns(_mockFilmsContainerClient.Object);

        _controller = new UsedFilmThumbnailController(_storageConfig, _databaseService, _mockBlobService.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task SearchByFilmName_WithEmptyQuery_ReturnsAllThumbnails()
    {
        // Arrange
        var thumbnail1 = new UsedFilmThumbnailEntity
        {
            FilmName = "Kodak Portra 400",
            ImageId = Guid.NewGuid(),
            Id = "thumb1"
        };
        var thumbnail2 = new UsedFilmThumbnailEntity
        {
            FilmName = "Fuji Superia 200",
            ImageId = Guid.NewGuid(),
            Id = "thumb2"
        };

        await _databaseService.AddAsync(thumbnail1);
        await _databaseService.AddAsync(thumbnail2);

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
        var thumbnail1 = new UsedFilmThumbnailEntity
        {
            FilmName = "Kodak Portra 400",
            ImageId = Guid.NewGuid(),
            Id = "thumb1"
        };
        var thumbnail2 = new UsedFilmThumbnailEntity
        {
            FilmName = "Fuji Superia 200",
            ImageId = Guid.NewGuid(),
            Id = "thumb2"
        };
        var thumbnail3 = new UsedFilmThumbnailEntity
        {
            FilmName = "Kodak T-Max 100",
            ImageId = Guid.NewGuid(),
            Id = "thumb3"
        };

        await _databaseService.AddAsync(thumbnail1);
        await _databaseService.AddAsync(thumbnail2);
        await _databaseService.AddAsync(thumbnail3);

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
        var thumbnail1 = new UsedFilmThumbnailEntity
        {
            FilmName = "Kodak Portra 400",
            ImageId = Guid.NewGuid(),
            Id = "thumb1"
        };
        var thumbnail2 = new UsedFilmThumbnailEntity
        {
            FilmName = "Fuji Superia 200",
            ImageId = Guid.NewGuid(),
            Id = "thumb2"
        };

        await _databaseService.AddAsync(thumbnail1);
        await _databaseService.AddAsync(thumbnail2);

        // Act
        var result = await _controller.SearchByFilmName("kodak");

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var thumbnailsList = Assert.IsAssignableFrom<List<UsedFilmThumbnailDto>>(okResult.Value);
        Assert.Single(thumbnailsList);
        Assert.Equal("Kodak Portra 400", thumbnailsList.First().FilmName);
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
}

