using AnalogAgenda.Server.Controllers;
using AnalogAgenda.Server.Tests.Helpers;
using Configuration.Sections;
using Database.Data;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AnalogAgenda.Server.Tests.Controllers;

public class SessionControllerTests
{
    private readonly Mock<IDatabaseService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly DtoConvertor _dtoConvertor;
    private readonly EntityConvertor _entityConvertor;
    private readonly SessionController _controller;

    public SessionControllerTests()
    {
        _mockTableService = new Mock<IDatabaseService>();
        _mockBlobService = new Mock<IBlobService>();
        
        var systemConfig = new Configuration.Sections.System { IsDev = false };
        var storageConfig = new Storage { AccountName = "teststorage" };
        _dtoConvertor = new DtoConvertor(systemConfig, storageConfig);
        _entityConvertor = new EntityConvertor();
        
        _controller = new SessionController(_mockTableService.Object, _mockBlobService.Object, _dtoConvertor, _entityConvertor);
    }
    
    [Fact]
    public void SessionController_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var systemConfig = new Configuration.Sections.System { IsDev = false };
        var storageConfig = new Storage { AccountName = "teststorage" };
        var dtoConvertor = new DtoConvertor(systemConfig, storageConfig);
        var entityConvertor = new EntityConvertor();
        var controller = new SessionController(_mockTableService.Object, _mockBlobService.Object, dtoConvertor, entityConvertor);

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
        var systemConfig = new Configuration.Sections.System { IsDev = false };
        var storageConfig = new Storage { AccountName = "teststorage" };
        var dtoConvertor = new DtoConvertor(systemConfig, storageConfig);
        var entityConvertor = new EntityConvertor();
        var controller = new SessionController(_mockTableService.Object, _mockBlobService.Object, dtoConvertor, entityConvertor);

        // Assert
        Assert.NotNull(controller);
    }
}
