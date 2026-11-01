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

    public SecurityLogger(AppDbContext db, ILogger<SecurityLogger> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task LogAsync(SecurityEvent evt)
    {
        try
        {
            if (evt.Id == Guid.Empty) evt.Id = Guid.NewGuid();
            if (evt.CreatedAt == default) evt.CreatedAt = DateTime.UtcNow;

            if (evt.Metadata is not null && evt.Metadata.Length > 8000)
            {
                // Truncate oversized metadata rather than fail the insert
                evt.Metadata = evt.Metadata.Substring(0, 8000);
            }

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

    private static string? Truncate(string? s, int max)
        => s is null ? null : (s.Length <= max ? s : s.Substring(0, max));
}