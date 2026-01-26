using Database.Data;
using Database.Entities;
using Database.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AnalogAgenda.Functions.Functions
{
    public class TestHttpTrigger(
        ILoggerFactory loggerFactory,
        IDatabaseService databaseService,
        AnalogAgendaDbContext dbContext)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<TestHttpTrigger>();

        [Function("Health")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "health")] HttpRequestData req,
            FunctionContext executionContext)
        {
            _logger.LogInformation("Health check HTTP trigger function executed - checking database connection");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var baseInfo = new
            {
                message = "Test HTTP trigger function is working!",
                timestamp = DateTime.UtcNow.ToString("o"),
                method = req.Method,
                url = req.Url.ToString(),
                functionName = executionContext.FunctionDefinition.Name
            };

            object status;

            // Test database connection
            try
            {
                _logger.LogInformation("Attempting to connect to database...");

                // Test connection with a simple query (database-level operation, requires dbContext)
                var canConnect = await dbContext.Database.CanConnectAsync();

                if (canConnect)
                {
                    _logger.LogInformation("Database connection successful, fetching statistics...");

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

                    status = new
                    {
                        baseInfo.message,
                        baseInfo.timestamp,
                        baseInfo.method,
                        baseInfo.url,
                        baseInfo.functionName,
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
                        }
                    };

                    _logger.LogInformation($"Database statistics: Users={usersCount}, Notes={notesCount}, Films={filmsCount}, DevKits={devKitsCount}, Sessions={sessionsCount}");
                }
                else
                {
                    status = new
                    {
                        baseInfo.message,
                        baseInfo.timestamp,
                        baseInfo.method,
                        baseInfo.url,
                        baseInfo.functionName,
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
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection test failed");

                status = new
                {
                    baseInfo.message,
                    baseInfo.timestamp,
                    baseInfo.method,
                    baseInfo.url,
                    baseInfo.functionName,
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
                    }
                };
            }

            await response.WriteStringAsync(System.Text.Json.JsonSerializer.Serialize(status));

            return response;
        }
    }
}

