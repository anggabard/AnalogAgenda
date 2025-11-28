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

public class SimpleNotesControllerTests : IDisposable
{
    private readonly AnalogAgendaDbContext _dbContext;
    private readonly IDatabaseService _databaseService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<BlobContainerClient> _mockContainerClient;
    private readonly Storage _storageConfig;
    private readonly DtoConvertor _dtoConvertor;
    private readonly EntityConvertor _entityConvertor;
    private readonly NotesController _controller;

    public SimpleNotesControllerTests()
    {
        _dbContext = InMemoryDbContextFactory.Create($"NotesTestDb_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_dbContext);
        _mockBlobService = new Mock<IBlobService>();
        _mockContainerClient = new Mock<BlobContainerClient>();
        _storageConfig = new Storage { AccountName = "teststorage" };

        var systemConfig = new Configuration.Sections.System { IsDev = false };
        _dtoConvertor = new DtoConvertor(systemConfig, _storageConfig);
        _entityConvertor = new EntityConvertor();

        _mockBlobService.Setup(x => x.GetBlobContainer(ContainerName.notes))
                       .Returns(_mockContainerClient.Object);

        _controller = new NotesController(_databaseService, _mockBlobService.Object, _dtoConvertor, _entityConvertor);
    }

    [Fact]
    public async Task CreateNewNote_WithValidDto_ReturnsCreated()
    {
        // Arrange
        var noteDto = new NoteDto
        {
            Name = "Test Note",
            SideNote = "Test Description",
            ImageBase64 = null!,
            Entries = []
        };

        // Act
        var result = await _controller.CreateNewNote(noteDto);

        // Assert
        var createdResult = Assert.IsType<CreatedResult>(result);
        var createdNote = Assert.IsType<NoteDto>(createdResult.Value);
        Assert.NotNull(createdNote.Id);
        Assert.Equal("Test Note", createdNote.Name);
    }

    [Fact]
    public async Task GetAllNotes_ReturnsAllNotes()
    {
        // Arrange
        var note1 = new NoteEntity { Name = "Note 1", Id = "note1" };
        var note2 = new NoteEntity { Name = "Note 2", Id = "note2" };
        await _databaseService.AddAsync(note1);
        await _databaseService.AddAsync(note2);

        // Act
        var result = await _controller.GetAllNotes(withEntries: false, page: 0);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var notes = Assert.IsAssignableFrom<IEnumerable<NoteDto>>(okResult.Value);
        Assert.Equal(2, notes.Count());
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}

