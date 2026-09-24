// DTOs/OpenPlayDTOs.cs
namespace PickleballBookingSystem.DTOs;

public record CreateOpenPlaySessionRequest(
    string[] CourtIds,              // ✅ was single CourtId
    string Date,
    string StartTime,
    string EndTime,
    int MaxPlayers,
    decimal PricePerPlayer,
    string SkillLevel,
    string? HostName,
    string? Title,
    string? Description
);

public record UpdateOpenPlaySessionRequest(
    string[] CourtIds,              // ✅ was single CourtId
    string Date,
    string StartTime,
    string EndTime,
    int MaxPlayers,
    decimal PricePerPlayer,
    string SkillLevel,
    string? HostName,
    string? Title,
    string? Description,
    bool IsActive
);

public record JoinOpenPlayRequest(
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string? Notes
);

public record OpenPlayCourtDto(
    string Id,
    string Name
);

public record OpenPlaySessionDto(
    string Id,
    string CourtId,                 // primary court
    string CourtName,               // primary court name
    List<OpenPlayCourtDto> Courts,  // ✅ NEW — all courts
    string Date,
    string StartTime,
    string EndTime,
    int MaxPlayers,
    int CurrentPlayers,
    int SpotsLeft,
    decimal PricePerPlayer,
    string SkillLevel,
    string? HostName,
    string? Title,
    string? Description,
    string Status,
    bool IsActive,
    string CreatedAt
);

public record OpenPlaySessionStatsDto(
    string Id,
    int TotalPlayers,
    int MaxPlayers,
    decimal TotalRevenue,
    decimal PendingRevenue,
    int ConfirmedCount,
    int PendingCount
);

public record OpenPlayPlayerDto(
    string BookingId,
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string ReferenceCode,
    string Status,
    string PaymentMethod,
    decimal AmountPaid,
    string JoinedAt
);

public record PublicOpenPlayPlayerDto(
    string BookingId,
    string DisplayName,
    string Status,
    string JoinedAt
);