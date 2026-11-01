// Interfaces/ISecurityLogger.cs
using PickleballBookingSystem.Entities;

namespace PickleballBookingSystem.Interfaces;

public interface ISecurityLogger
{
    /// <summary>
    /// Persist a security event. Implementations MUST NOT throw — a failed
    /// log write must never break the request it's trying to audit.
    /// </summary>
    Task LogAsync(SecurityEvent evt);

    /// <summary>
    /// Convenience helper for the common "log an auth failure" case.
    /// </summary>
    Task LogAuthFailureAsync(
        Guid clientId,
        string email,
        string? ip,
        string? userAgent,
        string eventType = "auth.login_failed",
        string? message = null);

    /// <summary>
    /// Hash an IP address with SHA-256 so it can be stored long-term
    /// for abuse correlation without retaining the raw address.
    /// </summary>
    string HashIp(string? ip);
}