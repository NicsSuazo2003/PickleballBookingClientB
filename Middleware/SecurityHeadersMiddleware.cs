// Middleware/SecurityHeadersMiddleware.cs
namespace PickleballBookingSystem.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent MIME sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking
        headers["X-Frame-Options"] = "DENY";

        // Referrer policy — don't leak URLs to third parties
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Disable the legacy XSS auditor (browsers now handle this via CSP)
        headers["X-XSS-Protection"] = "0";

        // Permissions Policy — disable unnecessary browser features
        headers["Permissions-Policy"] =
            "camera=(), microphone=(), geolocation=(), payment=()";

        // HSTS — only on HTTPS
        if (context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }

        // Note: no Content-Security-Policy here. The API returns JSON,
        // and the SPA is served from Vercel — CSP belongs on the Vercel
        // side (vercel.json / next.config). Adding a permissive CSP here
        // would give false confidence.

        await _next(context);
    }
}