using AnalogAgenda.Server.Controllers;
using AnalogAgenda.Server.Identity;
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
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using System.Security.Principal;

namespace AnalogAgenda.Server.Tests.Controllers;

public class FilmSearchTests : IDisposable
{
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly IDatabaseService _databaseService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly FilmController _controller;
    private readonly Storage _storageConfig;

    public FilmSearchTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"FilmSearchTestDb_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_dbContext);
        _mockBlobService = new Mock<IBlobService>();
        _storageConfig = new Storage { AccountName = "test" };
        
        var systemConfig = new Configuration.Sections.System { IsDev = false };
        var dtoConvertor = new DtoConvertor(systemConfig, _storageConfig);
        var entityConvertor = new EntityConvertor();
        
        _controller = new FilmController(_databaseService, _mockBlobService.Object, dtoConvertor, entityConvertor);
        
        // Setup mock user identity
        var claims = new List<Claim> { new(ClaimTypes.Name, "Angel") };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task GetDevelopedFilms_WithSearchParams_AppliesFilters()
    {
        // Arrange
        var film1 = new FilmEntity { Id = "1", Brand = "Test Film 1", Type = EFilmType.ColorNegative, Developed = true, Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Brand = "Test Film 2", Type = EFilmType.ColorPositive, Developed = true, Iso = "200" };
        var film3 = new FilmEntity { Id = "3", Brand = "Other Film", Type = EFilmType.ColorNegative, Developed = true, Iso = "400" };

        await _databaseService.AddAsync(film1);
        await _databaseService.AddAsync(film2);
        await _databaseService.AddAsync(film3);

        var searchDto = new FilmSearchDto
        {
            Brand = "Test Film",
            Type = "ColorNegative",
            Page = 1,
            PageSize = 5
        };

        // Act
        var result = await _controller.GetDevelopedFilms(searchDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        // Should filter by brand and type
        Assert.Single(pagedResponse.Data);
        Assert.Equal("Test Film 1", pagedResponse.Data.First().Brand);
    }

    [Fact]
    public async Task GetMyDevelopedFilms_WithSearchParams_AppliesFilters()
    {
        // Arrange
        var film1 = new FilmEntity { Id = "1", Brand = "My Film 1", PurchasedBy = EUsernameType.Angel, Developed = true, Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Brand = "Other Film", PurchasedBy = EUsernameType.Cristiana, Developed = true, Iso = "200" };
        var film3 = new FilmEntity { Id = "3", Brand = "My Film 2", PurchasedBy = EUsernameType.Angel, Developed = true, Iso = "400" };

        await _databaseService.AddAsync(film1);
        await _databaseService.AddAsync(film2);
        await _databaseService.AddAsync(film3);

        var searchDto = new FilmSearchDto
        {
            Brand = "My Film",
            Page = 1,
            PageSize = 5
        };

        // Act
        var result = await _controller.GetMyDevelopedFilms(searchDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        // Should filter by user and brand
        Assert.Equal(2, pagedResponse.Data.Count());
        Assert.All(pagedResponse.Data, film => Assert.Contains("My Film", film.Brand));
    }

    [Fact]
    public async Task GetNotDevelopedFilms_WithEmptySearchParams_ReturnsAllNotDeveloped()
    {
        // Arrange
        var film1 = new FilmEntity { Id = "1", Brand = "Film 1", Developed = false, Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Brand = "Film 2", Developed = false, Iso = "200" };
        var film3 = new FilmEntity { Id = "3", Brand = "Film 3", Developed = true, Iso = "400" };

        await _databaseService.AddAsync(film1);
        await _databaseService.AddAsync(film2);
        await _databaseService.AddAsync(film3);

        var searchDto = new FilmSearchDto
        {
            Page = 1,
            PageSize = 5
        };

        // Act
        var result = await _controller.GetNotDevelopedFilms(searchDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        Assert.Equal(2, pagedResponse.Data.Count());
    }

    [Fact]
    public async Task GetMyNotDevelopedFilms_WithDevKitFilter_AppliesFilter()
    {
        // Arrange
        var film1 = new FilmEntity { Id = "1", Brand = "Film 1", PurchasedBy = EUsernameType.Angel, Developed = false, DevelopedWithDevKitId = "devkit1", Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Brand = "Film 2", PurchasedBy = EUsernameType.Angel, Developed = false, DevelopedWithDevKitId = "devkit2", Iso = "200" };

        await _databaseService.AddAsync(film1);
        await _databaseService.AddAsync(film2);

        var searchDto = new FilmSearchDto
        {
            DevelopedWithDevKitId = "devkit1",
            Page = 1,
            PageSize = 5
        };

        // Act
        var result = await _controller.GetMyNotDevelopedFilms(searchDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        Assert.Single(pagedResponse.Data);
        Assert.Equal("Film 1", pagedResponse.Data.First().Brand);
    }

    [Fact]
    public async Task GetDevelopedFilms_WithSessionFilter_AppliesFilter()
    {
        // Arrange
        var film1 = new FilmEntity { Id = "1", Brand = "Film 1", Developed = true, DevelopedInSessionId = "session1", Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Brand = "Film 2", Developed = true, DevelopedInSessionId = "session2", Iso = "200" };

        await _databaseService.AddAsync(film1);
        await _databaseService.AddAsync(film2);

        var searchDto = new FilmSearchDto
        {
            DevelopedInSessionId = "session1",
            Page = 1,
            PageSize = 5
        };

        // Act
        var result = await _controller.GetDevelopedFilms(searchDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        Assert.Single(pagedResponse.Data);
        Assert.Equal("Film 1", pagedResponse.Data.First().Brand);
    }

    [Fact]
    public async Task GetDevelopedFilms_WithMultipleFilters_AppliesAllFilters()
    {
        // Arrange
        var film1 = new FilmEntity { Id = "1", Brand = "Test Film 1", Type = EFilmType.ColorNegative, PurchasedBy = EUsernameType.Angel, Developed = true, Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Brand = "Test Film 2", Type = EFilmType.ColorPositive, PurchasedBy = EUsernameType.Angel, Developed = true, Iso = "200" };
        var film3 = new FilmEntity { Id = "3", Brand = "Other Film", Type = EFilmType.ColorNegative, PurchasedBy = EUsernameType.Cristiana, Developed = true, Iso = "400" };

        await _databaseService.AddAsync(film1);
        await _databaseService.AddAsync(film2);
        await _databaseService.AddAsync(film3);

        var searchDto = new FilmSearchDto
        {
            Brand = "Test",
            Type = "ColorNegative",
            PurchasedBy = "Angel",
            Page = 1,
            PageSize = 5
        };

        // Act
        var result = await _controller.GetDevelopedFilms(searchDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        // Should match all criteria
        Assert.Single(pagedResponse.Data);
        Assert.Equal("Test Film 1", pagedResponse.Data.First().Brand);
    }

    [Fact]
    public async Task GetDevelopedFilms_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var films = new List<FilmEntity>
        {
            new() { Id = "1", Brand = "Film 1", Developed = true, Iso = "400" },
            new() { Id = "2", Brand = "Film 2", Developed = true, Iso = "200" },
            new() { Id = "3", Brand = "Film 3", Developed = true, Iso = "400" },
            new() { Id = "4", Brand = "Film 4", Developed = true, Iso = "200" },
            new() { Id = "5", Brand = "Film 5", Developed = true, Iso = "400" }
        };

        foreach (var film in films)
        {
            await _databaseService.AddAsync(film);
        }

        var searchDto = new FilmSearchDto
        {
            Page = 2,
            PageSize = 2
        };

        // Act
        var result = await _controller.GetDevelopedFilms(searchDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        Assert.Equal(2, pagedResponse.Data.Count());
        Assert.Equal(2, pagedResponse.CurrentPage);
        Assert.Equal(2, pagedResponse.PageSize);
        Assert.Equal(5, pagedResponse.TotalCount);
    }

    [Fact]
    public async Task GetDevelopedFilms_WithNoPurchasedByFilter_ReturnsFilmsFromAllUsers()
    {
        var angelFilm = new FilmEntity
        {
            Id = "angel-1",
            Brand = "Angel Film",
            PurchasedBy = EUsernameType.Angel,
            Developed = true,
            Iso = "400"
        };
        var cristianaFilm = new FilmEntity
        {
            Id = "cristiana-1",
            Brand = "Cristiana Film",
            PurchasedBy = EUsernameType.Cristiana,
            Developed = true,
            Iso = "200"
        };
        await _databaseService.AddAsync(angelFilm);
        await _databaseService.AddAsync(cristianaFilm);

        var searchDto = new FilmSearchDto { Page = 1, PageSize = 10 };

        var result = await _controller.GetDevelopedFilms(searchDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        Assert.Equal(2, pagedResponse.TotalCount);
        var brands = pagedResponse.Data.Select(f => f.Brand).OrderBy(b => b).ToList();
        Assert.Contains("Angel Film", brands);
        Assert.Contains("Cristiana Film", brands);
    }

    [Fact]
    public async Task GetDevelopedFilms_WithPurchasedByFilter_ReturnsOnlyThatOwnersFilms()
    {
        var angelFilm = new FilmEntity
        {
            Id = "angel-1",
            Brand = "Angel Film",
            PurchasedBy = EUsernameType.Angel,
            Developed = true,
            Iso = "400"
        };
        var cristianaFilm = new FilmEntity
        {
            Id = "cristiana-1",
            Brand = "Cristiana Film",
            PurchasedBy = EUsernameType.Cristiana,
            Developed = true,
            Iso = "200"
        };
        await _databaseService.AddAsync(angelFilm);
        await _databaseService.AddAsync(cristianaFilm);

        var searchDto = new FilmSearchDto
        {
            Page = 1,
            PageSize = 10,
            PurchasedBy = "Angel"
        };

        var result = await _controller.GetDevelopedFilms(searchDto);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        Assert.Single(pagedResponse.Data);
        Assert.Equal("Angel Film", pagedResponse.Data.Single().Brand);
    }
}

