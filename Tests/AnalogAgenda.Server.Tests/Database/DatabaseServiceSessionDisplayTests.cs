using AnalogAgenda.Server.Tests.Helpers;
using Database.Data;
using Database.Entities;
using Database.Services;
using Database.Services.Interfaces;

namespace AnalogAgenda.Server.Tests.Database;

public class DatabaseServiceSessionDisplayTests : IDisposable
{
    private readonly AnalogAgendaDbContext _context;
    private readonly IDatabaseService _databaseService;

    public DatabaseServiceSessionDisplayTests()
    {
        _context = InMemoryDbContextFactory.Create($"SessionDisplay_{Guid.NewGuid()}");
        _databaseService = new DatabaseService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    private static SessionEntity Session(string id, int index, DateTime sessionDate) => new()
    {
        Id = id,
        Index = index,
        Name = null,
        Location = id,
        Participants = "[]",
        SessionDate = sessionDate,
        ImageId = Guid.NewGuid()
    };

    [Fact]
    public async Task GetNextSessionIndexAsync_ReturnsOne_WhenNoSessions()
    {
        var next = await _databaseService.GetNextSessionIndexAsync();
        Assert.Equal(1, next);
    }

    [Fact]
    public async Task GetNextSessionIndexAsync_ReturnsMaxPlusOne_WhenSessionsExist()
    {
        var d = DateTime.UtcNow.Date;
        await _databaseService.AddAsync(Session("a", 1, d));
        await _databaseService.AddAsync(Session("b", 5, d.AddDays(-1)));

        var next = await _databaseService.GetNextSessionIndexAsync();
        Assert.Equal(6, next);
    }
}
