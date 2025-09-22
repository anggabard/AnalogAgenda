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

public class FilmControllerTests
{
    private readonly Mock<ITableService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<TableClient> _mockTableClient;
    private readonly Mock<BlobContainerClient> _mockContainerClient;
    private readonly Storage _storageConfig;
    private readonly FilmController _controller;

    public FilmControllerTests()
    {
        _mockTableService = new Mock<ITableService>();
        _mockBlobService = new Mock<IBlobService>();
        _mockTableClient = new Mock<TableClient>();
        _mockContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        _mockTableService.Setup(x => x.GetTable(TableName.Films))
                        .Returns(_mockTableClient.Object);
        
        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.films))
                       .Returns(_mockContainerClient.Object);

        _controller = new FilmController(_storageConfig, _mockTableService.Object, _mockBlobService.Object);
    }

    [Fact]
    public async Task CreateNewFilm_WithValidDto_ReturnsOk()
    {
        // Arrange
        var filmDto = new FilmDto
        {
            Name = "Kodak Portra 400",
            Iso = 400,
            Type = "Color Negative",
            NumberOfExposures = 36,
            Cost = 12.50,
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = "Professional color negative film",
            Developed = false,
            ImageBase64 = "" // No image
        };

        // Act
        var result = await _controller.CreateNewFilm(filmDto);

        // Assert
        Assert.IsType<OkResult>(result);
    }


    [Fact]
    public async Task GetAllFilms_ReturnsOkWithFilms()
    {
        // Arrange
        var filmEntities = new List<FilmEntity>
        {
            new FilmEntity
            {
                Name = "Kodak Portra 400",
                Iso = 400,
                Type = EFilmType.ColorNegative,
                NumberOfExposures = 36,
                Cost = 12.50,
                PurchasedBy = EUsernameType.Angel,
                PurchasedOn = DateTime.UtcNow,
                Description = "Professional color negative film",
                Developed = false,
                ImageId = Guid.NewGuid(),
                RowKey = "test-film-key"
            }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<FilmEntity>())
                        .ReturnsAsync(filmEntities);

        // Act
        var result = await _controller.GetAllFilms();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var films = Assert.IsAssignableFrom<IEnumerable<FilmDto>>(okResult.Value);
        Assert.Single(films);
    }

    [Fact]
    public async Task GetFilmByRowKey_WithValidKey_ReturnsOkWithFilm()
    {
        // Arrange
        var rowKey = "test-film-key";
        var filmEntity = new FilmEntity
        {
            RowKey = rowKey,
            Name = "Kodak Portra 400",
            Iso = 400,
            Type = EFilmType.ColorNegative,
            NumberOfExposures = 36,
            Cost = 12.50,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            Description = "Professional color negative film",
            Developed = false,
            ImageId = Guid.NewGuid()
        };

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<FilmEntity>(rowKey))
                        .ReturnsAsync(filmEntity);

        // Act
        var result = await _controller.GetFilmByRowKey(rowKey);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var film = Assert.IsType<FilmDto>(okResult.Value);
        Assert.Equal("Kodak Portra 400", film.Name);
    }


    [Fact]
    public async Task UpdateFilm_WithValidDto_ReturnsNoContent()
    {
        // Arrange
        var rowKey = "test-film-key";
        var updateDto = new FilmDto
        {
            Name = "Updated Film",
            Iso = 800,
            Type = "Black and White",
            NumberOfExposures = 24,
            Cost = 15.00,
            PurchasedBy = "Tudor",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            Description = "Updated description",
            Developed = true,
            ImageBase64 = ""
        };

        var existingEntity = new FilmEntity { RowKey = rowKey, Name = "Existing Film", ImageId = Guid.NewGuid() };
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<FilmEntity>(rowKey)).ReturnsAsync(existingEntity);

        // Act
        var result = await _controller.UpdateFilm(rowKey, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteFilm_WithValidKey_ReturnsNoContent()
    {
        // Arrange
        var rowKey = "test-film-key";
        var existingEntity = new FilmEntity { RowKey = rowKey, Name = "Film to Delete", ImageId = Guid.NewGuid() };
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<FilmEntity>(rowKey)).ReturnsAsync(existingEntity);

        // Act
        var result = await _controller.DeleteFilm(rowKey);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }
}
