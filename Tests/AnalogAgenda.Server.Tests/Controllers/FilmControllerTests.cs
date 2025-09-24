using AnalogAgenda.Server.Controllers;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AnalogAgenda.Server.Tests.Controllers;

public class FilmControllerTests
{
    private readonly Mock<ITableService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<TableClient> _mockFilmsTableClient;
    private readonly Mock<TableClient> _mockPhotosTableClient;
    private readonly Mock<BlobContainerClient> _mockFilmsContainerClient;
    private readonly Mock<BlobContainerClient> _mockPhotosContainerClient;
    private readonly Storage _storageConfig;
    private readonly FilmController _controller;

    public FilmControllerTests()
    {
        _mockTableService = new Mock<ITableService>();
        _mockBlobService = new Mock<IBlobService>();
        _mockFilmsTableClient = new Mock<TableClient>();
        _mockPhotosTableClient = new Mock<TableClient>();
        _mockFilmsContainerClient = new Mock<BlobContainerClient>();
        _mockPhotosContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        _mockTableService.Setup(x => x.GetTable(TableName.Films))
                        .Returns(_mockFilmsTableClient.Object);
        
        _mockTableService.Setup(x => x.GetTable(TableName.Photos))
                        .Returns(_mockPhotosTableClient.Object);
        
        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.films))
                       .Returns(_mockFilmsContainerClient.Object);
                       
        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.photos))
                       .Returns(_mockPhotosContainerClient.Object);

        _controller = new FilmController(_storageConfig, _mockTableService.Object, _mockBlobService.Object);
        
        // Setup controller context for user authentication
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };
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

        var pagedResponse = new PagedResponseDto<FilmEntity>
        {
            Data = filmEntities,
            TotalCount = filmEntities.Count,
            PageSize = 10,
            CurrentPage = 1
        };

        _mockTableService.Setup(x => x.GetTableEntriesPagedAsync<FilmEntity>(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(pagedResponse);

        // Act
        var result = await _controller.GetAllFilms();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        Assert.Single(pagedResult.Data);
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
        
        // Mock photo deletion dependencies
        _mockTableService.Setup(x => x.GetTableEntriesAsync<PhotoEntity>(It.IsAny<System.Linq.Expressions.Expression<System.Func<PhotoEntity, bool>>>()))
                        .ReturnsAsync(new List<PhotoEntity>());
                        
        // Mock BlobClient for photo deletion
        var mockBlobClient = new Mock<BlobClient>();
        _mockPhotosContainerClient.Setup(x => x.GetBlobClient(It.IsAny<string>()))
                                 .Returns(mockBlobClient.Object);

        // Act
        var result = await _controller.DeleteFilm(rowKey);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    #region My Films Tests

    [Fact]
    public async Task GetMyDevelopedFilms_WithValidUser_ReturnsUserFilms()
    {
        // Arrange
        SetupAuthenticatedUser("Angel");

        var allFilms = new List<FilmEntity>
        {
            new FilmEntity { Name = "Angel's Film 1", PurchasedBy = EUsernameType.Angel, Developed = true, PurchasedOn = DateTime.UtcNow.AddDays(-1), RowKey = "key1" },
            new FilmEntity { Name = "Tudor's Film", PurchasedBy = EUsernameType.Tudor, Developed = true, PurchasedOn = DateTime.UtcNow.AddDays(-2), RowKey = "key2" },
            new FilmEntity { Name = "Angel's Film 2", PurchasedBy = EUsernameType.Angel, Developed = true, PurchasedOn = DateTime.UtcNow, RowKey = "key3" }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<FilmEntity>(It.IsAny<System.Linq.Expressions.Expression<System.Func<FilmEntity, bool>>>()))
                        .ReturnsAsync(allFilms.Where(f => f.Developed).ToList());

        // Act
        var result = await _controller.GetMyDevelopedFilms(page: 0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var films = Assert.IsAssignableFrom<IEnumerable<FilmDto>>(okResult.Value);
        var filmList = films.ToList();
        
        Assert.Equal(2, filmList.Count);
        Assert.All(filmList, film => Assert.Equal("Angel", film.PurchasedBy));
        Assert.Equal("Angel's Film 2", filmList.First().Name); // Should be ordered by PurchasedOn desc
    }

    [Fact]
    public async Task GetMyDevelopedFilms_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        SetupAuthenticatedUser("Angel");

        var allDevelopedFilms = new List<FilmEntity>();
        for (int i = 1; i <= 10; i++)
        {
            allDevelopedFilms.Add(new FilmEntity 
            { 
                Name = $"Angel's Film {i}", 
                PurchasedBy = EUsernameType.Angel, 
                Developed = true, 
                PurchasedOn = DateTime.UtcNow.AddDays(-i),
                RowKey = $"key{i}" 
            });
        }

        _mockTableService.Setup(x => x.GetTableEntriesAsync<FilmEntity>(It.IsAny<System.Linq.Expressions.Expression<System.Func<FilmEntity, bool>>>()))
                        .ReturnsAsync(allDevelopedFilms);

        // Act
        var result = await _controller.GetMyDevelopedFilms(page: 2, pageSize: 3);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        Assert.Equal(3, pagedResponse.Data.Count());
        Assert.Equal(10, pagedResponse.TotalCount);
        Assert.Equal(3, pagedResponse.PageSize);
        Assert.Equal(2, pagedResponse.CurrentPage);
        Assert.True(pagedResponse.HasNextPage);
        Assert.True(pagedResponse.HasPreviousPage);
    }

    [Fact]
    public async Task GetMyNotDevelopedFilms_WithValidUser_ReturnsUserFilms()
    {
        // Arrange
        SetupAuthenticatedUser("Tudor");

        var allFilms = new List<FilmEntity>
        {
            new FilmEntity { Name = "Tudor's Film 1", PurchasedBy = EUsernameType.Tudor, Developed = false, PurchasedOn = DateTime.UtcNow.AddDays(-1), RowKey = "key1" },
            new FilmEntity { Name = "Angel's Film", PurchasedBy = EUsernameType.Angel, Developed = false, PurchasedOn = DateTime.UtcNow.AddDays(-2), RowKey = "key2" },
            new FilmEntity { Name = "Tudor's Film 2", PurchasedBy = EUsernameType.Tudor, Developed = false, PurchasedOn = DateTime.UtcNow, RowKey = "key3" }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<FilmEntity>(It.IsAny<System.Linq.Expressions.Expression<System.Func<FilmEntity, bool>>>()))
                        .ReturnsAsync(allFilms.Where(f => !f.Developed).ToList());

        // Act
        var result = await _controller.GetMyNotDevelopedFilms(page: 0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var films = Assert.IsAssignableFrom<IEnumerable<FilmDto>>(okResult.Value);
        var filmList = films.ToList();
        
        Assert.Equal(2, filmList.Count);
        Assert.All(filmList, film => Assert.Equal("Tudor", film.PurchasedBy));
        Assert.Equal("Tudor's Film 2", filmList.First().Name); // Should be ordered by PurchasedOn desc
    }

    [Fact]
    public async Task GetMyDevelopedFilms_WithUnauthenticatedUser_ThrowsException()
    {
        // Arrange - No user authentication setup (no claims)

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => 
            await _controller.GetMyDevelopedFilms());
    }

    [Fact]
    public async Task GetMyNotDevelopedFilms_WithUnauthenticatedUser_ThrowsException()
    {
        // Arrange - No user authentication setup (no claims)

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(async () => 
            await _controller.GetMyNotDevelopedFilms());
    }

    [Fact]
    public async Task GetMyDevelopedFilms_WithEmptyUsername_ReturnsUnauthorized()
    {
        // Arrange - Setup user with empty name claim
        SetupAuthenticatedUser("");

        // Act
        var result = await _controller.GetMyDevelopedFilms();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetMyNotDevelopedFilms_WithEmptyUsername_ReturnsUnauthorized()
    {
        // Arrange - Setup user with empty name claim
        SetupAuthenticatedUser("");

        // Act
        var result = await _controller.GetMyNotDevelopedFilms();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task GetMyDevelopedFilms_WithEmptyResults_ReturnsEmptyList()
    {
        // Arrange
        SetupAuthenticatedUser("Angel");

        _mockTableService.Setup(x => x.GetTableEntriesAsync<FilmEntity>(It.IsAny<System.Linq.Expressions.Expression<System.Func<FilmEntity, bool>>>()))
                        .ReturnsAsync(new List<FilmEntity>());

        // Act
        var result = await _controller.GetMyDevelopedFilms(page: 0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var films = Assert.IsAssignableFrom<IEnumerable<FilmDto>>(okResult.Value);
        Assert.Empty(films);
    }

    [Fact]
    public async Task GetMyDevelopedFilms_LastPage_ReturnsCorrectPaginationInfo()
    {
        // Arrange
        SetupAuthenticatedUser("Angel");

        var allDevelopedFilms = new List<FilmEntity>
        {
            new FilmEntity { Name = "Film 1", PurchasedBy = EUsernameType.Angel, Developed = true, PurchasedOn = DateTime.UtcNow, RowKey = "key1" },
            new FilmEntity { Name = "Film 2", PurchasedBy = EUsernameType.Angel, Developed = true, PurchasedOn = DateTime.UtcNow, RowKey = "key2" }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<FilmEntity>(It.IsAny<System.Linq.Expressions.Expression<System.Func<FilmEntity, bool>>>()))
                        .ReturnsAsync(allDevelopedFilms);

        // Act
        var result = await _controller.GetMyDevelopedFilms(page: 1, pageSize: 5);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        Assert.Equal(2, pagedResponse.Data.Count());
        Assert.Equal(2, pagedResponse.TotalCount);
        Assert.Equal(1, pagedResponse.CurrentPage);
        Assert.False(pagedResponse.HasNextPage);
        Assert.False(pagedResponse.HasPreviousPage);
    }

    #endregion

    #region Helper Methods

    private void SetupAuthenticatedUser(string username)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, username)
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        
        _controller.ControllerContext.HttpContext.User = principal;
    }

    #endregion
}
