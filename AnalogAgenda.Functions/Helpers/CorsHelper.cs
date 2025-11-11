using Microsoft.Azure.Functions.Worker.Http;

namespace AnalogAgenda.Functions.Helpers;

public static class CorsHelper
{
    private const string AllowedOrigin = "https://analogagenda.site";

    /// <summary>
    /// Handles CORS preflight (OPTIONS) requests
    /// </summary>
    public static async Task<HttpResponseData> HandlePreflightRequestAsync(HttpRequestData req)
    {
        var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
        AddCorsHeaders(response);
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
    public static void AddCorsHeaders(HttpResponseData response)
    {
        response.Headers.Add("Access-Control-Allow-Origin", AllowedOrigin);
    }
}

