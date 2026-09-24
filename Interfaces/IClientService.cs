using PickleballBookingSystem.DTOs;

namespace PickleballBookingSystem.Interfaces;

public interface IClientService
{
    Task<ClientDto> GetClientBySubdomainAsync(string subdomain);
    Task<Guid> GetClientIdBySubdomainAsync(string subdomain);
    Task<ClientDto> UpdateClientSettingsAsync(Guid clientId, UpdateClientSettingsRequest request); // ✅ NEW
}

// ✅ Add PaymentMethods to ClientDto
public record ClientDto(
    string Id,
    string Name,
    string Subdomain,
    string? LogoUrl,
    string PrimaryColor,
    string AccentColor,
    string? GcashNumber,
    string? GcashAccountName,
    object? PaymentMethods = null // ✅ ADD THIS
);