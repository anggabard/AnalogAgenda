using System.Collections.Concurrent;
using System.Net;

namespace AnalogAgenda.Server.Middleware;

public class RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<RateLimitingMiddleware> _logger = logger;
    
    // Store request counts per IP
    private static readonly ConcurrentDictionary<string, ClientRequestInfo> _clients = new();
    
    private const int MaxRequests = 30; 
    private static readonly TimeSpan TimeWindow = TimeSpan.FromMinutes(1);
    private const int MaxLoginAttempts = 5;
    private static readonly TimeSpan LoginWindow = TimeSpan.FromMinutes(3);

    public async Task InvokeAsync(HttpContext context)
    {     
        var clientIp = GetClientIpAddress(context);
        var endpoint = context.Request.Path.ToString().ToLowerInvariant();
        
        // Check if this is an authentication endpoint
        var isAuthEndpoint = IsAuthenticationEndpoint(endpoint);
        
        if (isAuthEndpoint)
        {
            var client = _clients.GetOrAdd(clientIp, _ => new ClientRequestInfo());
            
            // Clean up old requests
            CleanupOldRequests(client);
            
            // Check rate limits
            if (IsRateLimited(client, endpoint))
            {
                _logger.LogWarning("Rate limit exceeded for IP: {ClientIp} on endpoint: {Endpoint}", 
                    clientIp, endpoint);
                
                await ReturnRateLimitResponse(context);
                return;
            }
            
            // Record this request
            RecordRequest(client, endpoint);
        }
        
        await _next(context);
    }

    private static string GetClientIpAddress(HttpContext context)
    {
        // Try to get the real IP from headers first (in case of proxies/load balancers)
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
        
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }
        
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static bool IsAuthenticationEndpoint(string endpoint)
    {
        var authEndpoints = new[]
        {
            "/api/account/login",
            "/api/account/changepassword"
        };
        
        return authEndpoints.Any(authEndpoint => 
            endpoint.StartsWith(authEndpoint, StringComparison.OrdinalIgnoreCase));
    }

    private static void CleanupOldRequests(ClientRequestInfo client)
    {
        var cutoffTime = DateTime.UtcNow.Subtract(TimeWindow);
        var loginCutoffTime = DateTime.UtcNow.Subtract(LoginWindow);
        
        lock (client.Lock)
        {
            client.GeneralRequests.RemoveAll(time => time < cutoffTime);
            client.LoginAttempts.RemoveAll(time => time < loginCutoffTime);
        }
    }

    private static bool IsRateLimited(ClientRequestInfo client, string endpoint)
    {
        lock (client.Lock)
        {
            if (endpoint.Contains("login", StringComparison.OrdinalIgnoreCase))
            {
                return client.LoginAttempts.Count >= MaxLoginAttempts;
            }
            
            return client.GeneralRequests.Count >= MaxRequests;
        }
    }

    private static void RecordRequest(ClientRequestInfo client, string endpoint)
    {
        lock (client.Lock)
        {
            if (endpoint.Contains("login", StringComparison.OrdinalIgnoreCase))
            {
                client.LoginAttempts.Add(DateTime.UtcNow);
            }
            else
            {
                client.GeneralRequests.Add(DateTime.UtcNow);
            }
        }
    }

    private static async Task ReturnRateLimitResponse(HttpContext context)
    {
        context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            error = "Rate limit exceeded",
            message = "Too many requests. Please try again later.",
            retryAfter = TimeWindow.TotalSeconds
        };
        
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }

}

public class ClientRequestInfo
{
    public List<DateTime> GeneralRequests { get; } = new();
    public List<DateTime> LoginAttempts { get; } = new();
    public readonly object Lock = new();
}
