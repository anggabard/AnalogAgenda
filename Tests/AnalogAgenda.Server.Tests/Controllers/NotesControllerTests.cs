using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using AnalogAgenda.Server.Controllers;
using Database.DTOs;
using Database.Entities;
using Database.Services.Interfaces;
using Configuration.Sections;
using Azure.Storage.Blobs;
using Azure.Data.Tables;

namespace Tests.AnalogAgenda.Server.Tests.Controllers;

public class NotesControllerTests
{
    private readonly Mock<ITableService> _mockTableService;
    private readonly Mock<IBlobService> _mockBlobService;
    private readonly Mock<Storage> _mockStorageConfig;
    private readonly NotesController _controller;

    public NotesControllerTests()
    {
        _mockTableService = new Mock<ITableService>();
        _mockBlobService = new Mock<IBlobService>();
        _mockStorageConfig = new Mock<Storage>();
        
        _controller = new NotesController(_mockStorageConfig.Object, _mockTableService.Object, _mockBlobService.Object);
    }

    [Fact]
    public async Task GetMergedNotes_WithValidCompositeId_ReturnsListOfNotes()
    {
        // Arrange
        var compositeId = "ABCD1234"; // 2 notes with interleaved rowKeys "AC13" and "BD24"
        var note1 = new NoteEntity { RowKey = "AC13", Name = "Note 1" };
        var note2 = new NoteEntity { RowKey = "BD24", Name = "Note 2" };
        
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<NoteEntity>("AC13"))
            .ReturnsAsync(note1);
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<NoteEntity>("BD24"))
            .ReturnsAsync(note2);
        _mockTableService.Setup(x => x.GetTableEntriesAsync<NoteEntryEntity>(It.IsAny<System.Linq.Expressions.Expression<Func<NoteEntryEntity, bool>>>()))
            .ReturnsAsync(new List<NoteEntryEntity>());
        _mockTableService.Setup(x => x.GetTableEntriesAsync<NoteEntryRuleEntity>())
            .ReturnsAsync(new List<NoteEntryRuleEntity>());
        _mockTableService.Setup(x => x.GetTableEntriesAsync<NoteEntryOverrideEntity>())
            .ReturnsAsync(new List<NoteEntryOverrideEntity>());

        // Act
        var result = await _controller.GetMergedNotes(compositeId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult?.Value);
        var notes = okResult.Value as List<NoteDto>;
        Assert.NotNull(notes);
        Assert.Equal(2, notes!.Count);
        Assert.Contains(notes, n => n.Name == "Note 1");
        Assert.Contains(notes, n => n.Name == "Note 2");
    }

    [Fact]
    public async Task GetMergedNotes_WithInvalidCompositeId_ReturnsNotFound()
    {
        // Arrange
        var invalidCompositeId = "INVALID"; // Will decode to "INVA" but note won't exist
        
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<NoteEntity>(It.IsAny<string>()))
            .ReturnsAsync((NoteEntity?)null);

        // Act
        var result = await _controller.GetMergedNotes(invalidCompositeId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GetMergedNotes_WithNonExistentNotes_ReturnsNotFound()
    {
        // Arrange
        var compositeId = "ABCD1234";
        
        _mockTableService.Setup(x => x.GetTableEntryIfExistsAsync<NoteEntity>(It.IsAny<string>()))
            .ReturnsAsync((NoteEntity?)null);

        // Act
        var result = await _controller.GetMergedNotes(compositeId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public void DecodeCompositeId_WithValidId_ReturnsCorrectRowKeys()
    {
        // Arrange
        var compositeId = "ABCD1234"; // 2 notes: interleaved to "AC13" and "BD24"
        var controller = new TestableNotesController(_mockStorageConfig.Object, _mockTableService.Object, _mockBlobService.Object);

        // Act
        var result = controller.TestDecodeCompositeId(compositeId);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("AC13", result);
        Assert.Contains("BD24", result);
    }

    [Fact]
    public void DecodeCompositeId_WithThreeNotes_ReturnsCorrectRowKeys()
    {
        // Arrange
        var compositeId = "ABC123DEF456"; // 3 notes: "A1D4", "B2E5", "C3F6"
        var controller = new TestableNotesController(_mockStorageConfig.Object, _mockTableService.Object, _mockBlobService.Object);

        // Act
        var result = controller.TestDecodeCompositeId(compositeId);

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains("A1D4", result);
        Assert.Contains("B2E5", result);
        Assert.Contains("C3F6", result);
    }
}

// Test helper class to access private methods
public class TestableNotesController : NotesController
{
    public TestableNotesController(Storage storageCfg, ITableService tablesService, IBlobService blobsService) 
        : base(storageCfg, tablesService, blobsService)
    {
    }

    public List<string> TestDecodeCompositeId(string compositeId)
    {
        // Use reflection to access private method
        var method = typeof(NotesController).GetMethod("DecodeCompositeId", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (List<string>)method!.Invoke(this, new object[] { compositeId })!;
    }
}