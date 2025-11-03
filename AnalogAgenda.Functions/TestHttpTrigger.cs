using Database.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Net;

namespace AnalogAgenda.Functions
{
    public class TestHttpTrigger(
        ILoggerFactory loggerFactory,
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

                // Test connection with a simple query
                var canConnect = await dbContext.Database.CanConnectAsync();

                if (canConnect)
                {
                    _logger.LogInformation("Database connection successful, fetching statistics...");

                    // Get basic statistics from database
                    var usersCount = await dbContext.Users.CountAsync();
                    var notesCount = await dbContext.Notes.CountAsync();
                    var filmsCount = await dbContext.Films.CountAsync();
                    var devKitsCount = await dbContext.DevKits.CountAsync();
                    var sessionsCount = await dbContext.Sessions.CountAsync();

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

