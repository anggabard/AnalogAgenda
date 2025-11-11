using AnalogAgenda.Server.Controllers;
using AnalogAgenda.Server.Services.Interfaces;
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

public class PhotoControllerTests : IDisposable
{
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly IDatabaseService _databaseService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<IImageCacheService> _mockImageCacheService;
    private readonly Mock<BlobContainerClient> _mockPhotosContainerClient;
    private readonly Mock<BlobContainerClient> _mockFilmsContainerClient;
    private readonly Mock<BlobClient> _mockBlobClient;
    private readonly Storage _storageConfig;
    private readonly PhotoController _controller;

    public PhotoControllerTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"PhotoTestDb_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_dbContext);
        _mockBlobService = new Mock<IBlobService>();
        _mockImageCacheService = new Mock<IImageCacheService>();
        _mockPhotosContainerClient = new Mock<BlobContainerClient>();
        _mockFilmsContainerClient = new Mock<BlobContainerClient>();
        _mockBlobClient = new Mock<BlobClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.photos))
                       .Returns(_mockPhotosContainerClient.Object);
        
        _mockPhotosContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                                 .Returns(_mockBlobClient.Object);

        _controller = new PhotoController(_storageConfig, _databaseService, _mockBlobService.Object, _mockImageCacheService.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetPreview_WithNonExistentPhoto_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existent-photo";

        // Act
        var result = await _controller.GetPreview(id);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetPreview_WithCachedImage_ReturnsFromCache()
    {
        // Arrange
        var photoId = "test-photo-id";
        var imageId = Guid.NewGuid();
        var filmId = "test-film-id";
        var film = new FilmEntity { Id = filmId, Name = "Test Film", Iso = "400" };
        await _databaseService.AddAsync(film);

        var photo = new PhotoEntity { Id = photoId, FilmId = filmId, Index = 1, ImageId = imageId };
        await _databaseService.AddAsync(photo);

        var cachedBytes = new byte[] { 1, 2, 3, 4, 5 };
        var contentType = "image/jpeg";
        (byte[] imageBytes, string contentType)? cachedImage = (cachedBytes, contentType);

        _mockImageCacheService.Setup(x => x.TryGetPreview(imageId, out cachedImage))
            .Returns(true);

        // Act
        var result = await _controller.GetPreview(photoId);

        // Assert
        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(cachedBytes, fileResult.FileContents);
        Assert.Equal(contentType, fileResult.ContentType);
        
        // Verify cache was checked
        _mockImageCacheService.Verify(x => x.TryGetPreview(imageId, out cachedImage), Times.Once);
    }

    [Fact]
    public async Task GetPhotosByFilmId_WithValidFilmId_ReturnsOkWithPhotos()
    {
        // Arrange
        var filmId = "test-film-id";
        var film = new FilmEntity { Id = filmId, Name = "Test Film", Iso = "400" };
        await _databaseService.AddAsync(film);

        var photo1 = new PhotoEntity { FilmId = filmId, Index = 2, Id = "photo2", ImageId = Guid.NewGuid() };
        var photo2 = new PhotoEntity { FilmId = filmId, Index = 1, Id = "photo1", ImageId = Guid.NewGuid() };
        var photo3 = new PhotoEntity { FilmId = filmId, Index = 3, Id = "photo3", ImageId = Guid.NewGuid() };

        await _databaseService.AddAsync(photo1);
        await _databaseService.AddAsync(photo2);
        await _databaseService.AddAsync(photo3);

        // Act
        var result = await _controller.GetPhotosByFilmId(filmId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var photos = Assert.IsType<List<PhotoDto>>(okResult.Value);
        Assert.Equal(3, photos.Count);
        
        // Verify photos are sorted by index
        Assert.Equal(1, photos[0].Index);
        Assert.Equal(2, photos[1].Index);
        Assert.Equal(3, photos[2].Index);
    }

    [Fact]
    public async Task GetPhotosByFilmId_WithNoPhotos_ReturnsEmptyList()
    {
        // Arrange
        var filmId = "test-film-id";
        var film = new FilmEntity { Id = filmId, Name = "Test Film", Iso = "400" };
        await _databaseService.AddAsync(film);

        // Act
        var result = await _controller.GetPhotosByFilmId(filmId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var photos = Assert.IsType<List<PhotoDto>>(okResult.Value);
        Assert.Empty(photos);
    }

    [Fact]
    public async Task DownloadPhoto_WithNonExistentPhoto_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existent-photo";

        // Act
        var result = await _controller.DownloadPhoto(id);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DownloadAllPhotos_WithNonExistentFilm_ReturnsNotFound()
    {
        // Arrange
        var filmId = "non-existent-film";

        // Act
        var result = await _controller.DownloadAllPhotos(filmId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DownloadAllPhotos_WithNoPhotos_ReturnsNotFound()
    {
        // Arrange
        var filmId = "test-film-id";
        var film = new FilmEntity { Id = filmId, Name = "Test Film", Iso = "400" };
        await _databaseService.AddAsync(film);

        // Act
        var result = await _controller.DownloadAllPhotos(filmId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeletePhoto_WithValidId_ReturnsNoContent()
    {
        // Arrange
        var id = "test-photo-key";
        var photo = new PhotoEntity { Id = id, FilmId = "test-film", Index = 1, ImageId = Guid.NewGuid() };
        await _databaseService.AddAsync(photo);

        // Act
        var result = await _controller.DeletePhoto(id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify photo was deleted
        var deletedPhoto = await _databaseService.GetByIdAsync<PhotoEntity>(id);
        Assert.Null(deletedPhoto);
    }

    [Fact]
    public async Task DeletePhoto_WithNonExistentPhoto_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existent-photo";

        // Act
        var result = await _controller.DeletePhoto(id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}

