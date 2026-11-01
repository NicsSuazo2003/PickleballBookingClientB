// DTOs/SecurityEventDto.cs
namespace PickleballBookingSystem.DTOs;

public record SecurityEventDto(
    string Id,
    string ClientId,
    string? IpHash,
    string? IpAddress,
    string? UserAgent,
    string EventType,
    string Severity,
    string? Message,
    object? Metadata,
    bool Success,
    string? ErrorCode,
    string CreatedAt
);

/// <summary>
/// Query parameters for GET /api/admin/security-events
/// </summary>
public record SecurityEventQuery(
    string? EventType = null,
    string? Severity = null,
    string? Search = null,
    DateTime? From = null,
    DateTime? To = null,
    bool HideRateLimit = true,     // frontend toggle default: hide 429 noise
    int Take = 100
);