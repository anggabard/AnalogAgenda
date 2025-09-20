namespace AnalogAgenda.Server.Middleware;

public class SecurityHeadersMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        // Remove server information header
        context.Response.Headers.Remove("Server");
        
        // Add security headers
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        
        // HSTS (HTTP Strict Transport Security) - only for HTTPS
        if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }
        
        // Content Security Policy - adjust based on your needs
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob: analogagendastorage.blob.core.windows.net; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none';");
        
        // Permissions Policy (formerly Feature Policy)
        context.Response.Headers.Append("Permissions-Policy", 
            "camera=(), microphone=(), geolocation=(), payment=()");

        await _next(context);
    }
}
