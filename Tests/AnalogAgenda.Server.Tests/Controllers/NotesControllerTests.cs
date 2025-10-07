using AnalogAgenda.Server.Controllers;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Configuration.Sections;
using Database.DBObjects.Enums;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AnalogAgenda.Server.Tests.Controllers;

public class NotesControllerTests
{
    private readonly Mock<ITableService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<TableClient> _mockNotesTableClient;
    private readonly Mock<TableClient> _mockEntriesTableClient;
    private readonly Mock<BlobContainerClient> _mockContainerClient;
    private readonly Storage _storageConfig;
    private readonly NotesController _controller;

    public NotesControllerTests()
    {
        _mockTableService = new Mock<ITableService>();
        _mockBlobService = new Mock<IBlobService>();
        _mockNotesTableClient = new Mock<TableClient>();
        _mockEntriesTableClient = new Mock<TableClient>();
        _mockContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        _mockTableService.Setup(x => x.GetTable(TableName.Notes))
                        .Returns(_mockNotesTableClient.Object);
        
        _mockTableService.Setup(x => x.GetTable(TableName.NotesEntries))
                        .Returns(_mockEntriesTableClient.Object);
        
        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.notes))
                       .Returns(_mockContainerClient.Object);

        _controller = new NotesController(_storageConfig, _mockTableService.Object, _mockBlobService.Object);
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

        _mockNotesTableClient.Setup(x => x.AddEntityAsync(It.IsAny<NoteEntity>(), default))
                            .Returns(Task.FromResult(It.IsAny<Azure.Response>()));

        // Act
        var result = await _controller.CreateNewNote(noteDto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        Assert.IsType<NoteDto>(createdResult.Value);
        _mockNotesTableClient.Verify(x => x.AddEntityAsync(It.IsAny<NoteEntity>(), default), Times.Once);
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

        _mockNotesTableClient.Setup(x => x.AddEntityAsync(It.IsAny<NoteEntity>(), default))
                            .ThrowsAsync(new Exception("Database error"));

        _mockTableService.Setup(x => x.EntryExistsAsync(It.IsAny<NoteEntity>()))
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
        // Arrange
        var noteEntities = new List<NoteEntity>
        {
            new NoteEntity
            {
                Name = "Test Note",
                SideNote = "Test Description",
                ImageId = Guid.NewGuid(),
                RowKey = "test-row-key"
            }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<NoteEntity>())
                        .ReturnsAsync(noteEntities);
                        
        var pagedResponse = new PagedResponseDto<NoteEntity>
        {
            Data = noteEntities,
            TotalCount = noteEntities.Count,
            PageSize = 5,
            CurrentPage = 1
        };
        
        _mockTableService.Setup(x => x.GetTableEntriesPagedAsync<NoteEntity>(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(pagedResponse);

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
        // Arrange
        var noteEntities = new List<NoteEntity>
        {
            new NoteEntity
            {
                Name = "Test Note",
                SideNote = "Test Description",
                ImageId = Guid.NewGuid(),
                RowKey = "test-row-key"
            }
        };

        var noteEntryEntities = new List<NoteEntryEntity>
        {
            new NoteEntryEntity
            {
                Details = "Test Entry",
                Time = 1.0,
                Process = "Test Process",
                Film = "Test Film",
                NoteRowKey = "test-row-key"
            }
        };

        _mockTableService.Setup(x => x.GetTableEntriesAsync<NoteEntity>())
                        .ReturnsAsync(noteEntities);
                        
        var pagedResponse = new PagedResponseDto<NoteEntity>
        {
            Data = noteEntities,
            TotalCount = noteEntities.Count,
            PageSize = 5,
            CurrentPage = 1
        };
        
        _mockTableService.Setup(x => x.GetTableEntriesPagedAsync<NoteEntity>(It.IsAny<int>(), It.IsAny<int>()))
                        .ReturnsAsync(pagedResponse);

        _mockTableService.Setup(x => x.GetTableEntriesAsync<NoteEntryEntity>())
                        .ReturnsAsync(noteEntryEntities);

        // Act
        var result = await _controller.GetAllNotes(withEntries: true);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var pagedResult = Assert.IsType<PagedResponseDto<NoteDto>>(okResult.Value);
        Assert.Single(pagedResult.Data);
    }

    [Fact]
    public async Task GetNoteByRowKey_WithExistingNote_ReturnsOkWithNote()
    {
        // Arrange
        var rowKey = "test-row-key";
        var noteEntity = new NoteEntity
        {
            Name = "Test Note",
            SideNote = "Test Description",
            ImageId = Guid.NewGuid(),
            RowKey = rowKey
        };

        var noteEntryEntities = new List<NoteEntryEntity>
        {
            new NoteEntryEntity
            {
                Details = "Test Entry",
                Time = 1.0,
                Process = "Test Process",
                Film = "Test Film",
                NoteRowKey = rowKey
            }
        };

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<NoteEntity>(rowKey))
                        .ReturnsAsync(noteEntity);

        _mockTableService.Setup(x => x.GetTableEntriesAsync<NoteEntryEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryEntity, bool>>>()))
                        .ReturnsAsync(noteEntryEntities);

        // Act
        var result = await _controller.GetNoteByRowKey(rowKey);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var note = Assert.IsType<NoteDto>(okResult.Value);
        Assert.NotNull(note.RowKey);  // RowKey is auto-generated
    }

    [Fact]
    public async Task GetNoteByRowKey_WithNonExistingNote_ReturnsNotFound()
    {
        // Arrange
        var rowKey = "non-existing-key";

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<NoteEntity>(rowKey))
                        .ReturnsAsync((NoteEntity?)null);

        // Act
        var result = await _controller.GetNoteByRowKey(rowKey);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Contains(rowKey, notFoundResult.Value?.ToString() ?? "");
    }

    [Fact]
    public async Task UpdateNote_WithValidData_ReturnsNoContent()
    {
        // Arrange
        var rowKey = "test-row-key";
        var existingEntity = new NoteEntity
        {
            Name = "Old Title",
            SideNote = "Old Description",
            ImageId = Guid.NewGuid(),
            RowKey = rowKey,
            CreatedDate = DateTime.UtcNow.AddDays(-1),
            ETag = new Azure.ETag("test-etag")
        };

        var updateDto = new NoteDto
        {
            Name = "New Title",
            SideNote = "New Description",
            ImageBase64 = null!,
            Entries = new List<NoteEntryDto>()
        };

        var existingEntryEntities = new List<NoteEntryEntity>();

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<NoteEntity>(rowKey))
                        .ReturnsAsync(existingEntity);

        _mockTableService.Setup(x => x.GetTableEntriesAsync<NoteEntryEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryEntity, bool>>>()))
                        .ReturnsAsync(existingEntryEntities);

        _mockNotesTableClient.Setup(x => x.UpdateEntityAsync(
                               It.IsAny<NoteEntity>(), 
                               It.IsAny<Azure.ETag>(), 
                               TableUpdateMode.Replace, 
                               default))
                            .Returns(Task.FromResult(It.IsAny<Azure.Response>()));

        _mockTableService.Setup(x => x.DeleteTableEntriesAsync(It.IsAny<IEnumerable<NoteEntryEntity>>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateNote(rowKey, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockNotesTableClient.Verify(x => x.UpdateEntityAsync(
            It.IsAny<NoteEntity>(), 
            existingEntity.ETag, 
            TableUpdateMode.Replace, 
            default), Times.Once);
    }

    [Fact]
    public async Task UpdateNote_WithNullDto_ReturnsBadRequest()
    {
        // Arrange
        var rowKey = "test-row-key";

        // Act
        var result = await _controller.UpdateNote(rowKey, null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Invalid data.", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateNote_WithNonExistingNote_ReturnsNotFound()
    {
        // Arrange
        var rowKey = "non-existing-key";
        var updateDto = new NoteDto
        {
            Name = "New Title",
            SideNote = "New Description",
            Entries = new List<NoteEntryDto>()
        };

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<NoteEntity>(rowKey))
                        .ReturnsAsync((NoteEntity?)null);

        // Act
        var result = await _controller.UpdateNote(rowKey, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteNote_WithExistingNote_ReturnsNoContent()
    {
        // Arrange
        var rowKey = "test-row-key";
        var existingEntity = new NoteEntity
        {
            Name = "Test Note",
            SideNote = "Test Description",
            ImageId = Guid.NewGuid(),
            RowKey = rowKey
        };

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<NoteEntity>(rowKey))
                        .ReturnsAsync(existingEntity);

        _mockTableService.Setup(x => x.DeleteTableEntriesAsync<NoteEntryEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryEntity, bool>>>()))
                        .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteNote(rowKey);

        // Assert
        Assert.IsType<NoContentResult>(result);
        // Note: DeleteEntityWithImageAsync from base controller handles the note deletion internally
        _mockTableService.Verify(x => x.DeleteTableEntriesAsync<NoteEntryEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryEntity, bool>>>()), Times.Once);
        _mockTableService.Verify(x => x.GetTableEntryIfExistsAsync<NoteEntity>(rowKey), Times.Once);
    }

    [Fact]
    public async Task DeleteNote_WithNonExistingNote_ReturnsNotFound()
    {
        // Arrange
        var rowKey = "non-existing-key";

        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<NoteEntity>(rowKey))
                        .ReturnsAsync((NoteEntity?)null);

        // Act
        var result = await _controller.DeleteNote(rowKey);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
