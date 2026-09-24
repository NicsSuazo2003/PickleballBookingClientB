// Interfaces/IOpenPlayService.cs
using PickleballBookingSystem.DTOs;

namespace PickleballBookingSystem.Interfaces;

public interface IOpenPlayService
{
    // Public
    Task<List<OpenPlaySessionDto>> GetUpcomingSessionsAsync(Guid clientId);
    Task<OpenPlaySessionDto?> GetSessionByIdAsync(Guid id, Guid clientId);
    Task<BookingDto> JoinSessionAsync(Guid id, JoinOpenPlayRequest request, Guid clientId);
    Task<List<PublicOpenPlayPlayerDto>> GetPublicPlayersAsync(Guid id, Guid clientId);
    // Admin
    Task<List<OpenPlaySessionDto>> AdminGetAllSessionsAsync(Guid clientId);
    Task<OpenPlaySessionDto> AdminCreateSessionAsync(CreateOpenPlaySessionRequest request, Guid clientId);
    Task<OpenPlaySessionDto> AdminUpdateSessionAsync(Guid id, UpdateOpenPlaySessionRequest request, Guid clientId);
    Task AdminDeleteSessionAsync(Guid id, Guid clientId);
    Task<List<OpenPlayPlayerDto>> AdminGetPlayersAsync(Guid id, Guid clientId);
    Task<OpenPlaySessionStatsDto> AdminGetSessionStatsAsync(Guid id, Guid clientId);
}