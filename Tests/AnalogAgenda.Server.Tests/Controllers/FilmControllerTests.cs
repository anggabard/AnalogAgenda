using AnalogAgenda.Server.Controllers;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading;

namespace AnalogAgenda.Server.Tests.Controllers;

public class FilmControllerTests
{
    private readonly Storage _storage;
    private readonly Mock<ITableService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<TableClient> _mockTableClient;
    private readonly Mock<BlobContainerClient> _mockBlobContainer;
    private readonly FilmController _controller;

    public FilmControllerTests()
    {
        _storage = new Storage { AccountName = "testaccount" };
        _mockTableService = new Mock<ITableService>();
        _mockBlobService = new Mock<IBlobService>();
        _mockTableClient = new Mock<TableClient>();
        _mockBlobContainer = new Mock<BlobContainerClient>();
        
        _mockTableService.Setup(x => x.GetTable(It.IsAny<TableName>()))
            .Returns(_mockTableClient.Object);
        _mockBlobService.Setup(x => x.GetBlobContainer(It.IsAny<ContainerName>()))
            .Returns(_mockBlobContainer.Object);
        
        _controller = new FilmController(_storage, _mockTableService.Object, _mockBlobService.Object);
    }

    [Fact]
    public void FilmController_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var controller = new FilmController(_storage, _mockTableService.Object, _mockBlobService.Object);

        // Assert
        Assert.NotNull(controller);
    }

    [Fact]
    public async Task CreateNewFilm_WithValidDto_ReturnsCreatedResult()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Test Film",
            Iso = "400",
            Type = "ColorNegative",
            NumberOfExposures = 36,
            Cost = 10.50,
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ImageUrl = "",
            Description = "Test Description",
            Developed = false
        };

        _mockTableClient.Setup(x => x.AddEntityAsync(It.IsAny<FilmEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _controller.CreateNewFilm(filmDto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(createdResult.Value);
    }

    [Fact]
    public async Task CreateNewFilm_WithBulkCount1_ReturnsCreatedResult()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Test Film",
            Iso = "400",
            Type = "ColorNegative",
            NumberOfExposures = 36,
            Cost = 10.50,
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ImageUrl = "",
            Description = "Test Description",
            Developed = false
        };

        _mockTableClient.Setup(x => x.AddEntityAsync(It.IsAny<FilmEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _controller.CreateNewFilm(filmDto, 1);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(createdResult.Value);
    }

    [Fact]
    public async Task CreateNewFilm_WithBulkCount5_Creates5Films()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Test Film",
            Iso = "400",
            Type = "ColorNegative",
            NumberOfExposures = 36,
            Cost = 10.50,
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ImageUrl = "",
            Description = "Test Description",
            Developed = false
        };

        var addEntityCallCount = 0;
        _mockTableClient.Setup(x => x.AddEntityAsync(It.IsAny<FilmEntity>(), It.IsAny<CancellationToken>()))
            .Callback(() => addEntityCallCount++)
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _controller.CreateNewFilm(filmDto, 5);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(createdResult.Value);
        Assert.Equal(5, addEntityCallCount);
    }

    [Fact]
    public async Task CreateNewFilm_WithBulkCount0_ReturnsBadRequest()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Test Film",
            Iso = "400",
            Type = "ColorNegative",
            NumberOfExposures = 36,
            Cost = 10.50,
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ImageUrl = "",
            Description = "Test Description",
            Developed = false
        };

        // Act
        var result = await _controller.CreateNewFilm(filmDto, 0);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("bulkCount must be between 1 and 10", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateNewFilm_WithBulkCount11_ReturnsBadRequest()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Test Film",
            Iso = "400",
            Type = "ColorNegative",
            NumberOfExposures = 36,
            Cost = 10.50,
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ImageUrl = "",
            Description = "Test Description",
            Developed = false
        };

        // Act
        var result = await _controller.CreateNewFilm(filmDto, 11);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("bulkCount must be between 1 and 10", badRequestResult.Value);
    }

    [Fact]
    public async Task CreateNewFilm_WithBulkCount10_Creates10Films()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Test Film",
            Iso = "400",
            Type = "ColorNegative",
            NumberOfExposures = 36,
            Cost = 10.50,
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ImageUrl = "",
            Description = "Test Description",
            Developed = false
        };

        var addEntityCallCount = 0;
        _mockTableClient.Setup(x => x.AddEntityAsync(It.IsAny<FilmEntity>(), It.IsAny<CancellationToken>()))
            .Callback(() => addEntityCallCount++)
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _controller.CreateNewFilm(filmDto, 10);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(createdResult.Value);
        Assert.Equal(10, addEntityCallCount);
    }

    [Fact]
    public async Task CreateNewFilm_WithException_ReturnsUnprocessableEntity()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Test Film",
            Iso = "400",
            Type = "ColorNegative",
            NumberOfExposures = 36,
            Cost = 10.50,
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ImageUrl = "",
            Description = "Test Description",
            Developed = false
        };

        _mockTableClient.Setup(x => x.AddEntityAsync(It.IsAny<FilmEntity>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _controller.CreateNewFilm(filmDto, 1);

        // Assert
        var unprocessableEntityResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.Equal("Database error", unprocessableEntityResult.Value);
    }

    [Fact]
    public async Task CreateNewFilm_WithBulkCount3_EachFilmHasUniqueDates()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Test Film",
            Iso = "400",
            Type = "ColorNegative",
            NumberOfExposures = 36,
            Cost = 10.50,
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ImageUrl = "",
            Description = "Test Description",
            Developed = false
        };

        var capturedEntities = new List<FilmEntity>();
        _mockTableClient.Setup(x => x.AddEntityAsync(It.IsAny<FilmEntity>(), It.IsAny<CancellationToken>()))
            .Callback<FilmEntity, CancellationToken>((entity, token) => capturedEntities.Add(entity))
            .ReturnsAsync(Mock.Of<Azure.Response>());

        // Act
        var result = await _controller.CreateNewFilm(filmDto, 3);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.NotNull(createdResult.Value);
        Assert.Equal(3, capturedEntities.Count);

        // Verify each film has unique CreatedDate and UpdatedDate
        for (int i = 0; i < capturedEntities.Count - 1; i++)
        {
            Assert.True(capturedEntities[i].CreatedDate < capturedEntities[i + 1].CreatedDate);
            Assert.True(capturedEntities[i].UpdatedDate < capturedEntities[i + 1].UpdatedDate);
        }
    }
}