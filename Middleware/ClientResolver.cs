// Middleware/ClientResolver.cs
using Microsoft.EntityFrameworkCore;
using PickleballBookingSystem.Data;

namespace PickleballBookingSystem.Middleware;

public class ClientResolver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly ILogger<ClientResolver> _logger;

    private const string ItemKey = "ResolvedClientId";
    private const string SubdomainItemKey = "ResolvedSubdomain";

    public ClientResolver(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration config,
        AppDbContext db,
        ILogger<ClientResolver> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _config = config;
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// The subdomain for the current request.
    ///
    /// Rules:
    ///   1. If X-Client-Subdomain header is PRESENT, it MUST be allowlisted.
    ///      If it isn't, we return null — NO fallback. (Tenant isolation.)
    ///   2. If the header is ABSENT, fall back to Clients:DefaultSubdomain
    ///      (which must itself be allowlisted).
    ///   3. If neither applies, infer from Host (allowlisted only).
    ///
    /// Returns null when no valid tenant is identified. Controllers translate
    /// that into 409 via InvalidOperationException.
    /// </summary>
    public string? GetSubdomain()
    {
        var http = _httpContextAccessor.HttpContext;
        if (http is null) return null;

        // Cache per-request so we don't repeat the work in every controller
        if (http.Items[SubdomainItemKey] is string cached)
            return cached;

        var allowlist = _config.GetSection("Clients:AllowedSubdomains").Get<string[]>()
            ?? Array.Empty<string>();
        var defaultSub = _config["Clients:DefaultSubdomain"];

        var headerVal = http.Request.Headers["X-Client-Subdomain"]
            .FirstOrDefault()?.Trim();

        // ── Rule 1: header present → must be allowlisted, NO fallback ──
        if (!string.IsNullOrEmpty(headerVal))
        {
            if (allowlist.Contains(headerVal, StringComparer.OrdinalIgnoreCase))
            {
                http.Items[SubdomainItemKey] = headerVal;
                return headerVal;
            }

            _logger.LogWarning(
                "ClientResolver: header subdomain {Subdomain} not in allowlist, rejecting",
                headerVal);
            return null;
        }

        // ── Rule 2: no header → default subdomain (if allowlisted) ──
        if (!string.IsNullOrEmpty(defaultSub)
            && allowlist.Contains(defaultSub, StringComparer.OrdinalIgnoreCase))
        {
            http.Items[SubdomainItemKey] = defaultSub;
            return defaultSub;
        }

        // ── Rule 3: no header, no default → infer from Host (allowlisted only) ──
        var host = http.Request.Host.Host;
        if (!string.IsNullOrEmpty(host))
        {
            var parts = host.Split('.');
            if (parts.Length >= 2 && parts[0] != "www" && parts[0] != "localhost")
            {
                var candidate = parts[0];
                if (allowlist.Contains(candidate, StringComparer.OrdinalIgnoreCase))
                {
                    http.Items[SubdomainItemKey] = candidate;
                    return candidate;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Resolve the current client's GUID. Caches per-request.
    /// Throws InvalidOperationException (→ 409 via ExceptionMiddleware) if
    /// the client isn't identified — this is the fix for the "public endpoint
    /// returns 401 and redirects customers to login" bug.
    /// </summary>
    public async Task<Guid> ResolveClientIdAsync()
    {
        var http = _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("Client identification required");

        if (http.Items[ItemKey] is Guid cached)
            return cached;

        var subdomain = GetSubdomain()
            ?? throw new InvalidOperationException("Client identification required");

        var client = await _db.Clients
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Subdomain == subdomain && c.Status == "active")
            ?? throw new InvalidOperationException("Client identification required");

        http.Items[ItemKey] = client.Id;
        return client.Id;
    }
}