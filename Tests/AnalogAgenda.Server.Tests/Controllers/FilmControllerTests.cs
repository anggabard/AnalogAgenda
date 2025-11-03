using AnalogAgenda.Server.Controllers;
using Azure;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.DTOs.Subclasses;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading;

namespace AnalogAgenda.Server.Tests.Controllers;

public class FilmControllerTests
{
    private readonly Storage _storage;
    private readonly Mock<IDatabaseService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<TableClient> _mockTableClient;
    private readonly Mock<BlobContainerClient> _mockBlobContainer;
    private readonly FilmController _controller;

    public FilmControllerTests()
    {
        _storage = new Storage { AccountName = "testaccount" };
        _mockTableService = new Mock<IDatabaseService>();
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
            Developed = false,
            ExposureDates = string.Empty
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
            Developed = false,
            ExposureDates = string.Empty
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
            Developed = false,
            ExposureDates = string.Empty
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
            Developed = false,
            ExposureDates = string.Empty
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
            Developed = false,
            ExposureDates = string.Empty
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
            Developed = false,
            ExposureDates = string.Empty
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
            Developed = false,
            ExposureDates = string.Empty
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
            Developed = false,
            ExposureDates = string.Empty
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

    [Fact]
    public async Task CreateNewFilm_WithExposureDates_ShouldSaveCorrectly()
    {
        // Arrange
        var exposureDatesJson = "[{\"date\":\"2024-01-15\",\"description\":\"Beach shoot\"},{\"date\":\"2024-01-20\",\"description\":\"Portrait session\"}]";
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
            Developed = false,
            ExposureDates = exposureDatesJson
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
    public async Task CreateNewFilm_WithEmptyExposureDates_ShouldSaveCorrectly()
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
            Developed = false,
            ExposureDates = string.Empty
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
    public void FilmDto_ExposureDatesList_ShouldSerializeAndDeserializeCorrectly()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Test Film",
            Iso = "400",
            Type = "ColorNegative",
            PurchasedBy = "Angel"
        };
        var exposureDates = new List<ExposureDateEntry>
        {
            new ExposureDateEntry { Date = DateOnly.FromDateTime(DateTime.UtcNow), Description = "Test exposure 1" },
            new ExposureDateEntry { Date = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)), Description = "Test exposure 2" }
        };

        // Act
        filmDto.ExposureDatesList = exposureDates;

        // Assert
        Assert.NotEmpty(filmDto.ExposureDates);
        var deserializedDates = filmDto.ExposureDatesList;
        Assert.Equal(2, deserializedDates.Count);
        Assert.Equal("Test exposure 1", deserializedDates[0].Description);
        Assert.Equal("Test exposure 2", deserializedDates[1].Description);
    }

    [Fact]
    public void FilmDto_ExposureDatesList_WithEmptyList_ShouldReturnEmptyString()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Test Film",
            Iso = "400",
            Type = "ColorNegative",
            PurchasedBy = "Angel"
        };

        // Act
        filmDto.ExposureDatesList = new List<ExposureDateEntry>();

        // Assert
        Assert.Equal(string.Empty, filmDto.ExposureDates);
    }
}