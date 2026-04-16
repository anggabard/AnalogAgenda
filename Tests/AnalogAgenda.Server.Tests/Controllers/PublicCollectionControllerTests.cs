using AnalogAgenda.Server.Controllers;
using AnalogAgenda.Server.Tests.Helpers;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects;
using Database.DBObjects.Enums;
using Database.Data;
using Database.DTOs;
using Database.Entities;
using Database.Helpers;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System.Net;
using System.Text;

namespace AnalogAgenda.Server.Tests.Controllers;

public class PublicCollectionControllerTests : IDisposable
{
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly IDatabaseService _databaseService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<BlobContainerClient> _mockPhotosContainerClient;
    private readonly DtoConvertor _dtoConvertor;
    private readonly IDataProtectionProvider _dataProtectionProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly string _keyDirectory;

    public PublicCollectionControllerTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"PublicCollectionTests_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_dbContext);
        _mockBlobService = new Mock<IBlobService>();
        _mockPhotosContainerClient = new Mock<BlobContainerClient>();
        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.photos))
            .Returns(_mockPhotosContainerClient.Object);

        var storageConfig = new Storage { AccountName = "teststorage" };
        var systemConfig = new Configuration.Sections.System { IsDev = false };
        _dtoConvertor = new DtoConvertor(systemConfig, storageConfig);

        _keyDirectory = Path.Combine(Path.GetTempPath(), $"aad-dp-{Guid.NewGuid()}");
        Directory.CreateDirectory(_keyDirectory);
        _dataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(_keyDirectory));

        _memoryCache = new MemoryCache(new MemoryCacheOptions());
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
        _memoryCache.Dispose();
        try
        {
            if (Directory.Exists(_keyDirectory))
                Directory.Delete(_keyDirectory, recursive: true);
        }
        catch
        {
            // best-effort cleanup on Windows file locks
        }
    }

    private PublicCollectionController CreateController(HttpContext? httpContext = null)
    {
        var c = new PublicCollectionController(
            _databaseService,
            _mockBlobService.Object,
            _dtoConvertor,
            _dataProtectionProvider,
            _memoryCache);
        c.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext ?? new DefaultHttpContext(),
        };
        return c;
    }

    private static CollectionEntity NewPublicCollection(string passwordPlain)
    {
        return new CollectionEntity
        {
            Name = "Public Coll",
            Owner = EUsernameType.Angel,
            Location = string.Empty,
            IsOpen = true,
            ImageId = Constants.DefaultCollectionImageId,
            IsPublic = true,
            PublicPasswordHash = PasswordHasher.HashPassword(passwordPlain),
        };
    }

    private static void AppendRequestCookie(HttpContext http, string name, string value)
    {
        http.Request.Headers.Append("Cookie", $"{name}={WebUtility.UrlEncode(value)}");
    }

    private string ProtectCookiePayload(string payload)
    {
        var protector = _dataProtectionProvider.CreateProtector("AnalogAgenda.PublicCollection.v1");
        var token = protector.Protect(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(token);
    }

    private static string AccessCookieName(string collectionId) =>
        "AACollPub_" + WebUtility.UrlEncode(collectionId);

    [Fact]
    public async Task GetPage_NoCookie_ReturnsRequiresPassword()
    {
        var entity = NewPublicCollection("secret");
        await _databaseService.AddAsync(entity);

        var controller = CreateController();
        var result = await controller.GetPage(entity.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PublicCollectionPageDto>(ok.Value);
        Assert.True(dto.RequiresPassword);
        Assert.Equal(entity.Id, dto.Id);
    }

    [Fact]
    public async Task GetPage_LegacyTwoPartCookie_ReturnsRequiresPassword()
    {
        var entity = NewPublicCollection("secret");
        await _databaseService.AddAsync(entity);

        var exp = DateTime.UtcNow.AddDays(1);
        var legacyPayload = $"{entity.Id}|{exp:O}";
        var raw = ProtectCookiePayload(legacyPayload);

        var http = new DefaultHttpContext();
        AppendRequestCookie(http, AccessCookieName(entity.Id), raw);

        var controller = CreateController(http);
        var result = await controller.GetPage(entity.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PublicCollectionPageDto>(ok.Value);
        Assert.True(dto.RequiresPassword);
    }

    [Fact]
    public async Task GetPage_ExpiredThreePartCookie_ReturnsRequiresPassword()
    {
        var entity = NewPublicCollection("secret");
        await _databaseService.AddAsync(entity);

        var exp = DateTime.UtcNow.AddDays(-1);
        var payload = $"{entity.Id}|{exp:O}|{entity.PublicPasswordHash}";
        var raw = ProtectCookiePayload(payload);

        var http = new DefaultHttpContext();
        AppendRequestCookie(http, AccessCookieName(entity.Id), raw);

        var controller = CreateController(http);
        var result = await controller.GetPage(entity.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PublicCollectionPageDto>(ok.Value);
        Assert.True(dto.RequiresPassword);
    }

    [Fact]
    public async Task GetPage_WrongPasswordHashInCookie_ReturnsRequiresPassword()
    {
        var entity = NewPublicCollection("secret");
        await _databaseService.AddAsync(entity);

        var exp = DateTime.UtcNow.AddDays(1);
        var payload = $"{entity.Id}|{exp:O}|{PasswordHasher.HashPassword("other")}";
        var raw = ProtectCookiePayload(payload);

        var http = new DefaultHttpContext();
        AppendRequestCookie(http, AccessCookieName(entity.Id), raw);

        var controller = CreateController(http);
        var result = await controller.GetPage(entity.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PublicCollectionPageDto>(ok.Value);
        Assert.True(dto.RequiresPassword);
    }

    [Fact]
    public async Task GetPage_ValidCookie_ReturnsFullPageWithComments()
    {
        var entity = NewPublicCollection("secret");
        await _databaseService.AddAsync(entity);

        var comment = new CollectionPublicCommentEntity
        {
            CollectionId = entity.Id,
            AuthorName = "A",
            Body = "Hello",
        };
        comment.Id = comment.GetId();
        await _databaseService.AddAsync(comment);

        var exp = DateTime.UtcNow.AddDays(1);
        var payload = $"{entity.Id}|{exp:O}|{entity.PublicPasswordHash}";
        var raw = ProtectCookiePayload(payload);

        var http = new DefaultHttpContext();
        AppendRequestCookie(http, AccessCookieName(entity.Id), raw);

        var controller = CreateController(http);
        var result = await controller.GetPage(entity.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<PublicCollectionPageDto>(ok.Value);
        Assert.False(dto.RequiresPassword);
        Assert.Equal(entity.Id, dto.Id);
        Assert.Single(dto.Comments);
        Assert.Equal("Hello", dto.Comments[0].Body);
    }

    [Fact]
    public async Task Verify_CorrectPassword_AppendsAccessCookie()
    {
        var entity = NewPublicCollection("secret");
        await _databaseService.AddAsync(entity);

        var http = new DefaultHttpContext();
        var controller = CreateController(http);

        var actionResult = await controller.Verify(entity.Id, new CollectionPublicVerifyDto { Password = "secret" });

        Assert.IsType<OkObjectResult>(actionResult);
        var setCookie = http.Response.Headers.SetCookie.ToString();
        Assert.Contains(AccessCookieName(entity.Id), setCookie, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostComment_NoCookie_ReturnsUnauthorized()
    {
        var entity = NewPublicCollection("secret");
        await _databaseService.AddAsync(entity);

        var controller = CreateController();
        var result = await controller.PostComment(entity.Id, new CollectionPublicCommentPostDto
        {
            AuthorName = "X",
            Body = "Y",
        });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task PostComment_ValidCookie_CreatesComment()
    {
        var entity = NewPublicCollection("secret");
        await _databaseService.AddAsync(entity);

        var exp = DateTime.UtcNow.AddDays(1);
        var payload = $"{entity.Id}|{exp:O}|{entity.PublicPasswordHash}";
        var raw = ProtectCookiePayload(payload);

        var http = new DefaultHttpContext();
        AppendRequestCookie(http, AccessCookieName(entity.Id), raw);

        var controller = CreateController(http);
        var result = await controller.PostComment(entity.Id, new CollectionPublicCommentPostDto
        {
            AuthorName = "Visitor",
            Body = "Nice",
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<CollectionPublicCommentDto>(ok.Value);
        Assert.Equal("Nice", dto.Body);
        Assert.Equal("Visitor", dto.AuthorName);
    }
}
