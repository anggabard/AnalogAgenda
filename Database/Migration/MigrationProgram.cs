using Configuration.Sections;
using Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Database.DataMigration;

/// <summary>
/// Standalone program to run data migration from Table Storage to SQL
/// Run with: dotnet run --project Database -- [--dry-run]
/// </summary>
public class MigrationProgram
{
    public static async Task Main(string[] args)
    {
        var dryRun = args.Contains("--dry-run");
        
        Console.WriteLine("==============================================");
        Console.WriteLine("   Analog Agenda Data Migration Utility");
        Console.WriteLine("   Table Storage → Azure SQL Database");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        // Load configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Load Azure AD configuration
        var azureAdConfig = new AzureAd
        {
            ClientId = configuration["AzureAd:ClientId"] ?? throw new Exception("AzureAd:ClientId not configured"),
            TenantId = configuration["AzureAd:TenantId"] ?? throw new Exception("AzureAd:TenantId not configured"),
            ClientSecret = configuration["AzureAd:ClientSecret"] ?? throw new Exception("AzureAd:ClientSecret not configured")
        };

        // Load Storage configuration
        var storageConfig = new Storage
        {
            AccountName = configuration["Storage:AccountName"] ?? throw new Exception("Storage:AccountName not configured")
        };

        // Setup DbContext
        var connectionString = configuration.GetConnectionString("AnalogAgendaDb")
            ?? throw new Exception("Connection string 'AnalogAgendaDb' not configured");

        var optionsBuilder = new DbContextOptionsBuilder<AnalogAgendaDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        using var dbContext = new AnalogAgendaDbContext(optionsBuilder.Options);

        // Ensure database is created
        Console.WriteLine("Ensuring database exists...");
        await dbContext.Database.MigrateAsync();
        Console.WriteLine("✓ Database ready");
        Console.WriteLine();

        // Run migration
        var migrationUtility = new DataMigrationUtility(azureAdConfig, storageConfig, dbContext, dryRun);
        await migrationUtility.MigrateAllDataAsync();

        Console.WriteLine();
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

