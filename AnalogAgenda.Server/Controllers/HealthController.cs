using Configuration.Sections;
using Database.Data;
using Database.DBObjects.Enums;
using Database.Entities;
using Database.Helpers;
using Database.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AnalogAgenda.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController(
    IDatabaseService databaseService,
    AnalogAgendaDbContext dbContext,
    IBlobService blobService,
    AzureAd azureAdConfig) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
        var baseInfo = new
        {
            message = "API is operational",
            timestamp = DateTime.UtcNow.ToString("o"),
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        };

        try
        {
            // Test database connection (database-level operation, requires dbContext)
            var canConnect = await dbContext.Database.CanConnectAsync();

            if (canConnect)
            {
                // Get basic statistics from database using DatabaseService
                var users = await databaseService.GetAllAsync<UserEntity>();
                var notes = await databaseService.GetAllAsync<NoteEntity>();
                var films = await databaseService.GetAllAsync<FilmEntity>();
                var devKits = await databaseService.GetAllAsync<DevKitEntity>();
                var sessions = await databaseService.GetAllAsync<SessionEntity>();

                var usersCount = users.Count;
                var notesCount = notes.Count;
                var filmsCount = films.Count;
                var devKitsCount = devKits.Count;
                var sessionsCount = sessions.Count;

                // Test blob storage connectivity
                var blobStatus = await TestBlobStorageAsync();
                
                // Test Azure AD credentials
                var azureAdStatus = await TestAzureAdCredentialsAsync();

                var blobConnected = ((dynamic)blobStatus).connected;
                var azureAdValid = ((dynamic)azureAdStatus).valid;
                var allHealthy = blobConnected && azureAdValid;

                var statusCode = allHealthy ? 200 : 503;

                return StatusCode(statusCode, new
                {
                    baseInfo.message,
                    baseInfo.timestamp,
                    baseInfo.environment,
                    database = new
                    {
                        connected = true,
                        canConnect = true,
                        error = (string?)null,
                        statistics = new
                        {
                            usersCount,
                            notesCount,
                            filmsCount,
                            devKitsCount,
                            sessionsCount
                        }
                    },
                    blobStorage = blobStatus,
                    azureAd = azureAdStatus
                });
            }
            else
            {
                return StatusCode(503, new
                {
                    baseInfo.message,
                    baseInfo.timestamp,
                    baseInfo.environment,
                    database = new
                    {
                        connected = false,
                        canConnect = false,
                        error = "Database exists but cannot connect",
                        statistics = new
                        {
                            usersCount = 0,
                            notesCount = 0,
                            filmsCount = 0,
                            devKitsCount = 0,
                            sessionsCount = 0
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            // Test blob storage and Azure AD even if database fails
            var blobStatus = await TestBlobStorageAsync();
            var azureAdStatus = await TestAzureAdCredentialsAsync();

            return StatusCode(503, new
            {
                baseInfo.message,
                baseInfo.timestamp,
                baseInfo.environment,
                database = new
                {
                    connected = false,
                    canConnect = false,
                    error = ex.Message,
                    statistics = new
                    {
                        usersCount = 0,
                        notesCount = 0,
                        filmsCount = 0,
                        devKitsCount = 0,
                        sessionsCount = 0
                    }
                },
                blobStorage = blobStatus,
                azureAd = azureAdStatus
            });
        }
    }

    private async Task<object> TestBlobStorageAsync()
    {
        try
        {
            // Try to access a known container to test connectivity
            var container = blobService.GetBlobContainer(ContainerName.films);
            
            // Test if we can check container existence
            var exists = await container.ExistsAsync();
            
            return new
            {
                connected = true,
                error = (string?)null,
                containerExists = exists.Value
            };
        }
        catch (Exception ex)
        {
            return new
            {
                connected = false,
                error = ex.Message,
                containerExists = false
            };
        }
    }

    private async Task<object> TestAzureAdCredentialsAsync()
    {
        try
        {
            // Create credential and try to get a token to validate credentials
            var credential = azureAdConfig.GetClientSecretCredential();
            
            // Try to get a token with a common scope (storage scope)
            var tokenRequestContext = new Azure.Core.TokenRequestContext(new[] { "https://storage.azure.com/.default" });
            var token = await credential.GetTokenAsync(tokenRequestContext, default);
            
            return new
            {
                valid = true,
                error = (string?)null,
                tokenExpiresOn = token.ExpiresOn.ToString("o")
            };
        }
        catch (ArgumentException ex)
        {
            // Configuration error
            return new
            {
                valid = false,
                error = $"Configuration error: {ex.Message}",
                tokenExpiresOn = (string?)null
            };
        }
        catch (Exception ex)
        {
            // Authentication error
            return new
            {
                valid = false,
                error = ex.Message,
                tokenExpiresOn = (string?)null
            };
        }
    }
}

