namespace PickleballBookingSystem.DTOs;

public record PriceRuleDto(
    string Id,
    string Name,
    string DayOfWeek,
    string StartTime,
    string EndTime,
    decimal PricePerHour,
    bool IsActive,
    int Priority
);

public record CreatePriceRuleRequest(
    string Name,
    string DayOfWeek,
    string StartTime,
    string EndTime,
    decimal PricePerHour,
    int Priority
);

public record UpdatePriceRuleRequest(
    string? Name,
    string? DayOfWeek,
    string? StartTime,
    string? EndTime,
    decimal? PricePerHour,
    bool? IsActive,
    int? Priority
);