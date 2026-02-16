using System.Security.Claims;
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AnalogAgenda.Server.Tests.Controllers;

public class PhotoControllerTests : IDisposable
{
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly IDatabaseService _databaseService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<BlobContainerClient> _mockPhotosContainerClient;
    private readonly Mock<BlobContainerClient> _mockFilmsContainerClient;
    private readonly Mock<BlobClient> _mockBlobClient;
    private readonly Storage _storageConfig;
    private readonly DtoConvertor _dtoConvertor;
    private readonly EntityConvertor _entityConvertor;
    private readonly PhotoController _controller;

    public PhotoControllerTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"PhotoTestDb_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_dbContext);
        _mockBlobService = new Mock<IBlobService>();
        _mockPhotosContainerClient = new Mock<BlobContainerClient>();
        _mockFilmsContainerClient = new Mock<BlobContainerClient>();
        _mockBlobClient = new Mock<BlobClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        var systemConfig = new Configuration.Sections.System { IsDev = false };
        _dtoConvertor = new DtoConvertor(systemConfig, _storageConfig);
        _entityConvertor = new EntityConvertor();

        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.photos))
                       .Returns(_mockPhotosContainerClient.Object);
        
        _mockPhotosContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                                 .Returns(_mockBlobClient.Object);

        _controller = new PhotoController(_databaseService, _mockBlobService.Object, _dtoConvertor, _entityConvertor);

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, nameof(EUsernameType.Angel)) }, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
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
    public async Task GetPreview_WithNonExistentBlob_ReturnsNotFound()
    {
        // Arrange
        var photoId = "test-photo-id";
        var imageId = Guid.NewGuid();
        var filmId = "test-film-id";
        var film = new FilmEntity { Id = filmId, Brand = "Test Film", Iso = "400" };
        await _databaseService.AddAsync(film);

        var photo = new PhotoEntity { Id = photoId, FilmId = filmId, Index = 1, ImageId = imageId };
        await _databaseService.AddAsync(photo);

        var previewBlobClient = new Mock<BlobClient>();
        previewBlobClient.Setup(x => x.ExistsAsync(default)).ReturnsAsync(Azure.Response.FromValue(false, Mock.Of<Azure.Response>()));
        
        _mockPhotosContainerClient.Setup(x => x.GetBlobClient($"preview/{imageId}"))
                                   .Returns(previewBlobClient.Object);

        // Act
        var result = await _controller.GetPreview(photoId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetPhotosByFilmId_WithValidFilmId_ReturnsOkWithPhotos()
    {
        // Arrange
        var filmId = "test-film-id";
        var film = new FilmEntity { Id = filmId, Brand = "Test Film", Iso = "400", PurchasedBy = EUsernameType.Angel };
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
        var film = new FilmEntity { Id = filmId, Brand = "Test Film", Iso = "400", PurchasedBy = EUsernameType.Angel };
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
        var film = new FilmEntity { Id = filmId, Brand = "Test Film", Iso = "400", PurchasedBy = EUsernameType.Angel };
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
        var filmId = "test-film";
        var film = new FilmEntity { Id = filmId, Brand = "Test Film", Iso = "400", PurchasedBy = EUsernameType.Angel };
        await _databaseService.AddAsync(film);

        var id = "test-photo-key";
        var photo = new PhotoEntity { Id = id, FilmId = filmId, Index = 1, ImageId = Guid.NewGuid() };
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

    [Fact]
    public async Task SetPhotoRestricted_AsOwner_WithValidPhoto_ReturnsOkAndUpdatesRestricted()
    {
        var filmId = "film-1";
        var film = new FilmEntity { Id = filmId, Brand = "Test", Iso = "400", PurchasedBy = EUsernameType.Angel };
        await _databaseService.AddAsync(film);
        var photoId = "photo-1";
        var photo = new PhotoEntity { Id = photoId, FilmId = filmId, Index = 1, ImageId = Guid.NewGuid(), Restricted = false };
        await _databaseService.AddAsync(photo);

        var result = await _controller.SetPhotoRestricted(photoId, new SetRestrictedDto { Restricted = true });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PhotoDto>(okResult.Value);
        Assert.True(dto.Restricted);
        var updated = await _databaseService.GetByIdAsync<PhotoEntity>(photoId);
        Assert.NotNull(updated);
        Assert.True(updated!.Restricted);
    }

    [Fact]
    public async Task SetPhotoRestricted_AsOwner_SetToFalse_ReturnsOk()
    {
        var filmId = "film-1";
        var film = new FilmEntity { Id = filmId, Brand = "Test", Iso = "400", PurchasedBy = EUsernameType.Angel };
        await _databaseService.AddAsync(film);
        var photoId = "photo-1";
        var photo = new PhotoEntity { Id = photoId, FilmId = filmId, Index = 1, ImageId = Guid.NewGuid(), Restricted = true };
        await _databaseService.AddAsync(photo);

        var result = await _controller.SetPhotoRestricted(photoId, new SetRestrictedDto { Restricted = false });

        Assert.IsType<OkObjectResult>(result);
        var updated = await _databaseService.GetByIdAsync<PhotoEntity>(photoId);
        Assert.NotNull(updated);
        Assert.False(updated!.Restricted);
    }

    [Fact]
    public async Task SetPhotoRestricted_NonOwner_ReturnsForbid()
    {
        var filmId = "film-1";
        var film = new FilmEntity { Id = filmId, Brand = "Test", Iso = "400", PurchasedBy = EUsernameType.Cristiana };
        await _databaseService.AddAsync(film);
        var photoId = "photo-1";
        var photo = new PhotoEntity { Id = photoId, FilmId = filmId, Index = 1, ImageId = Guid.NewGuid() };
        await _databaseService.AddAsync(photo);

        var result = await _controller.SetPhotoRestricted(photoId, new SetRestrictedDto { Restricted = true });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task SetPhotoRestricted_NonExistentPhoto_ReturnsNotFound()
    {
        var result = await _controller.SetPhotoRestricted("non-existent", new SetRestrictedDto { Restricted = true });
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SetPhotoRestricted_PhotoExistsButFilmMissing_ReturnsForbid()
    {
        var photoId = "photo-orphan";
        var photo = new PhotoEntity { Id = photoId, FilmId = "missing-film", Index = 1, ImageId = Guid.NewGuid() };
        await _databaseService.AddAsync(photo);

        var result = await _controller.SetPhotoRestricted(photoId, new SetRestrictedDto { Restricted = true });

        Assert.IsType<ForbidResult>(result);
    }
}

