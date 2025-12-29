namespace CrmApp.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Content-Security-Policy
        // Allow unsafe-eval and unsafe-inline to fix user's issue and support CDN scripts
        context.Response.Headers.Append("Content-Security-Policy", 
            "default-src 'self'; " + 
            "script-src 'self' 'unsafe-inline' 'unsafe-eval' https://cdn.jsdelivr.net https://code.jquery.com https://cdnjs.cloudflare.com; " + 
            "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com https://cdnjs.cloudflare.com; " + 
            "font-src 'self' https://fonts.gstatic.com https://cdnjs.cloudflare.com; " + 
            "img-src 'self' data:; " + 
            "connect-src 'self' ws: https://cdn.jsdelivr.net;");

        // Other security headers
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        await _next(context);
    }
}
