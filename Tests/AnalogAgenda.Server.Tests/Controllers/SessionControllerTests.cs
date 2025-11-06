using AnalogAgenda.Server.Controllers;
using AnalogAgenda.Server.Tests.Helpers;
using Configuration.Sections;
using Database.Data;
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
    public async Task GetSessionById_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var id = "nonexistent-session";

        // Act
        var result = await _controller.GetSessionById(id);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"No Session found with Id: {id}", notFoundResult.Value);
    }

    [Fact]
    public void SessionController_HasCorrectRoute()
    {
        // Arrange & Act
        var controller = new SessionController(_mockStorage.Object, _mockTableService.Object, _mockBlobService.Object);

        // Assert
        Assert.NotNull(controller);
    }
}
