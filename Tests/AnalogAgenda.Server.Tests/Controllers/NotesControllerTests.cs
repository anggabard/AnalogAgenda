using AnalogAgenda.Server.Controllers;
using AnalogAgenda.Server.Tests.Helpers;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.Data;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AnalogAgenda.Server.Tests.Controllers;

public class NotesControllerTests : IDisposable
{
    private readonly Mock<IDatabaseService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<BlobContainerClient> _mockContainerClient;
    private readonly Storage _storageConfig;
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly NotesController _controller;

    public NotesControllerTests()
    {
        _mockTableService = new Mock<IDatabaseService>();
        _mockBlobService = new Mock<IBlobService>();
        _mockContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };
        _dbContext = InMemoryDbContextFactory.Create($"NotesTestDb_{Guid.NewGuid()}");
        
        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.notes))
                       .Returns(_mockContainerClient.Object);

        _controller = new NotesController(_storageConfig, _mockTableService.Object, _mockBlobService.Object, _dbContext);
    }
    
    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateNewNote_WithValidDto_ReturnsOkWithRowKey()
    {
        // Arrange
        var noteDto = new NoteDto
        {
            Name = "Test Note",
            SideNote = "Test Description",
            ImageBase64 = null!,
            Entries = []
        };

        _mockTableService.Setup(x => x.AddAsync(It.IsAny<NoteEntity>()))
                            .ReturnsAsync(It.IsAny<NoteEntity>());

        // Act
        var result = await _controller.CreateNewNote(noteDto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.IsType<NoteDto>(createdResult.Value);
        _mockTableService.Verify(x => x.AddAsync(It.IsAny<NoteEntity>()), Times.Once);
    }

    [Fact]
    public async Task CreateNewNote_WithException_ReturnsUnprocessableEntity()
    {
        // Arrange
        var noteDto = new NoteDto
        {
            Name = "Test Note",
            SideNote = "Test Description",
            ImageBase64 = null!,
            Entries = new List<NoteEntryDto>()
        };

        _mockTableService.Setup(x => x.AddAsync(It.IsAny<NoteEntity>()))
                            .ThrowsAsync(new Exception("Database error"));

        _mockTableService.Setup(x => x.ExistsAsync<NoteEntity>(It.IsAny<string>()))
                        .ReturnsAsync(false);

        // Act
        var result = await _controller.CreateNewNote(noteDto);

        // Assert
        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.Equal("Database error", unprocessableResult.Value);
    }

    [Fact]
    public async Task GetAllNotes_WithoutEntries_ReturnsOkWithNotes()
    {
        // Arrange - Add data directly to dbContext since controller queries it
        var noteEntity = new NoteEntity
        {
            Name = "Test Note",
            SideNote = "Test Description",
            ImageId = Guid.NewGuid(),
            Id = "test-row-key"
        };
        _dbContext.Notes.Add(noteEntity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllNotes(withEntries: false);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResponseDto<NoteDto>>(okResult.Value);
        Assert.Single(pagedResult.Data);
    }

    [Fact]
    public async Task GetAllNotes_WithEntries_ReturnsOkWithNotesAndEntries()
    {
        // Arrange - Add data directly to dbContext
        var noteEntity = new NoteEntity
        {
            Name = "Test Note",
            SideNote = "Test Description",
            ImageId = Guid.NewGuid(),
            Id = "test-row-key"
        };

        var noteEntryEntity = new NoteEntryEntity
        {
            Details = "Test Entry",
            Time = 1.0,
            Process = "Test Process",
            Film = "Test Film",
            NoteId = "test-row-key",
            Id = "entry-key"
        };

        _dbContext.Notes.Add(noteEntity);
        _dbContext.NoteEntries.Add(noteEntryEntity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetAllNotes(withEntries: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResponseDto<NoteDto>>(okResult.Value);
        Assert.Single(pagedResult.Data);
    }

    [Fact]
    public async Task GetNoteById_WithExistingNote_ReturnsOkWithNote()
    {
        // Arrange - Add data directly to dbContext
        var id = "test-row-key";
        var noteEntity = new NoteEntity
        {
            Name = "Test Note",
            SideNote = "Test Description",
            ImageId = Guid.NewGuid(),
            Id = id
        };

        var noteEntryEntity = new NoteEntryEntity
        {
            Details = "Test Entry",
            Time = 1.0,
            Process = "Test Process",
            Film = "Test Film",
            NoteId = id,
            Id = "entry-key"
        };

        _dbContext.Notes.Add(noteEntity);
        _dbContext.NoteEntries.Add(noteEntryEntity);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetNoteById(id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var note = Assert.IsType<NoteDto>(okResult.Value);
        Assert.NotNull(note.Id);  // Id is auto-generated
    }

    [Fact]
    public async Task GetNoteById_WithNonExistingNote_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existing-key";

        _mockTableService.Setup(x => x.GetByIdAsync<NoteEntity>(id))
                        .ReturnsAsync((NoteEntity?)null);

        // Act
        var result = await _controller.GetNoteById(id);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(id, notFoundResult.Value?.ToString() ?? "");
    }

    [Fact]
    public async Task UpdateNote_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var id = "test-row-key";
        var existingEntity = new NoteEntity
        {
            Name = "Old Title",
            SideNote = "Old Description",
            ImageId = Guid.NewGuid(),
            Id = id,
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            // ETag removed - EF Core handles concurrency
        };

        var updateDto = new NoteDto
        {
            Name = "New Title",
            SideNote = "New Description",
            ImageBase64 = null!,
            Entries = new List<NoteEntryDto>()
        };

        var existingEntryEntities = new List<NoteEntryEntity>();

        _mockTableService.Setup(x => x.GetByIdAsync<NoteEntity>(id))
                        .ReturnsAsync(existingEntity);

        _mockTableService.Setup(x => x.GetAllAsync<NoteEntryEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryEntity, bool>>>()))
                        .ReturnsAsync(existingEntryEntities);

        _mockTableService.Setup(x => x.UpdateAsync(It.IsAny<NoteEntity>()))
                            .Returns(Task.CompletedTask);

        _mockTableService.Setup(x => x.DeleteRangeAsync(It.IsAny<IEnumerable<NoteEntryEntity>>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateNote(id, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockTableService.Verify(x => x.UpdateAsync(It.IsAny<NoteEntity>()), Times.Once);
    }

    [Fact]
    public async Task UpdateNote_WithNullDto_ReturnsBadRequest()
    {
        // Arrange
        var id = "test-row-key";

        // Act
        var result = await _controller.UpdateNote(id, null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid data.", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateNote_WithNonExistingNote_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existing-key";
        var updateDto = new NoteDto
        {
            Name = "New Title",
            SideNote = "New Description",
            Entries = new List<NoteEntryDto>()
        };

        _mockTableService.Setup(x => x.GetByIdAsync<NoteEntity>(id))
                        .ReturnsAsync((NoteEntity?)null);

        // Act
        var result = await _controller.UpdateNote(id, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteNote_WithExistingNote_ReturnsNoContent()
    {
        // Arrange
        var id = "test-row-key";
        var existingEntity = new NoteEntity
        {
            Name = "Test Note",
            SideNote = "Test Description",
            ImageId = Guid.NewGuid(),
            Id = id
        };

        _mockTableService.Setup(x => x.GetByIdAsync<NoteEntity>(id))
                        .ReturnsAsync(existingEntity);

        _mockTableService.Setup(x => x.DeleteRangeAsync<NoteEntryEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryEntity, bool>>>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteNote(id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        // Note: DeleteEntityWithImageAsync from base controller handles the note deletion internally
        _mockTableService.Verify(x => x.DeleteRangeAsync<NoteEntryEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryEntity, bool>>>()), Times.Once);
        _mockTableService.Verify(x => x.GetByIdAsync<NoteEntity>(id), Times.Once);
    }

    [Fact]
    public async Task DeleteNote_WithNonExistingNote_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existing-key";

        _mockTableService.Setup(x => x.GetByIdAsync<NoteEntity>(id))
                        .ReturnsAsync((NoteEntity?)null);

        // Act
        var result = await _controller.DeleteNote(id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
