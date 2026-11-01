// Entities/SecurityEvent.cs
namespace PickleballBookingSystem.Entities;

public class SecurityEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClientId { get; set; }

    // ✅ SHA-256 of the IP — safe to store long-term for abuse correlation
    public string? IpHash { get; set; }

    // ✅ Raw IP — kept short-term only (SecurityRetentionWorker purges after 30 days)
    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    // e.g. "auth.login_failed", "auth.login_locked", "auth.password_changed",
    //      "auth.password_reset_requested", "auth.password_reset_completed",
    //      "admin.staff_created", "booking.created", "rate_limit.triggered"
    public string EventType { get; set; } = string.Empty;

    // "info" | "warning" | "critical"
    public string Severity { get; set; } = "info";

    public string? Message { get; set; }

    // jsonb — see AppDbContext config
    public string? Metadata { get; set; }

    public bool Success { get; set; } = true;

    public string? ErrorCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}