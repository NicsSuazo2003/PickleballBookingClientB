using PickleballBookingSystem.DTOs;

namespace PickleballBookingSystem.Interfaces;

public interface ICourtService
{
    // ✅ NEW - Multi-court methods with client filter
    Task<List<CourtDto>> GetAllCourtsAsync(Guid clientId);
    Task<CourtDto> GetCourtByIdAsync(Guid id, Guid clientId);
    Task<CourtDto> CreateCourtAsync(CreateCourtRequest request, Guid clientId);
    Task<CourtDto> UpdateCourtAsync(Guid id, UpdateCourtRequest request, Guid clientId);
    Task DeleteCourtAsync(Guid id, Guid clientId);

    // ✅ UPDATED - Get availability for specific court with client filter
    Task<List<TimeSlotAvailabilityDto>> GetCourtAvailabilityAsync(Guid courtId, DateTime date, Guid clientId);

    // ✅ UPDATED - Blocked dates with court filter and client filter
    Task<List<BlockedDateDto>> GetBlockedDatesAsync(Guid? courtId, Guid clientId);
    Task<BlockedDateDto> AddBlockedDateAsync(CreateBlockedDateRequest request, Guid? courtId, Guid clientId);
    Task DeleteBlockedDateAsync(Guid id, Guid clientId);

    // ⚠️ DEPRECATED - Single court methods (keep for backward compatibility)
    Task<CourtDto> GetCourtAsync();
    Task<List<TimeSlotAvailabilityDto>> GetAvailabilityAsync(DateTime date);
    Task<CourtDto> UpdateCourtSettingsAsync(UpdateCourtRequest request);

    // Price Rules with client filter
    Task<List<PriceRuleDto>> GetPriceRulesAsync(Guid clientId);
    Task<PriceRuleDto> CreatePriceRuleAsync(CreatePriceRuleRequest request, Guid clientId);
    Task<PriceRuleDto> UpdatePriceRuleAsync(Guid id, UpdatePriceRuleRequest request, Guid clientId);
    Task DeletePriceRuleAsync(Guid id, Guid clientId);
}