using AnalogAgenda.Server.Controllers;
using Configuration.Sections;
using Database.DTOs;
using Database.Entities;
using Database.Services;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AnalogAgenda.Server.Tests.Controllers;

public class IdeaControllerTests
{
    private readonly Mock<IDatabaseService> _mockDatabaseService;
    private readonly DtoConvertor _dtoConvertor;
    private readonly EntityConvertor _entityConvertor;
    private readonly IdeaController _controller;

    public IdeaControllerTests()
    {
        _mockDatabaseService = new Mock<IDatabaseService>();
        var systemConfig = new Configuration.Sections.System { IsDev = false };
        var storageConfig = new Storage { AccountName = "teststorage" };
        _dtoConvertor = new DtoConvertor(systemConfig, storageConfig);
        _entityConvertor = new EntityConvertor();
        _controller = new IdeaController(_mockDatabaseService.Object, _dtoConvertor, _entityConvertor);
    }

    [Fact]
    public void IdeaController_Constructor_InitializesCorrectly()
    {
        var systemConfig = new Configuration.Sections.System { IsDev = false };
        var storageConfig = new Storage { AccountName = "teststorage" };
        var dtoConvertor = new DtoConvertor(systemConfig, storageConfig);
        var entityConvertor = new EntityConvertor();
        var controller = new IdeaController(_mockDatabaseService.Object, dtoConvertor, entityConvertor);

        Assert.NotNull(controller);
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoIdeas()
    {
        _mockDatabaseService.Setup(x => x.GetAllAsync<IdeaEntity>()).ReturnsAsync(new List<IdeaEntity>());

        var result = await _controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<IdeaDto>>(okResult.Value);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetAll_ReturnsIdeas_WhenIdeasExist()
    {
        var ideas = new List<IdeaEntity>
        {
            new() { Id = "abc", Title = "Idea 1", Description = "Desc 1", CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow },
            new() { Id = "def", Title = "Idea 2", Description = "Desc 2", CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow }
        };
        _mockDatabaseService.Setup(x => x.GetAllAsync<IdeaEntity>()).ReturnsAsync(ideas);

        var result = await _controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var list = Assert.IsAssignableFrom<IEnumerable<IdeaDto>>(okResult.Value).ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal("abc", list[0].Id);
        Assert.Equal("Idea 1", list[0].Title);
    }

    [Fact]
    public async Task GetById_WithInvalidId_ReturnsNotFound()
    {
        var id = "nonexistent";
        _mockDatabaseService.Setup(x => x.GetByIdAsync<IdeaEntity>(id)).ReturnsAsync((IdeaEntity?)null);

        var result = await _controller.GetById(id);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"No Idea found with Id: {id}", notFoundResult.Value);
    }

    [Fact]
    public async Task GetById_WithValidId_ReturnsIdea()
    {
        var id = "abc";
        var entity = new IdeaEntity { Id = id, Title = "My Idea", Description = "My desc", CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
        _mockDatabaseService.Setup(x => x.GetByIdAsync<IdeaEntity>(id)).ReturnsAsync(entity);

        var result = await _controller.GetById(id);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<IdeaDto>(okResult.Value);
        Assert.Equal(id, dto.Id);
        Assert.Equal("My Idea", dto.Title);
        Assert.Equal("My desc", dto.Description);
    }

    [Fact]
    public async Task Create_WithValidDto_ReturnsCreated()
    {
        var dto = new IdeaDto { Id = "", Title = "New Idea", Description = "New desc" };
        _mockDatabaseService
            .Setup(x => x.AddAsync(It.IsAny<IdeaEntity>()))
            .Callback<IdeaEntity>(e => e.Id = "xyz")
            .ReturnsAsync((IdeaEntity e) => e);

        var result = await _controller.Create(dto);

        var createdResult = Assert.IsType<CreatedResult>(result);
        var createdDto = Assert.IsType<IdeaDto>(createdResult.Value);
        Assert.Equal("xyz", createdDto.Id);
        Assert.Equal("New Idea", createdDto.Title);
        _mockDatabaseService.Verify(x => x.AddAsync(It.IsAny<IdeaEntity>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithNullDto_ReturnsBadRequest()
    {
        var result = await _controller.Create(null!);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid data.", badRequestResult.Value);
    }

    [Fact]
    public async Task Update_WithInvalidId_ReturnsNotFound()
    {
        var id = "nonexistent";
        var dto = new IdeaDto { Id = id, Title = "Updated", Description = "Updated desc" };
        _mockDatabaseService.Setup(x => x.GetByIdAsync<IdeaEntity>(id)).ReturnsAsync((IdeaEntity?)null);

        var result = await _controller.Update(id, dto);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Update_WithValidId_UpdatesAndReturnsNoContent()
    {
        var id = "abc";
        var existingEntity = new IdeaEntity { Id = id, Title = "Old", Description = "Old desc", CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
        var dto = new IdeaDto { Id = id, Title = "Updated", Description = "Updated desc" };
        _mockDatabaseService.Setup(x => x.GetByIdAsync<IdeaEntity>(id)).ReturnsAsync(existingEntity);
        _mockDatabaseService.Setup(x => x.UpdateAsync(It.IsAny<IdeaEntity>())).Returns(Task.CompletedTask);

        var result = await _controller.Update(id, dto);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("Updated", existingEntity.Title);
        Assert.Equal("Updated desc", existingEntity.Description);
        _mockDatabaseService.Verify(x => x.UpdateAsync(existingEntity), Times.Once);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ReturnsNotFound()
    {
        var id = "nonexistent";
        _mockDatabaseService.Setup(x => x.GetByIdAsync<IdeaEntity>(id)).ReturnsAsync((IdeaEntity?)null);

        var result = await _controller.Delete(id);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_WithValidId_DeletesAndReturnsNoContent()
    {
        var id = "abc";
        var entity = new IdeaEntity { Id = id, Title = "To Delete", Description = "", CreatedDate = DateTime.UtcNow, UpdatedDate = DateTime.UtcNow };
        _mockDatabaseService.Setup(x => x.GetByIdAsync<IdeaEntity>(id)).ReturnsAsync(entity);
        _mockDatabaseService.Setup(x => x.DeleteAsync(It.IsAny<IdeaEntity>())).Returns(Task.CompletedTask);

        var result = await _controller.Delete(id);

        Assert.IsType<NoContentResult>(result);
        _mockDatabaseService.Verify(x => x.DeleteAsync(entity), Times.Once);
    }
}
