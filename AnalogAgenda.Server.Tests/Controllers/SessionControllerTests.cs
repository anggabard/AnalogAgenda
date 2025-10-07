using Microsoft.AspNetCore.Mvc;
using Moq;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Database.DBObjects.Enums;
using Configuration.Sections;
using AnalogAgenda.Server.Controllers;
using Azure.Data.Tables;
using Azure.Storage.Blobs;

namespace AnalogAgenda.Server.Tests.Controllers;

public class SessionControllerTests
{
    private readonly Mock<ITableService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<TableClient> _mockSessionTable;
    private readonly Mock<TableClient> _mockFilmTable;
    private readonly Mock<TableClient> _mockDevKitTable;
    private readonly Mock<BlobContainerClient> _mockBlobContainer;
    private readonly SessionController _controller;
    private readonly Storage _storage;

    public SessionControllerTests()
    {
        _mockTableService = new Mock<ITableService>();
        _mockBlobService = new Mock<IBlobService>();
        _mockSessionTable = new Mock<TableClient>();
        _mockFilmTable = new Mock<TableClient>();
        _mockDevKitTable = new Mock<TableClient>();
        _mockBlobContainer = new Mock<BlobContainerClient>();
        
        _storage = new Storage { AccountName = "testaccount" };

        _mockTableService.Setup(x => x.GetTable(TableName.Sessions)).Returns(_mockSessionTable.Object);
        _mockTableService.Setup(x => x.GetTable(TableName.Films)).Returns(_mockFilmTable.Object);
        _mockTableService.Setup(x => x.GetTable(TableName.DevKits)).Returns(_mockDevKitTable.Object);
        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.sessions)).Returns(_mockBlobContainer.Object);

        _controller = new SessionController(_storage, _mockTableService.Object, _mockBlobService.Object);
    }

    [Fact]
    public async Task CreateNewSession_ShouldProcessBusinessLogic_WhenSessionIsCreated()
    {
        // Arrange
        var sessionDto = new SessionDto
        {
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            Location = "Test Location",
            Participants = "[\"Angel\", \"Tudor\"]",
            UsedSubstances = "[\"devkit1\", \"devkit2\"]",
            DevelopedFilms = "[\"film1\", \"film2\"]"
        };

        var film1 = new FilmEntity { RowKey = "film1", Developed = false };
        var film2 = new FilmEntity { RowKey = "film2", Developed = false };
        var devKit1 = new DevKitEntity { RowKey = "devkit1", FilmsDeveloped = 0 };
        var devKit2 = new DevKitEntity { RowKey = "devkit2", FilmsDeveloped = 5 };

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<FilmEntity>("film1"))
            .ReturnsAsync(film1);
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<FilmEntity>("film2"))
            .ReturnsAsync(film2);
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<DevKitEntity>("devkit1"))
            .ReturnsAsync(devKit1);
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<DevKitEntity>("devkit2"))
            .ReturnsAsync(devKit2);

        // Act
        var result = await _controller.CreateNewSession(sessionDto);

        // Assert
        Assert.IsType<CreatedResult>(result);
        
        // Verify films were marked as developed
        Assert.True(film1.Developed);
        Assert.True(film2.Developed);
        
        // Verify devkit film counts were incremented
        Assert.Equal(2, devKit1.FilmsDeveloped); // Was 0, added 2 films
        Assert.Equal(7, devKit2.FilmsDeveloped); // Was 5, added 2 films
        
        // Verify database updates were called
        _mockFilmTable.Verify(x => x.UpdateEntityAsync(film1, film1.ETag, TableUpdateMode.Replace), Times.Once);
        _mockFilmTable.Verify(x => x.UpdateEntityAsync(film2, film2.ETag, TableUpdateMode.Replace), Times.Once);
        _mockDevKitTable.Verify(x => x.UpdateEntityAsync(devKit1, devKit1.ETag, TableUpdateMode.Replace), Times.Once);
        _mockDevKitTable.Verify(x => x.UpdateEntityAsync(devKit2, devKit2.ETag, TableUpdateMode.Replace), Times.Once);
    }

    [Fact]
    public async Task UpdateSession_ShouldRevertAndApplyBusinessLogic_WhenFilmsChange()
    {
        // Arrange
        var originalSession = new SessionEntity
        {
            RowKey = "session1",
            UsedSubstances = "[\"devkit1\"]",
            DevelopedFilms = "[\"film1\"]"
        };

        var updatedSessionDto = new SessionDto
        {
            RowKey = "session1",
            SessionDate = DateOnly.FromDateTime(DateTime.Today),
            Location = "Updated Location",
            Participants = "[\"Angel\"]",
            UsedSubstances = "[\"devkit1\"]",
            DevelopedFilms = "[\"film2\"]" // Changed from film1 to film2
        };

        var film1 = new FilmEntity { RowKey = "film1", Developed = true };
        var film2 = new FilmEntity { RowKey = "film2", Developed = false };
        var devKit1 = new DevKitEntity { RowKey = "devkit1", FilmsDeveloped = 3 };

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<SessionEntity>("session1"))
            .ReturnsAsync(originalSession);
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<FilmEntity>("film1"))
            .ReturnsAsync(film1);
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<FilmEntity>("film2"))
            .ReturnsAsync(film2);
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<DevKitEntity>("devkit1"))
            .ReturnsAsync(devKit1);

        // Act
        var result = await _controller.UpdateSession("session1", updatedSessionDto);

        // Assert
        Assert.IsType<OkResult>(result);
        
        // Verify film1 was reverted (marked as not developed)
        Assert.False(film1.Developed);
        
        // Verify film2 was marked as developed
        Assert.True(film2.Developed);
        
        // DevKit count should remain the same (removed 1, added 1)
        Assert.Equal(3, devKit1.FilmsDeveloped);
    }

    [Fact]
    public async Task DeleteSession_ShouldRevertBusinessLogic_WhenSessionIsDeleted()
    {
        // Arrange
        var sessionToDelete = new SessionEntity
        {
            RowKey = "session1",
            UsedSubstances = "[\"devkit1\"]",
            DevelopedFilms = "[\"film1\", \"film2\"]"
        };

        var film1 = new FilmEntity { RowKey = "film1", Developed = true };
        var film2 = new FilmEntity { RowKey = "film2", Developed = true };
        var devKit1 = new DevKitEntity { RowKey = "devkit1", FilmsDeveloped = 5 };

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<SessionEntity>("session1"))
            .ReturnsAsync(sessionToDelete);
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<FilmEntity>("film1"))
            .ReturnsAsync(film1);
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<FilmEntity>("film2"))
            .ReturnsAsync(film2);
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<DevKitEntity>("devkit1"))
            .ReturnsAsync(devKit1);

        // Act
        var result = await _controller.DeleteSession("session1");

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify films were reverted (marked as not developed)
        Assert.False(film1.Developed);
        Assert.False(film2.Developed);
        
        // Verify devkit film count was decremented
        Assert.Equal(3, devKit1.FilmsDeveloped); // Was 5, removed 2 films
    }
}
