// Services/SecurityLogger.cs
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PickleballBookingSystem.Data;
using PickleballBookingSystem.Entities;
using PickleballBookingSystem.Interfaces;

namespace PickleballBookingSystem.Services;

public class SecurityLogger : ISecurityLogger
{
    private readonly AppDbContext _db;
    private readonly ILogger<SecurityLogger> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SecurityLogger(
        AppDbContext db,
        ILogger<SecurityLogger> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(SecurityEvent evt)
    {
        try
        {
            if (evt.Id == Guid.Empty) evt.Id = Guid.NewGuid();
            if (evt.CreatedAt == default) evt.CreatedAt = DateTime.UtcNow;

            // ✅ Auto-enrich IP + User-Agent from the current request if the
            // caller didn't set them. This closes the gap where booking.created,
            // auth.password_changed, admin.staff_* and similar events had no IP.
            EnrichFromHttpContext(evt);

            if (evt.Metadata is not null && evt.Metadata.Length > 8000)
                evt.Metadata = evt.Metadata.Substring(0, 8000);

            _db.SecurityEvents.Add(evt);
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // Never throw from a security log — the request is more important
            // than the audit trail. But do log the failure so we notice if the
            // events table goes missing.
            _logger.LogError(ex, "Failed to persist security event {EventType}", evt.EventType);
        }
    }

    public async Task LogAuthFailureAsync(
        Guid clientId,
        string email,
        string? ip,
        string? userAgent,
        string eventType = "auth.login_failed",
        string? message = null)
    {
        var evt = new SecurityEvent
        {
            ClientId = clientId,
            IpHash = HashIp(ip),
            IpAddress = string.IsNullOrWhiteSpace(ip) ? null : ip,
            UserAgent = Truncate(userAgent, 512),
            EventType = eventType,
            Severity = eventType.Contains("locked", StringComparison.OrdinalIgnoreCase)
                ? "warning" : "info",
            Message = message,
            Metadata = JsonSerializer.Serialize(new { email }),
            Success = false,
            ErrorCode = "AUTH_FAILED",
        };

        await LogAsync(evt);
    }

    public string HashIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return string.Empty;

        var bytes = Encoding.UTF8.GetBytes(ip.Trim());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();   // 64 chars
    }

    // ✅ Enrich any event that didn't set IP / User-Agent explicitly.
    private void EnrichFromHttpContext(SecurityEvent evt)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx is null) return;

        if (string.IsNullOrEmpty(evt.IpAddress) && string.IsNullOrEmpty(evt.IpHash))
        {
            var ip = ctx.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(ip))
            {
                evt.IpAddress = ip;
                evt.IpHash = HashIp(ip);
            }
        }

        if (string.IsNullOrEmpty(evt.UserAgent))
        {
            var ua = ctx.Request.Headers["User-Agent"].FirstOrDefault();
            evt.UserAgent = Truncate(ua, 512);
        }
    }

    private static string? Truncate(string? s, int max)
        => s is null ? null : (s.Length <= max ? s : s.Substring(0, max));
}