using AnalogAgenda.Server.Controllers;
using AnalogAgenda.Server.Tests.Helpers;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.Data;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;

namespace AnalogAgenda.Server.Tests.Controllers;

public class CollectionControllerTests : IDisposable
{
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly IDatabaseService _databaseService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<BlobContainerClient> _mockPhotosContainerClient;
    private readonly Storage _storageConfig;
    private readonly DtoConvertor _dtoConvertor;
    private readonly CollectionController _controller;

    public CollectionControllerTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"CollectionTestDb_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_dbContext);
        _mockBlobService = new Mock<IBlobService>();
        _mockPhotosContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };
        var systemConfig = new Configuration.Sections.System { IsDev = false };
        _dtoConvertor = new DtoConvertor(systemConfig, _storageConfig);

        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.photos))
            .Returns(_mockPhotosContainerClient.Object);

        _controller = new CollectionController(_databaseService, _mockBlobService.Object, _dtoConvertor);

        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, nameof(EUsernameType.Angel)) }, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) },
        };
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    private static CollectionEntity NewCollection(EUsernameType owner, string name, DateOnly? fromDate = null)
    {
        return new CollectionEntity
        {
            Name = name,
            Owner = owner,
            Location = string.Empty,
            IsOpen = true,
            ImageId = Constants.DefaultCollectionImageId,
            FromDate = fromDate,
        };
    }

    private static async Task<FilmEntity> AddFilmAsync(IDatabaseService db, EUsernameType purchasedBy, string suffix)
    {
        var film = new FilmEntity
        {
            Brand = "Brand",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            Cost = 1,
            PurchasedBy = purchasedBy,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Developed = true,
            Name = $"Film {suffix}",
        };
        return await db.AddAsync(film);
    }

    [Fact]
    public async Task GetById_NonOwner_ReturnsForbid()
    {
        var other = NewCollection(EUsernameType.Tudor, "Tudor's");
        await _databaseService.AddAsync(other);

        var result = await _controller.GetById(other.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Update_NonOwner_ReturnsForbid()
    {
        var other = NewCollection(EUsernameType.Tudor, "Tudor's");
        await _databaseService.AddAsync(other);

        var dto = new CollectionDto { Name = "Hacked", PhotoIds = [] };

        var result = await _controller.Update(other.Id, dto);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_NonOwner_ReturnsForbid()
    {
        var other = NewCollection(EUsernameType.Tudor, "Tudor's");
        await _databaseService.AddAsync(other);

        var result = await _controller.Delete(other.Id);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task DownloadArchive_NonOwner_ReturnsForbid()
    {
        var other = NewCollection(EUsernameType.Tudor, "Tudor's");
        await _databaseService.AddAsync(other);

        var result = await _controller.DownloadArchive(other.Id, small: false);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetMine_Paged_ReturnsContractAndSlices()
    {
        for (var i = 0; i < 5; i++)
        {
            await _databaseService.AddAsync(NewCollection(EUsernameType.Angel, $"C{i}", DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i))));
        }

        var page1 = await _controller.GetMine(page: 1, pageSize: 2);
        var ok1 = Assert.IsType<OkObjectResult>(page1);
        var dto1 = Assert.IsType<PagedResponseDto<CollectionDto>>(ok1.Value);
        Assert.Equal(5, dto1.TotalCount);
        Assert.Equal(2, dto1.PageSize);
        Assert.Equal(1, dto1.CurrentPage);
        Assert.True(dto1.HasNextPage);
        Assert.False(dto1.HasPreviousPage);
        Assert.Equal(2, dto1.Data.Count());

        var page3 = await _controller.GetMine(page: 3, pageSize: 2);
        var ok3 = Assert.IsType<OkObjectResult>(page3);
        var dto3 = Assert.IsType<PagedResponseDto<CollectionDto>>(ok3.Value);
        Assert.Equal(5, dto3.TotalCount);
        Assert.Equal(3, dto3.CurrentPage);
        Assert.Single(dto3.Data);
        Assert.False(dto3.HasNextPage);
    }

    [Fact]
    public async Task GetMine_PageSizeLessThanOne_IsClampedToOne()
    {
        await _databaseService.AddAsync(NewCollection(EUsernameType.Angel, "Only"));

        var result = await _controller.GetMine(page: 1, pageSize: 0);
        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PagedResponseDto<CollectionDto>>(ok.Value);
        Assert.Equal(1, dto.PageSize);
        Assert.Single(dto.Data);
    }

    [Fact]
    public async Task AppendPhotos_NonOwner_ReturnsForbid()
    {
        var other = NewCollection(EUsernameType.Tudor, "Tudor's");
        await _databaseService.AddAsync(other);

        var result = await _controller.AppendPhotos(other.Id, new IdListDto { Ids = ["x"] });

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task AppendPhotos_OnlyAddsPhotosFromFilmsOwnedByCurrentUser()
    {
        var collection = NewCollection(EUsernameType.Angel, "Mine");
        await _databaseService.AddAsync(collection);

        var filmAngel = await AddFilmAsync(_databaseService, EUsernameType.Angel, "a");
        var filmTudor = await AddFilmAsync(_databaseService, EUsernameType.Tudor, "t");

        var photoMine = new PhotoEntity { FilmId = filmAngel.Id, Index = 1, ImageId = Guid.NewGuid() };
        var photoOther = new PhotoEntity { FilmId = filmTudor.Id, Index = 1, ImageId = Guid.NewGuid() };
        await _databaseService.AddAsync(photoMine);
        await _databaseService.AddAsync(photoOther);

        var body = new IdListDto { Ids = [photoMine.Id, photoOther.Id] };
        var result = await _controller.AppendPhotos(collection.Id, body);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CollectionDto>(ok.Value);
        Assert.Equal(1, dto.PhotoCount);

        var links = await _databaseService.GetEntitiesAsync<CollectionPhotoEntity>(cp => cp.CollectionsId == collection.Id);
        Assert.Single(links);
        var link = links.OrderBy(l => l.CollectionIndex).First();
        Assert.Equal(photoMine.Id, link.PhotosId);
        Assert.Equal(1, link.CollectionIndex);
        Assert.Equal(filmAngel.Id, link.FilmId);
    }

    [Fact]
    public async Task AppendPhotos_SameFilm_SortsByPhotoIndexNotRequestOrder()
    {
        var collection = NewCollection(EUsernameType.Angel, "Mine");
        await _databaseService.AddAsync(collection);

        var film = await AddFilmAsync(_databaseService, EUsernameType.Angel, "one");
        var pHigh = new PhotoEntity { FilmId = film.Id, Index = 10, ImageId = Guid.NewGuid() };
        var pLow = new PhotoEntity { FilmId = film.Id, Index = 3, ImageId = Guid.NewGuid() };
        var pMid = new PhotoEntity { FilmId = film.Id, Index = 4, ImageId = Guid.NewGuid() };
        await _databaseService.AddAsync(pHigh);
        await _databaseService.AddAsync(pLow);
        await _databaseService.AddAsync(pMid);

        var body = new IdListDto { Ids = [pHigh.Id, pLow.Id, pMid.Id] };
        var result = await _controller.AppendPhotos(collection.Id, body);
        Assert.IsType<OkObjectResult>(result);

        var links = (await _databaseService.GetEntitiesAsync<CollectionPhotoEntity>(cp => cp.CollectionsId == collection.Id))
            .OrderBy(l => l.CollectionIndex)
            .ToList();
        Assert.Equal(3, links.Count);
        Assert.Equal(pLow.Id, links[0].PhotosId);
        Assert.Equal(pMid.Id, links[1].PhotosId);
        Assert.Equal(pHigh.Id, links[2].PhotosId);
        Assert.Equal(1, links[0].CollectionIndex);
        Assert.Equal(2, links[1].CollectionIndex);
        Assert.Equal(3, links[2].CollectionIndex);
    }

    [Fact]
    public async Task AppendPhotos_ContinuesCollectionIndexAfterExistingLinks()
    {
        var collection = NewCollection(EUsernameType.Angel, "Mine");
        await _databaseService.AddAsync(collection);

        var film = await AddFilmAsync(_databaseService, EUsernameType.Angel, "one");
        var p1 = new PhotoEntity { FilmId = film.Id, Index = 1, ImageId = Guid.NewGuid() };
        var p2 = new PhotoEntity { FilmId = film.Id, Index = 2, ImageId = Guid.NewGuid() };
        var p3 = new PhotoEntity { FilmId = film.Id, Index = 3, ImageId = Guid.NewGuid() };
        await _databaseService.AddAsync(p1);
        await _databaseService.AddAsync(p2);
        await _databaseService.AddAsync(p3);

        await _databaseService.AddEntitiesAsync(new List<CollectionPhotoEntity>
        {
            new() { CollectionsId = collection.Id, PhotosId = p1.Id, CollectionIndex = 1, FilmId = film.Id },
            new() { CollectionsId = collection.Id, PhotosId = p2.Id, CollectionIndex = 2, FilmId = film.Id },
        });

        var ok = await _controller.AppendPhotos(collection.Id, new IdListDto { Ids = [p3.Id] });
        Assert.IsType<OkObjectResult>(ok);

        var links = (await _databaseService.GetEntitiesAsync<CollectionPhotoEntity>(cp => cp.CollectionsId == collection.Id))
            .OrderBy(l => l.CollectionIndex)
            .ToList();
        Assert.Equal(3, links.Count);
        Assert.Equal(p3.Id, links[2].PhotosId);
        Assert.Equal(3, links[2].CollectionIndex);
    }

    [Fact]
    public async Task AppendPhotos_MultiFilm_OrderFilmsByFirstAppearanceInIds()
    {
        var collection = NewCollection(EUsernameType.Angel, "Mine");
        await _databaseService.AddAsync(collection);

        var filmA = await AddFilmAsync(_databaseService, EUsernameType.Angel, "A");
        var filmB = await AddFilmAsync(_databaseService, EUsernameType.Angel, "B");
        var a1 = new PhotoEntity { FilmId = filmA.Id, Index = 5, ImageId = Guid.NewGuid() };
        var b1 = new PhotoEntity { FilmId = filmB.Id, Index = 1, ImageId = Guid.NewGuid() };
        var b2 = new PhotoEntity { FilmId = filmB.Id, Index = 2, ImageId = Guid.NewGuid() };
        await _databaseService.AddAsync(a1);
        await _databaseService.AddAsync(b1);
        await _databaseService.AddAsync(b2);

        // B first in Ids → both B photos (sorted by index 1,2) then A
        var body = new IdListDto { Ids = [b2.Id, a1.Id, b1.Id] };
        var result = await _controller.AppendPhotos(collection.Id, body);
        Assert.IsType<OkObjectResult>(result);

        var orderedIds = (await _databaseService.GetEntitiesAsync<CollectionPhotoEntity>(cp => cp.CollectionsId == collection.Id))
            .OrderBy(l => l.CollectionIndex)
            .Select(l => l.PhotosId)
            .ToList();
        Assert.Equal(new[] { b1.Id, b2.Id, a1.Id }, orderedIds);
    }

    [Fact]
    public async Task SetPublicPassword_PublicCollection_UpdatesHashWithoutTouchingPhotos()
    {
        var collection = NewCollection(EUsernameType.Angel, "Pub");
        collection.IsPublic = true;
        collection.PublicPasswordHash = PasswordHasher.HashPassword("old");
        await _databaseService.AddAsync(collection);

        var film = await AddFilmAsync(_databaseService, EUsernameType.Angel, "f");
        var photo = new PhotoEntity { FilmId = film.Id, Index = 1, ImageId = Guid.NewGuid() };
        await _databaseService.AddAsync(photo);
        await _databaseService.AddEntitiesAsync(new List<CollectionPhotoEntity>
        {
            new()
            {
                CollectionsId = collection.Id,
                PhotosId = photo.Id,
                FilmId = film.Id,
                CollectionIndex = 1,
            },
        });

        var result = await _controller.SetPublicPassword(
            collection.Id,
            new CollectionSetPublicPasswordDto { PublicPassword = "newsecret" });

        Assert.IsType<OkObjectResult>(result);
        var reloaded = await _databaseService.GetByIdAsync<CollectionEntity>(collection.Id);
        Assert.NotNull(reloaded);
        Assert.NotNull(reloaded.PublicPasswordHash);
        Assert.True(PasswordHasher.VerifyPassword("newsecret", reloaded.PublicPasswordHash!));

        var links = await _databaseService.GetEntitiesAsync<CollectionPhotoEntity>(cp => cp.CollectionsId == collection.Id);
        Assert.Single(links);
        Assert.Equal(photo.Id, links[0].PhotosId);
    }

    [Fact]
    public async Task SetPublicPassword_NotPublic_ReturnsBadRequest()
    {
        var c = NewCollection(EUsernameType.Angel, "Private");
        await _databaseService.AddAsync(c);

        var result = await _controller.SetPublicPassword(c.Id, new CollectionSetPublicPasswordDto { PublicPassword = "x" });

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task SetPublicPassword_NonOwner_ReturnsForbid()
    {
        var c = NewCollection(EUsernameType.Tudor, "Other");
        c.IsPublic = true;
        c.PublicPasswordHash = PasswordHasher.HashPassword("p");
        await _databaseService.AddAsync(c);

        var result = await _controller.SetPublicPassword(c.Id, new CollectionSetPublicPasswordDto { PublicPassword = "new" });

        Assert.IsType<ForbidResult>(result);
    }
}
