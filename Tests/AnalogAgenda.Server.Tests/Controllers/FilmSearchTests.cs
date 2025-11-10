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
        
        _controller = new FilmController(_storageConfig, _databaseService, _mockBlobService.Object  );
        
        // Setup mock user identity
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "Angel") };
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
        var film1 = new FilmEntity { Id = "1", Name = "Test Film 1", Type = EFilmType.ColorNegative, Developed = true, Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Name = "Test Film 2", Type = EFilmType.ColorPositive, Developed = true, Iso = "200" };
        var film3 = new FilmEntity { Id = "3", Name = "Other Film", Type = EFilmType.ColorNegative, Developed = true, Iso = "400" };

        await _databaseService.AddAsync(film1);
        await _databaseService.AddAsync(film2);
        await _databaseService.AddAsync(film3);

        var searchDto = new FilmSearchDto
        {
            Name = "Test Film",
            Type = "ColorNegative",
            Page = 1,
            PageSize = 5
        };

        // Act
        var result = await _controller.GetDevelopedFilms(searchDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        // Should filter by name and type
        Assert.Single(pagedResponse.Data);
        Assert.Equal("Test Film 1", pagedResponse.Data.First().Name);
    }

    [Fact]
    public async Task GetMyDevelopedFilms_WithSearchParams_AppliesFilters()
    {
        // Arrange
        var film1 = new FilmEntity { Id = "1", Name = "My Film 1", PurchasedBy = EUsernameType.Angel, Developed = true, Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Name = "Other Film", PurchasedBy = EUsernameType.Cristiana, Developed = true, Iso = "200" };
        var film3 = new FilmEntity { Id = "3", Name = "My Film 2", PurchasedBy = EUsernameType.Angel, Developed = true, Iso = "400" };

        await _databaseService.AddAsync(film1);
        await _databaseService.AddAsync(film2);
        await _databaseService.AddAsync(film3);

        var searchDto = new FilmSearchDto
        {
            Name = "My Film",
            Page = 1,
            PageSize = 5
        };

        // Act
        var result = await _controller.GetMyDevelopedFilms(searchDto);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResponse = Assert.IsType<PagedResponseDto<FilmDto>>(okResult.Value);
        
        // Should filter by user and name
        Assert.Equal(2, pagedResponse.Data.Count());
        Assert.All(pagedResponse.Data, film => Assert.Contains("My Film", film.Name));
    }

    [Fact]
    public async Task GetNotDevelopedFilms_WithEmptySearchParams_ReturnsAllNotDeveloped()
    {
        // Arrange
        var film1 = new FilmEntity { Id = "1", Name = "Film 1", Developed = false, Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Name = "Film 2", Developed = false, Iso = "200" };
        var film3 = new FilmEntity { Id = "3", Name = "Film 3", Developed = true, Iso = "400" };

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
        var film1 = new FilmEntity { Id = "1", Name = "Film 1", PurchasedBy = EUsernameType.Angel, Developed = false, DevelopedWithDevKitId = "devkit1", Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Name = "Film 2", PurchasedBy = EUsernameType.Angel, Developed = false, DevelopedWithDevKitId = "devkit2", Iso = "200" };

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
        Assert.Equal("Film 1", pagedResponse.Data.First().Name);
    }

    [Fact]
    public async Task GetDevelopedFilms_WithSessionFilter_AppliesFilter()
    {
        // Arrange
        var film1 = new FilmEntity { Id = "1", Name = "Film 1", Developed = true, DevelopedInSessionId = "session1", Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Name = "Film 2", Developed = true, DevelopedInSessionId = "session2", Iso = "200" };

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
        Assert.Equal("Film 1", pagedResponse.Data.First().Name);
    }

    [Fact]
    public async Task GetDevelopedFilms_WithMultipleFilters_AppliesAllFilters()
    {
        // Arrange
        var film1 = new FilmEntity { Id = "1", Name = "Test Film 1", Type = EFilmType.ColorNegative, PurchasedBy = EUsernameType.Angel, Developed = true, Iso = "400" };
        var film2 = new FilmEntity { Id = "2", Name = "Test Film 2", Type = EFilmType.ColorPositive, PurchasedBy = EUsernameType.Angel, Developed = true, Iso = "200" };
        var film3 = new FilmEntity { Id = "3", Name = "Other Film", Type = EFilmType.ColorNegative, PurchasedBy = EUsernameType.Cristiana, Developed = true, Iso = "400" };

        await _databaseService.AddAsync(film1);
        await _databaseService.AddAsync(film2);
        await _databaseService.AddAsync(film3);

        var searchDto = new FilmSearchDto
        {
            Name = "Test",
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
        Assert.Equal("Test Film 1", pagedResponse.Data.First().Name);
    }

    [Fact]
    public async Task GetDevelopedFilms_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var films = new List<FilmEntity>
        {
            new FilmEntity { Id = "1", Name = "Film 1", Developed = true, Iso = "400" },
            new FilmEntity { Id = "2", Name = "Film 2", Developed = true, Iso = "200" },
            new FilmEntity { Id = "3", Name = "Film 3", Developed = true, Iso = "400" },
            new FilmEntity { Id = "4", Name = "Film 4", Developed = true, Iso = "200" },
            new FilmEntity { Id = "5", Name = "Film 5", Developed = true, Iso = "400" }
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
}

