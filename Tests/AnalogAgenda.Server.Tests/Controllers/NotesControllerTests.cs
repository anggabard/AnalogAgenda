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

        _controller = new NotesController(_storageConfig, _mockTableService.Object, _mockBlobService.Object);
    }
    
    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateNewNote_WithValidDto_ReturnsOkWithId()
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
                            .ReturnsAsync((NoteEntity entity) => entity);

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

        // Act
        var result = await _controller.CreateNewNote(noteDto);

        // Assert
        var unprocessableResult = Assert.IsType<UnprocessableEntityObjectResult>(result);
        Assert.Contains("Database error", unprocessableResult.Value?.ToString() ?? "");
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
            Id = "test-note-id"
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
            Id = "test-note-id"
        };

        var noteEntryEntity = new NoteEntryEntity
        {
            Details = "Test Entry",
            Time = 1.0,
            Step = "Test Process",
            NoteId = "test-note-id",
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
        var note = pagedResult.Data.First();
    }

    [Fact]
    public async Task GetNoteById_WithExistingNote_ReturnsOkWithNote()
    {
        // Arrange - Add data directly to dbContext
        var id = "test-note-id";
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
            Step = "Test Process",
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
        Assert.NotNull(note.Id);
        Assert.Single(note.Entries);
    }

    [Fact]
    public async Task GetNoteById_WithNonExistingNote_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existing-key";

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
        var id = "test-note-id";
        var existingEntity = new NoteEntity
        {
            Name = "Old Title",
            SideNote = "Old Description",
            ImageId = Guid.NewGuid(),
            Id = id,
            CreatedDate = DateTime.UtcNow.AddDays(-1),
        };

        var updateDto = new NoteDto
        {
            Name = "New Title",
            SideNote = "New Description",
            ImageBase64 = null!,
            Entries = new List<NoteEntryDto>()
        };

        _dbContext.Notes.Add(existingEntity);
        await _dbContext.SaveChangesAsync();
        
        // Detach the entity to avoid tracking conflicts
        _dbContext.Entry(existingEntity).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        _mockTableService.Setup(x => x.GetAllAsync<NoteEntryEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryEntity, bool>>>()))
                        .ReturnsAsync(new List<NoteEntryEntity>());

        _mockTableService.Setup(x => x.UpdateAsync(It.IsAny<NoteEntity>())).Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateNote(id, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
    }

    [Fact]
    public async Task UpdateNote_WithNullDto_ReturnsBadRequest()
    {
        // Arrange
        var id = "test-note-id";

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

        // Act
        var result = await _controller.UpdateNote(id, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteNote_WithExistingNote_ReturnsNoContent()
    {
        // Arrange
        var id = "test-note-id";
        var existingEntity = new NoteEntity
        {
            Name = "Test Note",
            SideNote = "Test Description",
            ImageId = Guid.NewGuid(),
            Id = id
        };

        _dbContext.Notes.Add(existingEntity);
        await _dbContext.SaveChangesAsync();

        _mockTableService.Setup(x => x.DeleteRangeAsync<NoteEntryEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryEntity, bool>>>())).Returns(Task.CompletedTask);

        // Act
        
        _mockTableService.Setup(x => x.GetByIdAsync<NoteEntity>(id))
                        .ReturnsAsync(existingEntity);

        _mockTableService.Setup(x => x.DeleteRangeAsync<NoteEntryRuleEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryRuleEntity, bool>>>()))
                        .Returns(Task.CompletedTask);
        
        _mockTableService.Setup(x => x.DeleteRangeAsync<NoteEntryOverrideEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryOverrideEntity, bool>>>()))
                        .Returns(Task.CompletedTask);
        var result = await _controller.DeleteNote(id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
    }

    [Fact]
    public async Task DeleteNote_WithNonExistingNote_ReturnsNotFound()
    {
        // Arrange
        var id = "non-existing-key";

        // Act
        var result = await _controller.DeleteNote(id);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}





