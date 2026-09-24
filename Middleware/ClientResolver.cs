namespace PickleballBookingSystem.Middleware;

public class ClientResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ClientResolver(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? GetSubdomain()
    {
        // Prefer the explicit header sent by the frontend
        var headerSubdomain = _httpContextAccessor.HttpContext?.Request.Headers["X-Client-Subdomain"].FirstOrDefault();
        if (!string.IsNullOrEmpty(headerSubdomain))
            return headerSubdomain;

        // Fallback: infer from host (useful if you later map real subdomains, e.g. picklejoe.yourapp.com)
        var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
        if (string.IsNullOrEmpty(host)) return null;

        var parts = host.Split('.');
        if (parts.Length >= 2 && parts[0] != "www" && parts[0] != "localhost")
            return parts[0];

        return null;
    }
}