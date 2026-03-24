using System.Security.Claims;
using AnalogAgenda.Server.Controllers;
using AnalogAgenda.Server.Tests.Helpers;
using Configuration.Sections;
using Database.Data;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AnalogAgenda.Server.Tests.Controllers;

/// <summary>
/// Integration-style tests for idea photo linking and visibility (restricted + film ownership).
/// </summary>
public class IdeaControllerPhotoEndpointsTests : IDisposable
{
    private const string IdeaId = "i01";
    private const string FilmAngelId = "film00000001";
    private const string FilmTudorId = "film00000002";
    private const string PhotoOpenId = "photo00000000001";
    private const string PhotoRestrictedId = "photo00000000002";
    private const string PhotoTudorFilmId = "photo00000000003";

    private readonly AnalogAgendaDbContext _dbContext;
    private readonly IDatabaseService _databaseService;
    private readonly DtoConvertor _dtoConvertor;
    private readonly IdeaController _controller;

    public IdeaControllerPhotoEndpointsTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"IdeaPhotoTests_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_dbContext);
        var systemConfig = new Configuration.Sections.System { IsDev = true };
        var storageConfig = new Storage { AccountName = "test", BlobEndpoint = "https://test.blob" };
        _dtoConvertor = new DtoConvertor(systemConfig, storageConfig);
        _controller = new IdeaController(_databaseService, _dtoConvertor, new EntityConvertor());
        Seed();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    private void SetUser(string username)
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, username) };
        var identity = new ClaimsIdentity(claims, "Test");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    private void Seed()
    {
        var idea = new IdeaEntity
        {
            Id = IdeaId,
            Title = "Test idea",
            Description = "",
            Outcome = "",
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        var filmAngel = new FilmEntity
        {
            Id = FilmAngelId,
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Developed = false,
            Description = "",
            Name = ""
        };

        var filmTudor = new FilmEntity
        {
            Id = FilmTudorId,
            Brand = "B",
            Iso = "200",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Tudor,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Developed = false,
            Description = "",
            Name = ""
        };

        _dbContext.Ideas.Add(idea);
        _dbContext.Films.AddRange(filmAngel, filmTudor);
        _dbContext.SaveChanges();

        var photoOpen = new PhotoEntity
        {
            Id = PhotoOpenId,
            FilmId = FilmAngelId,
            Index = 1,
            ImageId = Guid.NewGuid(),
            Restricted = false,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        var photoRestricted = new PhotoEntity
        {
            Id = PhotoRestrictedId,
            FilmId = FilmAngelId,
            Index = 2,
            ImageId = Guid.NewGuid(),
            Restricted = true,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        var photoTudorFilm = new PhotoEntity
        {
            Id = PhotoTudorFilmId,
            FilmId = FilmTudorId,
            Index = 1,
            ImageId = Guid.NewGuid(),
            Restricted = false,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _dbContext.Photos.AddRange(photoOpen, photoRestricted, photoTudorFilm);
        _dbContext.IdeaPhotos.AddRange(
            new IdeaPhotoEntity { IdeaId = IdeaId, PhotoId = PhotoOpenId },
            new IdeaPhotoEntity { IdeaId = IdeaId, PhotoId = PhotoRestrictedId },
            new IdeaPhotoEntity { IdeaId = IdeaId, PhotoId = PhotoTudorFilmId });
        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task GetIdeaPhotos_AngelSeesOwnRestrictedAndNonRestrictedEverywhere()
    {
        SetUser(nameof(EUsernameType.Angel));

        var result = await _controller.GetIdeaPhotos(IdeaId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<PhotoDto>>(ok.Value).ToList();
        // Non-restricted photos on any film + restricted photos only when user owns the film
        Assert.Equal(3, list.Count);
        Assert.Contains(list, p => p.Id == PhotoOpenId);
        Assert.Contains(list, p => p.Id == PhotoRestrictedId);
        Assert.Contains(list, p => p.Id == PhotoTudorFilmId);
    }

    [Fact]
    public async Task GetIdeaPhotos_TudorDoesNotSeeRestrictedPhotoOnAngelFilm()
    {
        SetUser(nameof(EUsernameType.Tudor));

        var result = await _controller.GetIdeaPhotos(IdeaId);

        var ok = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<PhotoDto>>(ok.Value).ToList();
        Assert.DoesNotContain(list, p => p.Id == PhotoRestrictedId);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, p => p.Id == PhotoOpenId);
        Assert.Contains(list, p => p.Id == PhotoTudorFilmId);
    }

    [Fact]
    public async Task AddPhotosToIdea_AngelCanLinkOwnFilmPhoto()
    {
        SetUser(nameof(EUsernameType.Angel));
        _dbContext.IdeaPhotos.RemoveRange(_dbContext.IdeaPhotos.Where(x => x.IdeaId == IdeaId));
        await _dbContext.SaveChangesAsync();

        var result = await _controller.AddPhotosToIdea(IdeaId, new IdListDto { Ids = [PhotoOpenId] });

        Assert.IsType<OkObjectResult>(result);
        var linked = await _dbContext.IdeaPhotos.Where(x => x.IdeaId == IdeaId).ToListAsync();
        Assert.Single(linked);
        Assert.Equal(PhotoOpenId, linked[0].PhotoId);
    }

    [Fact]
    public async Task AddPhotosToIdea_NonOwnerCannotLink()
    {
        SetUser(nameof(EUsernameType.Tudor));
        _dbContext.IdeaPhotos.RemoveRange(_dbContext.IdeaPhotos.Where(x => x.IdeaId == IdeaId));
        await _dbContext.SaveChangesAsync();

        var result = await _controller.AddPhotosToIdea(IdeaId, new IdListDto { Ids = [PhotoOpenId] });

        Assert.IsType<ForbidResult>(result);
        Assert.Empty(await _dbContext.IdeaPhotos.Where(x => x.IdeaId == IdeaId).ToListAsync());
    }

    [Fact]
    public async Task RemovePhotoFromIdea_OwnerCanUnlink()
    {
        SetUser(nameof(EUsernameType.Angel));

        var result = await _controller.RemovePhotoFromIdea(IdeaId, PhotoOpenId);

        Assert.IsType<NoContentResult>(result);
        Assert.False(await _dbContext.IdeaPhotos.AnyAsync(x => x.IdeaId == IdeaId && x.PhotoId == PhotoOpenId));
    }

    [Fact]
    public async Task RemovePhotoFromIdea_NonOwnerForbidden()
    {
        SetUser(nameof(EUsernameType.Tudor));

        var result = await _controller.RemovePhotoFromIdea(IdeaId, PhotoOpenId);

        Assert.IsType<ForbidResult>(result);
        Assert.True(await _dbContext.IdeaPhotos.AnyAsync(x => x.IdeaId == IdeaId && x.PhotoId == PhotoOpenId));
    }
}
