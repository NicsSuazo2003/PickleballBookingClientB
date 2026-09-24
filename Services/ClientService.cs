// Services/ClientService.cs
using Microsoft.EntityFrameworkCore;
using PickleballBookingSystem.Data;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Interfaces;
using System.Text.Json;

namespace PickleballBookingSystem.Services;

public class ClientService : IClientService
{
    private readonly AppDbContext _db;

    public ClientService(AppDbContext db) => _db = db;

    public async Task<ClientDto> GetClientBySubdomainAsync(string subdomain)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Subdomain == subdomain && c.Status == "active")
            ?? throw new KeyNotFoundException($"Client with subdomain '{subdomain}' not found");

        return new ClientDto(
            client.Id.ToString(),
            client.Name,
            client.Subdomain,
            client.LogoUrl,
            client.PrimaryColor,
            client.AccentColor,
            client.GcashNumber,
            client.GcashAccountName,
            // ✅ Parse and return PaymentMethods
            !string.IsNullOrEmpty(client.PaymentMethods)
                ? JsonSerializer.Deserialize<object>(client.PaymentMethods)
                : null
        );
    }

    public async Task<Guid> GetClientIdBySubdomainAsync(string subdomain)
    {
        var client = await _db.Clients
            .FirstOrDefaultAsync(c => c.Subdomain == subdomain && c.Status == "active")
            ?? throw new KeyNotFoundException($"Client with subdomain '{subdomain}' not found");

        return client.Id;
    }

    public async Task<ClientDto> UpdateClientSettingsAsync(Guid clientId, UpdateClientSettingsRequest request)
    {
        var client = await _db.Clients.FindAsync(clientId)
            ?? throw new KeyNotFoundException("Client not found");

        if (request.Name is not null) client.Name = request.Name;
        if (request.GcashNumber is not null) client.GcashNumber = request.GcashNumber;
        if (request.GcashAccountName is not null) client.GcashAccountName = request.GcashAccountName;

        // ✅ Save payment methods as JSON string (EF will handle jsonb conversion)
        if (request.PaymentMethods is not null)
        {
            client.PaymentMethods = JsonSerializer.Serialize(request.PaymentMethods);
        }

        await _db.SaveChangesAsync();

        return new ClientDto(
            client.Id.ToString(),
            client.Name,
            client.Subdomain,
            client.LogoUrl,
            client.PrimaryColor,
            client.AccentColor,
            client.GcashNumber,
            client.GcashAccountName,
            // ✅ Return updated PaymentMethods
            !string.IsNullOrEmpty(client.PaymentMethods)
                ? JsonSerializer.Deserialize<object>(client.PaymentMethods)
                : null
        );
    }
}