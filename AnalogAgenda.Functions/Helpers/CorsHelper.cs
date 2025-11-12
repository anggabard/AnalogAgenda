using Microsoft.Azure.Functions.Worker.Http;

namespace AnalogAgenda.Functions.Helpers;

public static class CorsHelper
{
    private const string AllowedOrigin = "https://analogagenda.site";
    private const string LocalOrigin = "http://localhost:4200"; // Typical Angular dev server port

    /// <summary>
    /// Handles CORS preflight (OPTIONS) requests
    /// </summary>
    public static async Task<HttpResponseData> HandlePreflightRequestAsync(HttpRequestData req)
    {
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        AddCorsHeaders(response, req);
        response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
        response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        response.Headers.Add("Access-Control-Max-Age", "3600");
        // Ensure response body is empty for OPTIONS
        await response.WriteStringAsync(string.Empty);
        return response;
    }

    /// <summary>
    /// Adds CORS headers to a response
    /// </summary>
    public static void AddCorsHeaders(HttpResponseData response, HttpRequestData? req = null)
    {
        // Check if request is from localhost (for local development)
        string origin = AllowedOrigin;
        if (req != null)
        {
            var originHeader = req.Headers.GetValues("Origin").FirstOrDefault();
            if (!string.IsNullOrEmpty(originHeader) && 
                (originHeader.Contains("localhost") || originHeader.Contains("127.0.0.1")))
            {
                origin = originHeader; // Allow the specific localhost origin
            }
        }
        
        response.Headers.Add("Access-Control-Allow-Origin", origin);
        response.Headers.Add("Access-Control-Allow-Credentials", "true");
    }
}

