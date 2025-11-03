using Database.Data;
using Microsoft.EntityFrameworkCore;

namespace AnalogAgenda.Server.Tests.Helpers;

/// <summary>
/// Factory to create in-memory DbContext for testing
/// </summary>
public static class InMemoryDbContextFactory
{
    public static AnalogAgendaDbContext Create(string databaseName = "TestDb")
    {
        var options = new DbContextOptionsBuilder<AnalogAgendaDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var context = new AnalogAgendaDbContext(options);
        
        // Ensure database is created
        context.Database.EnsureCreated();
        
        return context;
    }

    public static AnalogAgendaDbContext CreateWithData(string databaseName, Action<AnalogAgendaDbContext> seedAction)
    {
        var context = Create(databaseName);
        seedAction(context);
        context.SaveChanges();
        return context;
    }
}

