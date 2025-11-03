using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Database.Data;

/// <summary>
/// Design-time factory for EF Core migrations
/// </summary>
public class AnalogAgendaDbContextFactory : IDesignTimeDbContextFactory<AnalogAgendaDbContext>
{
    public AnalogAgendaDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AnalogAgendaDbContext>();
        
        // This connection string is only used for migrations
        // In production, it will be overridden by Aspire or appsettings
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=AnalogAgendaDb;Trusted_Connection=True;");

        return new AnalogAgendaDbContext(optionsBuilder.Options);
    }
}

