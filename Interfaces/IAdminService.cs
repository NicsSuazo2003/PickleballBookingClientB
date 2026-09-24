using PickleballBookingSystem.DTOs;

namespace PickleballBookingSystem.Interfaces;

public interface IAdminService
{
    // ✅ UPDATED - Get analytics with client filter
    Task<AnalyticsDto> GetAnalyticsAsync(Guid clientId);

    // ✅ UPDATED - Get court analytics with client filter
    Task<List<CourtAnalyticsDto>> GetCourtAnalyticsAsync(Guid clientId);
    Task<List<UserDto>> GetStaffByClientAsync(Guid clientId);
    Task<UserDto> CreateStaffAsync(CreateStaffRequest request, Guid clientId);
    Task UpdateStaffStatusAsync(Guid userId, string status, Guid clientId);
    Task DeleteStaffAsync(Guid userId, Guid clientId);
}   