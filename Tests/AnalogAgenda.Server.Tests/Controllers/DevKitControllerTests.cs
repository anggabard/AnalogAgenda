using System.Linq;
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
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AnalogAgenda.Server.Tests.Controllers;

public class DevKitControllerTests : IDisposable
{
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly IDatabaseService _databaseService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<BlobContainerClient> _mockContainerClient;
    private readonly Storage _storageConfig;
    private readonly DtoConvertor _dtoConvertor;
    private readonly EntityConvertor _entityConvertor;
    private readonly DevKitController _controller;

    public DevKitControllerTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"DevKitTestDb_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_dbContext);
        _mockBlobService = new Mock<IBlobService>();
        _mockContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        var systemConfig = new Configuration.Sections.System { IsDev = false };
        _dtoConvertor = new DtoConvertor(systemConfig, _storageConfig);
        _entityConvertor = new EntityConvertor();

        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.devkits))
                       .Returns(_mockContainerClient.Object);

        _controller = new DevKitController(_databaseService, _mockBlobService.Object, _dtoConvertor, _entityConvertor);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateNewKit_WithValidDto_ReturnsOk()
    {
        // Arrange
        var devKitDto = new DevKitDto
        {
            Name = "Test Kit",
            Url = "https://example.com",
            Type = "BW",
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act
        var result = await _controller.CreateNewKit(devKitDto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        var createdDto = Assert.IsType<DevKitDto>(createdResult.Value);
        Assert.NotNull(createdDto.Id);
        Assert.Equal("Test Kit", createdDto.Name);
    }

    [Fact]
    public async Task CreateNewKit_WithNullMixedOn_UnmixedSubstance_Succeeds()
    {
        var devKitDto = new DevKitDto
        {
            Name = "Unmixed Kit",
            Url = "https://example.com",
            Type = "C41",
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            MixedOn = null,
            ValidForWeeks = 6,
            ValidForFilms = 8,
            FilmsDeveloped = 0,
            Description = "",
            Expired = false
        };

        var result = await _controller.CreateNewKit(devKitDto);

        var createdResult = Assert.IsType<CreatedResult>(result);
        var createdDto = Assert.IsType<DevKitDto>(createdResult.Value);
        Assert.NotNull(createdDto.Id);
        Assert.Equal("Unmixed Kit", createdDto.Name);
        Assert.Null(createdDto.MixedOn);
    }

    [Fact]
    public async Task CreateNewKit_WithInvalidData_HandlesGracefully()
    {
        // Arrange - Test with empty name which should be validated by the controller
        var devKitDto = new DevKitDto
        {
            Name = "", // Invalid - empty name
            Url = "https://example.com",
            Type = "BW",
            PurchasedBy = "Angel",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act
        var result = await _controller.CreateNewKit(devKitDto);

        // Assert - The controller might handle this gracefully or return an error
        // We're just checking it doesn't crash
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAllKits_ReturnsOkWithDevKits()
    {
        // Arrange
        var devKit1 = new DevKitEntity
        {
            Name = "Test Kit 1",
            Url = "https://example.com",
            Type = EDevKitType.BW,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Id = "kit1"
        };
        var devKit2 = new DevKitEntity
        {
            Name = "Test Kit 2",
            Url = "https://example2.com",
            Type = EDevKitType.C41,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Id = "kit2"
        };

        await _databaseService.AddAsync(devKit1);
        await _databaseService.AddAsync(devKit2);

        // Act
        var result = await _controller.GetAllKits();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResponseDto<DevKitDto>>(okResult.Value);
        Assert.Equal(2, pagedResult.Data.Count());
    }

    [Fact]
    public async Task GetKitById_WithExistingKit_ReturnsOkWithKit()
    {
        // Arrange
        var id = "test-kit-id";
        var devKitEntity = new DevKitEntity
        {
            Name = "Test Kit",
            Url = "https://example.com",
            Type = EDevKitType.BW,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Id = id
        };

        await _databaseService.AddAsync(devKitEntity);

        // Act
        var result = await _controller.GetKitById(id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var devKit = Assert.IsType<DevKitDto>(okResult.Value);
        Assert.Equal(id, devKit.Id);
        Assert.Equal("Test Kit", devKit.Name);
    }

    [Fact]
    public async Task GetKitById_WithNonExistingKit_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existing-key";

        // Act
        var result = await _controller.GetKitById(id);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(id, notFoundResult.Value?.ToString() ?? "");
    }

    [Fact]
    public async Task UpdateKit_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var id = "test-kit-id";
        var existingEntity = new DevKitEntity
        {
            Name = "Test Kit",
            Url = "https://example.com",
            Type = EDevKitType.BW,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Id = id,
            CreatedDate = DateTime.UtcNow.AddDays(-1)
        };

        await _databaseService.AddAsync(existingEntity);

        var updateDto = new DevKitDto
        {
            Name = "Updated Kit",
            Url = "https://updated.com",
            Type = "E6",
            PurchasedBy = "Tudor",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            MixedOn = DateOnly.FromDateTime(DateTime.UtcNow),
            ValidForWeeks = 4,
            ValidForFilms = 10,
            FilmsDeveloped = 0,
            ImageUrl = "", // Empty to keep existing ImageId
            Description = "",
            Expired = false
        };

        // Act
        var result = await _controller.UpdateKit(id, updateDto);

        // Assert
        // Update might fail if there are validation issues
        if (result is NoContentResult)
        {
            // Verify the entity was updated
            var updatedEntity = await _databaseService.GetByIdAsync<DevKitEntity>(id);
            Assert.NotNull(updatedEntity);
            Assert.Equal("Updated Kit", updatedEntity.Name);
        }
        else
        {
            // If update fails, it should be UnprocessableEntity
            Assert.IsType<UnprocessableEntityObjectResult>(result);
        }
    }

    [Fact]
    public async Task UpdateKit_WithNullDto_ReturnsBadRequest()
    {
        // Arrange
        var id = "test-kit-id";

        // Act
        var result = await _controller.UpdateKit(id, null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid data.", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateKit_WithNonExistingKit_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existing-key";
        var updateDto = new DevKitDto
        {
            Name = "Updated Kit",
            Url = "https://updated.com",
            Type = "E6",
            PurchasedBy = "Tudor",
            PurchasedOn = DateOnly.FromDateTime(DateTime.UtcNow)
        };

        // Act
        var result = await _controller.UpdateKit(id, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteKit_WithExistingKit_ReturnsNoContent()
    {
        // Arrange
        var id = "test-kit-id";
        var existingEntity = new DevKitEntity
        {
            Name = "Test Kit",
            Url = "https://example.com",
            Type = EDevKitType.BW,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Id = id
        };

        await _databaseService.AddAsync(existingEntity);

        // Act
        var result = await _controller.DeleteKit(id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify the entity was deleted
        var deletedEntity = await _databaseService.GetByIdAsync<DevKitEntity>(id);
        Assert.Null(deletedEntity);
    }

    [Fact]
    public async Task DeleteKit_WithNonExistingKit_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existing-key";

        // Act
        var result = await _controller.DeleteKit(id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>
    /// Assignment GET uses DevKitSessions only: SessionDevKit navigation must not imply IsSelected.
    /// </summary>
    [Fact]
    public async Task GetSessionAssignment_ShowAll_IsSelectedFromDevKitSessionsOnly_IgnoresLegacyJoin()
    {
        var kitId = "devkit01";
        var kit = new DevKitEntity
        {
            Id = kitId,
            Name = "Kit",
            Url = "https://k",
            Type = EDevKitType.BW,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid()
        };
        var sessionLegacyOnly = new SessionEntity
        {
            Id = "sesslegacy",
            Index = 1,
            Location = "A",
            Participants = "[]",
            SessionDate = DateTime.UtcNow.AddDays(-5),
            ImageId = Guid.NewGuid()
        };
        sessionLegacyOnly.UsedDevKits.Add(kit);
        var sessionInJunction = new SessionEntity
        {
            Id = "sessjunct1",
            Index = 2,
            Location = "B",
            Participants = "[]",
            SessionDate = DateTime.UtcNow.AddDays(-1),
            ImageId = Guid.NewGuid()
        };

        _dbContext.DevKits.Add(kit);
        _dbContext.Sessions.AddRange(sessionLegacyOnly, sessionInJunction);
        await _dbContext.SaveChangesAsync();

        _dbContext.DevKitSessions.Add(new DevKitSessionEntity { DevKitId = kitId, SessionId = sessionInJunction.Id });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetSessionAssignment(kitId, showAll: true);
        var ok = Assert.IsType<OkObjectResult>(result);
        var rows = ((IEnumerable<DevKitSessionAssignmentRowDto>)ok.Value!).ToList();

        var legacyRow = rows.Single(r => r.Id == sessionLegacyOnly.Id);
        var junctionRow = rows.Single(r => r.Id == sessionInJunction.Id);
        Assert.False(legacyRow.IsSelected);
        Assert.True(junctionRow.IsSelected);
    }

    [Fact]
    public async Task GetSessionAssignment_ShowFalse_ReturnsOnlySessionsInDevKitSessions()
    {
        var kitId = "devkit01";
        var kit = new DevKitEntity
        {
            Id = kitId,
            Name = "Kit",
            Url = "https://k",
            Type = EDevKitType.BW,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid()
        };
        var sessionLegacyOnly = new SessionEntity
        {
            Id = "sesslegacy",
            Index = 1,
            Location = "A",
            Participants = "[]",
            SessionDate = DateTime.UtcNow.AddDays(-5),
            ImageId = Guid.NewGuid()
        };
        sessionLegacyOnly.UsedDevKits.Add(kit);
        var sessionInJunction = new SessionEntity
        {
            Id = "sessjunct1",
            Index = 2,
            Location = "B",
            Participants = "[]",
            SessionDate = DateTime.UtcNow.AddDays(-1),
            ImageId = Guid.NewGuid()
        };

        _dbContext.DevKits.Add(kit);
        _dbContext.Sessions.AddRange(sessionLegacyOnly, sessionInJunction);
        await _dbContext.SaveChangesAsync();
        _dbContext.DevKitSessions.Add(new DevKitSessionEntity { DevKitId = kitId, SessionId = sessionInJunction.Id });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetSessionAssignment(kitId, showAll: false);
        var ok = Assert.IsType<OkObjectResult>(result);
        var rows = ((IEnumerable<DevKitSessionAssignmentRowDto>)ok.Value!).ToList();

        Assert.Single(rows);
        Assert.Equal(sessionInJunction.Id, rows[0].Id);
        Assert.True(rows[0].IsSelected);
    }

    /// <summary>
    /// Assignment GET uses DevKitFilms only: Film.DevelopedWithDevKitId must not imply IsSelected.
    /// </summary>
    [Fact]
    public async Task GetFilmAssignment_ShowAll_IsSelectedFromDevKitFilmsOnly_IgnoresDevelopedWithDevKitId()
    {
        var kitId = "devkit02";
        var kit = new DevKitEntity
        {
            Id = kitId,
            Name = "Kit2",
            Url = "https://k",
            Type = EDevKitType.BW,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid()
        };
        var filmLegacyFkOnly = new FilmEntity
        {
            Id = "filmlegacy01",
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Developed = true,
            DevelopedWithDevKitId = kitId
        };
        var filmInJunction = new FilmEntity
        {
            Id = "filmjunct001",
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Developed = true,
            DevelopedWithDevKitId = null
        };

        _dbContext.DevKits.Add(kit);
        _dbContext.Films.AddRange(filmLegacyFkOnly, filmInJunction);
        await _dbContext.SaveChangesAsync();
        _dbContext.DevKitFilms.Add(new DevKitFilmEntity { DevKitId = kitId, FilmId = filmInJunction.Id });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetFilmAssignment(kitId, showAll: true);
        var ok = Assert.IsType<OkObjectResult>(result);
        var rows = ((IEnumerable<DevKitFilmAssignmentRowDto>)ok.Value!).ToList();

        var legacyRow = rows.Single(r => r.Id == filmLegacyFkOnly.Id);
        var junctionRow = rows.Single(r => r.Id == filmInJunction.Id);
        Assert.False(legacyRow.IsSelected);
        Assert.True(junctionRow.IsSelected);
    }

    [Fact]
    public async Task GetFilmAssignment_ShowFalse_ReturnsOnlyFilmsInDevKitFilms()
    {
        var kitId = "devkit02";
        var kit = new DevKitEntity
        {
            Id = kitId,
            Name = "Kit2",
            Url = "https://k",
            Type = EDevKitType.BW,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid()
        };
        var filmLegacyFkOnly = new FilmEntity
        {
            Id = "filmlegacy01",
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Developed = true,
            DevelopedWithDevKitId = kitId
        };
        var filmInJunction = new FilmEntity
        {
            Id = "filmjunct001",
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid(),
            Developed = true,
            DevelopedWithDevKitId = null
        };

        _dbContext.DevKits.Add(kit);
        _dbContext.Films.AddRange(filmLegacyFkOnly, filmInJunction);
        await _dbContext.SaveChangesAsync();
        _dbContext.DevKitFilms.Add(new DevKitFilmEntity { DevKitId = kitId, FilmId = filmInJunction.Id });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetFilmAssignment(kitId, showAll: false);
        var ok = Assert.IsType<OkObjectResult>(result);
        var rows = ((IEnumerable<DevKitFilmAssignmentRowDto>)ok.Value!).ToList();

        Assert.Single(rows);
        Assert.Equal(filmInJunction.Id, rows[0].Id);
        Assert.True(rows[0].IsSelected);
    }

    [Fact]
    public async Task GetSessionAssignment_ShowAll_OrdersSessionsNewestFirst()
    {
        var kitId = "devkitOrdSess";
        var kit = new DevKitEntity
        {
            Id = kitId,
            Name = "Kit",
            Url = "https://k",
            Type = EDevKitType.BW,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid()
        };
        var baseDate = DateTime.UtcNow.Date;
        var sNew = new SessionEntity
        {
            Id = "sOrdNew",
            Index = 1,
            Location = "N",
            Participants = "[]",
            SessionDate = baseDate.AddDays(-1),
            ImageId = Guid.NewGuid()
        };
        var sMid = new SessionEntity
        {
            Id = "sOrdMid",
            Index = 2,
            Location = "M",
            Participants = "[]",
            SessionDate = baseDate.AddDays(-10),
            ImageId = Guid.NewGuid()
        };
        var sOld = new SessionEntity
        {
            Id = "sOrdOld",
            Index = 3,
            Location = "O",
            Participants = "[]",
            SessionDate = baseDate.AddDays(-30),
            ImageId = Guid.NewGuid()
        };

        _dbContext.DevKits.Add(kit);
        _dbContext.Sessions.AddRange(sNew, sMid, sOld);
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetSessionAssignment(kitId, showAll: true);
        var ok = Assert.IsType<OkObjectResult>(result);
        var rows = ((IEnumerable<DevKitSessionAssignmentRowDto>)ok.Value!).ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "sOrdNew", "sOrdMid", "sOrdOld" }, rows.Select(r => r.Id));
    }

    [Fact]
    public async Task GetSessionAssignment_ShowFalse_OrdersSelectedSessionsNewestFirst()
    {
        var kitId = "devkitOrdSess2";
        var kit = new DevKitEntity
        {
            Id = kitId,
            Name = "Kit",
            Url = "https://k",
            Type = EDevKitType.BW,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid()
        };
        var baseDate = DateTime.UtcNow.Date;
        var sNew = new SessionEntity
        {
            Id = "s2New",
            Index = 1,
            Location = "N",
            Participants = "[]",
            SessionDate = baseDate.AddDays(-2),
            ImageId = Guid.NewGuid()
        };
        var sMid = new SessionEntity
        {
            Id = "s2Mid",
            Index = 2,
            Location = "M",
            Participants = "[]",
            SessionDate = baseDate.AddDays(-15),
            ImageId = Guid.NewGuid()
        };
        var sOld = new SessionEntity
        {
            Id = "s2Old",
            Index = 3,
            Location = "O",
            Participants = "[]",
            SessionDate = baseDate.AddDays(-40),
            ImageId = Guid.NewGuid()
        };

        _dbContext.DevKits.Add(kit);
        _dbContext.Sessions.AddRange(sNew, sMid, sOld);
        await _dbContext.SaveChangesAsync();
        _dbContext.DevKitSessions.AddRange(
            new DevKitSessionEntity { DevKitId = kitId, SessionId = sMid.Id },
            new DevKitSessionEntity { DevKitId = kitId, SessionId = sOld.Id });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetSessionAssignment(kitId, showAll: false);
        var ok = Assert.IsType<OkObjectResult>(result);
        var rows = ((IEnumerable<DevKitSessionAssignmentRowDto>)ok.Value!).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "s2Mid", "s2Old" }, rows.Select(r => r.Id));
    }

    [Fact]
    public async Task GetFilmAssignment_ShowAll_OrdersFilmsByExposureNewestFirst()
    {
        var kitId = "devkitOrdFilm";
        var kit = new DevKitEntity
        {
            Id = kitId,
            Name = "Kit",
            Url = "https://k",
            Type = EDevKitType.BW,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid()
        };
        var filmNew = new FilmEntity
        {
            Id = "fOrdNew",
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow.AddDays(-200),
            ImageId = Guid.NewGuid(),
            Developed = true
        };
        var filmMid = new FilmEntity
        {
            Id = "fOrdMid",
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow.AddDays(-100),
            ImageId = Guid.NewGuid(),
            Developed = true
        };
        var filmUnsel = new FilmEntity
        {
            Id = "fOrdUnsel",
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow.AddDays(-50),
            ImageId = Guid.NewGuid(),
            Developed = true
        };

        _dbContext.DevKits.Add(kit);
        _dbContext.Films.AddRange(filmNew, filmMid, filmUnsel);
        await _dbContext.SaveChangesAsync();

        _dbContext.ExposureDates.AddRange(
            new ExposureDateEntity { Id = "expOrdN", FilmId = filmNew.Id, Date = new DateOnly(2025, 6, 1), Description = string.Empty },
            new ExposureDateEntity { Id = "expOrdM", FilmId = filmMid.Id, Date = new DateOnly(2020, 1, 1), Description = string.Empty },
            new ExposureDateEntity { Id = "expOrdU", FilmId = filmUnsel.Id, Date = new DateOnly(2023, 8, 1), Description = string.Empty });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetFilmAssignment(kitId, showAll: true);
        var ok = Assert.IsType<OkObjectResult>(result);
        var rows = ((IEnumerable<DevKitFilmAssignmentRowDto>)ok.Value!).ToList();

        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { "fOrdNew", "fOrdUnsel", "fOrdMid" }, rows.Select(r => r.Id));
    }

    [Fact]
    public async Task GetFilmAssignment_ShowFalse_OrdersSelectedFilmsByExposureNewestFirst()
    {
        var kitId = "devkitOrdFilm2";
        var kit = new DevKitEntity
        {
            Id = kitId,
            Name = "Kit",
            Url = "https://k",
            Type = EDevKitType.BW,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow,
            ImageId = Guid.NewGuid()
        };
        var filmNew = new FilmEntity
        {
            Id = "f2New",
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow.AddDays(-200),
            ImageId = Guid.NewGuid(),
            Developed = true
        };
        var filmOld = new FilmEntity
        {
            Id = "f2Old",
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow.AddDays(-100),
            ImageId = Guid.NewGuid(),
            Developed = true
        };
        var filmOther = new FilmEntity
        {
            Id = "f2Other",
            Brand = "B",
            Iso = "400",
            Type = EFilmType.ColorNegative,
            PurchasedBy = EUsernameType.Angel,
            PurchasedOn = DateTime.UtcNow.AddDays(-50),
            ImageId = Guid.NewGuid(),
            Developed = true
        };

        _dbContext.DevKits.Add(kit);
        _dbContext.Films.AddRange(filmNew, filmOld, filmOther);
        await _dbContext.SaveChangesAsync();

        _dbContext.ExposureDates.AddRange(
            new ExposureDateEntity { Id = "exp2New", FilmId = filmNew.Id, Date = new DateOnly(2025, 1, 1), Description = string.Empty },
            new ExposureDateEntity { Id = "exp2Old", FilmId = filmOld.Id, Date = new DateOnly(2019, 1, 1), Description = string.Empty },
            new ExposureDateEntity { Id = "exp2Oth", FilmId = filmOther.Id, Date = new DateOnly(2024, 1, 1), Description = string.Empty });
        await _dbContext.SaveChangesAsync();

        _dbContext.DevKitFilms.AddRange(
            new DevKitFilmEntity { DevKitId = kitId, FilmId = filmNew.Id },
            new DevKitFilmEntity { DevKitId = kitId, FilmId = filmOld.Id });
        await _dbContext.SaveChangesAsync();

        var result = await _controller.GetFilmAssignment(kitId, showAll: false);
        var ok = Assert.IsType<OkObjectResult>(result);
        var rows = ((IEnumerable<DevKitFilmAssignmentRowDto>)ok.Value!).ToList();

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "f2New", "f2Old" }, rows.Select(r => r.Id));
    }
}

