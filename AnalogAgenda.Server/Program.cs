using AnalogAgenda.Server.Middleware;
using AnalogAgenda.Server.Validators;
using Configuration;
using Database.Services;
using Database.Services.Interfaces;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();
builder.Services.AddScoped<IValidator<Database.DTOs.LoginDto>, LoginDtoValidator>();
builder.Services.AddScoped<IValidator<Database.DTOs.ChangePasswordDto>, ChangePasswordDtoValidator>();
builder.Services.AddAzureAdConfigBinding();
builder.Services.AddStorageConfigBinding();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.Cookie.Name = ".AnalogAgenda.Auth";
        opt.Cookie.HttpOnly = true;
        opt.Cookie.SameSite = SameSiteMode.None;
        opt.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        opt.ExpireTimeSpan = TimeSpan.FromDays(1);

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

builder.Services.AddSingleton<ITableService, TableService>();
builder.Services.AddSingleton<IBlobService, BlobService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", builder =>
    {
        builder.WithOrigins("https://localhost:58774")
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

var app = builder.Build();

// Add global exception handling middleware early in the pipeline
app.UseMiddleware<GlobalExceptionMiddleware>();

// Add rate limiting middleware for authentication endpoints
app.UseMiddleware<RateLimitingMiddleware>();

// Add security headers middleware
app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseCors("Frontend");
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();

// Make the implicit Program class public for testing
public partial class Program { }
