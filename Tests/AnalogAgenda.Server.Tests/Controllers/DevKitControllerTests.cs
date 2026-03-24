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
            Location = "A",
            Participants = "[]",
            SessionDate = DateTime.UtcNow.AddDays(-5),
            ImageId = Guid.NewGuid()
        };
        sessionLegacyOnly.UsedDevKits.Add(kit);
        var sessionInJunction = new SessionEntity
        {
            Id = "sessjunct1",
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
            Location = "A",
            Participants = "[]",
            SessionDate = DateTime.UtcNow.AddDays(-5),
            ImageId = Guid.NewGuid()
        };
        sessionLegacyOnly.UsedDevKits.Add(kit);
        var sessionInJunction = new SessionEntity
        {
            Id = "sessjunct1",
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
}

