using AnalogAgenda.Server.Middleware;
using AnalogAgenda.Server.Validators;
using Configuration;
using Database.Data;
using Database.DBObjects.Enums;
using Database.Services;
using Database.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Load configuration early so it's available for Data Protection setup
// This allows appsettings.Development.json to be used for local development
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

builder.AddServiceDefaults();

// Configure Data Protection to use Azure Blob Storage for shared keys across replicas
// This is CRITICAL for cookie authentication in multi-replica scenarios
// Without this, cookies encrypted by one replica cannot be decrypted by another replica
// which causes 401 errors when requests are load-balanced to different replicas
try
{
    // Try to get connection string from Aspire (injected as ConnectionStrings__analogagendastorage)
    // or environment variables (Azure Container Apps sets STORAGE_CONNECTION_STRING)
    var connectionString = builder.Configuration.GetConnectionString("analogagendastorage")
        ?? Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");
    
    if (!string.IsNullOrEmpty(connectionString))
    {
        builder.Services.AddDataProtection()
            .PersistKeysToAzureBlobStorage(connectionString, ContainerName.dataprotectionkeys.ToString(), "keys.xml")
            .SetApplicationName("AnalogAgenda");
    }
    else
    {
        // Fallback: Use in-memory keys (NOT RECOMMENDED for production with multiple replicas)
        // This WILL cause 401 errors when requests hit different replicas
        // Each replica will have different keys, so cookies won't work across replicas
        builder.Services.AddDataProtection()
            .SetApplicationName("AnalogAgenda");
    }
}
catch (Exception)
{
    // If data protection configuration fails, log but continue
    // This allows the app to start even if blob storage isn't available
    builder.Services.AddDataProtection().SetApplicationName("AnalogAgenda");
}

// Add SQL Server DbContext via Aspire
builder.AddSqlServerDbContext<AnalogAgendaDbContext>("analogagendadb");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddScoped<IValidator<Database.DTOs.LoginDto>, LoginDtoValidator>();
builder.Services.AddScoped<IValidator<Database.DTOs.ChangePasswordDto>, ChangePasswordDtoValidator>();
builder.Services.AddScoped<IValidator<Database.DTOs.FilmDto>, FilmDtoValidator>();

var isDev = builder.Environment.IsDevelopment();
builder.Configuration["System:IsDev"] = isDev.ToString();

builder.Services.AddAzureAdConfigBinding();
builder.Services.AddStorageConfigBinding();
builder.Services.AddSystemConfigBinding();
builder.Services.AddSecurityConfigBinding();

// Register database and blob services
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IBlobService, BlobService>();

// Register converter services
builder.Services.AddScoped<DtoConvertor>();
builder.Services.AddScoped<EntityConvertor>();

// Configure Kestrel to accept larger request bodies (for single photo uploads: 30MB file + base64 overhead = ~40MB, rounded to 60MB)
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 100_000_000; // 100MB to accommodate base64-encoded 30MB images
});

// Configure form options for multipart form data
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 100_000_000; // 100MB
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.Cookie.Name = ".AnalogAgenda.Auth";
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = isDev
            ? SameSiteMode.None
            : SameSiteMode.Lax; 
        opt.Cookie.SecurePolicy = isDev
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        opt.ExpireTimeSpan = TimeSpan.FromDays(7);
        
        opt.SlidingExpiration = true;

        opt.Events.OnRedirectToLogin = context =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        opt.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", corsBuilder =>
    {
        // Frontend URL from Aspire (set via AppHost.cs) or environment variable
        var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? Environment.GetEnvironmentVariable("FRONTEND_URL");
        var allowedOrigins = new HashSet<string>();

        if (!string.IsNullOrEmpty(frontendUrl))
        {
            // Remove trailing slash if present
            frontendUrl = frontendUrl.TrimEnd('/');
            allowedOrigins.Add(frontendUrl);
            
            // Also add http version if it's https (for local development)
            if (frontendUrl.StartsWith("https://") && isDev)
            {
                allowedOrigins.Add(frontendUrl.Replace("https://", "http://"));
            }
        }

        corsBuilder.WithOrigins([.. allowedOrigins])
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

var app = builder.Build();

// Use IsDevelopment() for app-level checks
var appIsDev = app.Environment.IsDevelopment();

// Log data protection configuration status after app is built
var dataProtectionConnectionString = app.Configuration.GetConnectionString("analogagendastorage")
    ?? Environment.GetEnvironmentVariable("STORAGE_CONNECTION_STRING");
if (string.IsNullOrEmpty(dataProtectionConnectionString))
{
    app.Logger.LogWarning(
        "Data Protection using in-memory keys - cookies will NOT work across replicas! " +
        "Set ConnectionStrings__analogagendastorage in appsettings or STORAGE_CONNECTION_STRING environment variable to fix this. " +
        "This causes 401 errors when requests are load-balanced to different replicas."
    );
}
else
{
    app.Logger.LogInformation("Data Protection configured to use Azure Blob Storage for shared keys across replicas");
}

// Apply pending migrations automatically on startup (Development or Docker Compose deployment)
// In production (Azure), migrations are run via a separate job/process before deployment
var runMigrations = appIsDev || app.Environment.EnvironmentName.Equals("Docker", StringComparison.OrdinalIgnoreCase);
if (runMigrations)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AnalogAgendaDbContext>();
    try
    {
        // Check if there are pending migrations before applying
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            app.Logger.LogInformation("Applying {Count} pending migrations...", pendingMigrations.Count());
            await dbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            app.Logger.LogInformation("No pending migrations. Database is up to date.");
        }
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "An error occurred while applying database migrations.");
        throw; // Re-throw to prevent the app from starting with an incorrect database state
    }
}

app.MapDefaultEndpoints();

// CORS must be before authentication/authorization to handle OPTIONS preflight requests
// Always use CORS for Aspire and production
app.UseCors("Frontend");

app.UseHttpsRedirection();

// Authentication and authorization - OPTIONS requests will bypass this via CORS middleware
app.UseAuthentication();
app.UseAuthorization();

// Add security headers middleware after CORS to avoid interfering with CORS headers
app.UseMiddleware<SecurityHeadersMiddleware>();

if (appIsDev)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
