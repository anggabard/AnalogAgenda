using AnalogAgenda.Server.Controllers;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AnalogAgenda.Server.Tests.Controllers;

public class SessionControllerTests
{
    private readonly Mock<Storage> _mockStorage;
    private readonly Mock<ITableService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly SessionController _controller;

    public SessionControllerTests()
    {
        _mockStorage = new Mock<Storage>();
        _mockTableService = new Mock<ITableService>();
        _mockBlobService = new Mock<IBlobService>();
        _controller = new SessionController(_mockStorage.Object, _mockTableService.Object, _mockBlobService.Object);
    }

    [Fact]
    public async Task GetSessionByRowKey_WithValidRowKey_ReturnsSession()
    {
        // Arrange
        var rowKey = "test-session-123";
        var sessionEntity = new SessionEntity
        {
            RowKey = rowKey,
            Name = "Test Session",
            Description = "Test Description"
        };

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<SessionEntity>(rowKey))
            .ReturnsAsync(sessionEntity);

        // Act
        var result = await _controller.GetSessionByRowKey(rowKey);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var sessionDto = Assert.IsType<SessionDto>(okResult.Value);
        Assert.Equal(rowKey, sessionDto.RowKey);
        Assert.Equal("Test Session", sessionDto.Name);
    }

    [Fact]
    public async Task GetSessionByRowKey_WithInvalidRowKey_ReturnsNotFound()
    {
        // Arrange
        var rowKey = "nonexistent-session";

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<SessionEntity>(rowKey))
            .ReturnsAsync((SessionEntity?)null);

        // Act
        var result = await _controller.GetSessionByRowKey(rowKey);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"No Session found with RowKey: {rowKey}", notFoundResult.Value);
    }

    [Fact]
    public async Task CreateNewSession_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var sessionDto = new SessionDto
        {
            Name = "New Session",
            Description = "New Description",
            ImageBase64 = "base64data"
        };

        // Mock the base controller methods
        _mockTableService.Setup(x => x.GetTable(It.IsAny<Database.DBObjects.Enums.TableName>()))
            .Returns(new Mock<TableClient>().Object);
        _mockBlobService.Setup(x => x.GetBlobContainer(It.IsAny<Database.DBObjects.Enums.ContainerName>()))
            .Returns(new Mock<BlobContainerClient>().Object);

        // Act
        var result = await _controller.CreateNewSession(sessionDto);

        // Assert
        Assert.IsType<CreatedResult>(result);
    }

    [Fact]
    public async Task UpdateSession_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var rowKey = "test-session-123";
        var updateDto = new SessionDto
        {
            Name = "Updated Session",
            Description = "Updated Description"
        };

        var originalSession = new SessionEntity
        {
            RowKey = rowKey,
            Name = "Original Session"
        };

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<SessionEntity>(rowKey))
            .ReturnsAsync(originalSession);

        // Mock the base controller methods
        _mockTableService.Setup(x => x.GetTable(It.IsAny<Database.DBObjects.Enums.TableName>()))
            .Returns(new Mock<TableClient>().Object);
        _mockBlobService.Setup(x => x.GetBlobContainer(It.IsAny<Database.DBObjects.Enums.ContainerName>()))
            .Returns(new Mock<BlobContainerClient>().Object);

        // Act
        var result = await _controller.UpdateSession(rowKey, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteSession_WithValidRowKey_ReturnsNoContent()
    {
        // Arrange
        var rowKey = "test-session-123";
        var sessionToDelete = new SessionEntity
        {
            RowKey = rowKey,
            Name = "Session to Delete"
        };

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<SessionEntity>(rowKey))
            .ReturnsAsync(sessionToDelete);

        // Mock the base controller methods
        _mockTableService.Setup(x => x.GetTable(It.IsAny<Database.DBObjects.Enums.TableName>()))
            .Returns(new Mock<TableClient>().Object);
        _mockBlobService.Setup(x => x.GetBlobContainer(It.IsAny<Database.DBObjects.Enums.ContainerName>()))
            .Returns(new Mock<BlobContainerClient>().Object);

        // Act
        var result = await _controller.DeleteSession(rowKey);

        // Assert
        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetAllSessions_ReturnsOkResult()
    {
        // Arrange
        var page = 1;
        var pageSize = 5;

        // Mock the base controller methods
        _mockTableService.Setup(x => x.GetTable(It.IsAny<Database.DBObjects.Enums.TableName>()))
            .Returns(new Mock<TableClient>().Object);

        // Act
        var result = await _controller.GetAllSessions(page, pageSize);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }
}
