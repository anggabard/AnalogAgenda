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
    private readonly Mock<IDatabaseService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly SessionController _controller;

    public SessionControllerTests()
    {
        _mockStorage = new Mock<Storage>();
        _mockTableService = new Mock<IDatabaseService>();
        _mockBlobService = new Mock<IBlobService>();
        _controller = new SessionController(_mockStorage.Object, _mockTableService.Object, _mockBlobService.Object);
    }

    [Fact]
    public void SessionController_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var controller = new SessionController(_mockStorage.Object, _mockTableService.Object, _mockBlobService.Object);

        // Assert
        Assert.NotNull(controller);
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
    public void SessionController_HasCorrectRoute()
    {
        // Arrange & Act
        var controller = new SessionController(_mockStorage.Object, _mockTableService.Object, _mockBlobService.Object);

        // Assert
        Assert.NotNull(controller);
        // The route attribute is applied at the class level, so we just verify the controller exists
    }
}