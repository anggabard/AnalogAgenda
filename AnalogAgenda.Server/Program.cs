using AnalogAgenda.Server.Middleware;
using AnalogAgenda.Server.Validators;
using Configuration;
using Database.Data;
using Database.Services;
using Database.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
builder.Services.AddAzureAdConfigBinding();
builder.Services.AddStorageConfigBinding();

// Register database and blob services
builder.Services.AddScoped<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IBlobService, BlobService>();

// Configure Kestrel to accept larger request bodies (for bulk photo uploads: 36 photos × 50MB each + base64 overhead)
builder.Services.Configure<Microsoft.AspNetCore.Server.Kestrel.Core.KestrelServerOptions>(options =>
{
    options.Limits.MaxRequestBodySize = 3L * 1024 * 1024 * 1024; // 3GB to accommodate bulk uploads of base64-encoded 50MB images
});

// Configure form options for multipart form data
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 3L * 1024 * 1024 * 1024; // 3GB
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.Cookie.Name = ".AnalogAgenda.Auth";
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = builder.Environment.IsDevelopment()
            ? SameSiteMode.None
            : SameSiteMode.Lax; 
        opt.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        opt.ExpireTimeSpan = TimeSpan.FromDays(7);

        opt.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        opt.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

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
            if (frontendUrl.StartsWith("https://") && builder.Environment.IsDevelopment())
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

// Apply pending migrations automatically on startup (Development only)
// In production, migrations should be run via a separate job/process before deployment
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<AnalogAgendaDbContext>();
        try
        {
            // Ensure database is created and apply all pending migrations
            dbContext.Database.Migrate();
            app.Logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "An error occurred while applying database migrations.");
            throw; // Re-throw to prevent the app from starting with an incorrect database state
        }
    }
}

app.MapDefaultEndpoints();

// Add global exception handling middleware early in the pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

// CORS must be before authentication/authorization to handle OPTIONS preflight requests
// Always use CORS for Aspire and production
app.UseCors("Frontend");

app.UseHttpsRedirection();

// Authentication and authorization - OPTIONS requests will bypass this via CORS middleware
app.UseAuthentication();
app.UseAuthorization();

// Add security headers middleware after CORS to avoid interfering with CORS headers
app.UseMiddleware<SecurityHeadersMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseMiddleware<RateLimitingMiddleware>();
}

app.MapControllers();

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
