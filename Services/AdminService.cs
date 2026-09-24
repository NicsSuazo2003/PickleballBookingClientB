using Microsoft.EntityFrameworkCore;
using PickleballBookingSystem.Data;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Entities;
using PickleballBookingSystem.Interfaces;
using BCrypt.Net;

namespace PickleballBookingSystem.Services;

public class AdminService : IAdminService
{
    private readonly AppDbContext _db;

    public AdminService(AppDbContext db) => _db = db;

    // ========================================
    // ANALYTICS
    // ========================================

    public async Task<AnalyticsDto> GetAnalyticsAsync(Guid clientId)
    {
        var bookings = await _db.Bookings
            .Where(b => b.ClientId == clientId)
            .ToListAsync();

        var confirmedOrCompleted = bookings
            .Where(b => b.Status == "confirmed" || b.Status == "completed")
            .ToList();

        var totalRevenue = confirmedOrCompleted.Sum(b => b.TotalAmount);
        var totalBookings = bookings.Count;
        var activeUsers = bookings.Select(b => b.CustomerEmail).Distinct().Count();

        var confirmedBookings = bookings.Count(b => b.Status == "confirmed");
        var pendingPayments = bookings.Count(b => b.Status == "pending_payment" || b.Status == "payment_submitted");
        var completedBookings = bookings.Count(b => b.Status == "completed");
        var cancelledBookings = bookings.Count(b => b.Status == "cancelled" || b.Status == "rejected");

        var statusBreakdown = bookings
            .GroupBy(b => b.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        var revenueByDay = confirmedOrCompleted
            .GroupBy(b => b.Date.Date)
            .Select(g => new RevenueByDayDto(g.Key.ToString("yyyy-MM-dd"), g.Sum(b => b.TotalAmount)))
            .OrderBy(r => r.Date).TakeLast(30).ToList();

        var bookingsByDay = bookings
            .GroupBy(b => b.Date.Date)
            .Select(g => new BookingsByDayDto(g.Key.ToString("yyyy-MM-dd"), g.Count()))
            .OrderBy(b => b.Date).TakeLast(30).ToList();

        return new AnalyticsDto(
            totalRevenue,
            totalBookings,
            activeUsers,
            confirmedBookings,
            pendingPayments,
            completedBookings,
            cancelledBookings,
            statusBreakdown,
            revenueByDay,
            bookingsByDay,
            12.5,
            8.3,
            5.1
        );
    }

    public async Task<List<CourtAnalyticsDto>> GetCourtAnalyticsAsync(Guid clientId)
    {
        var courts = await _db.Courts
            .Where(c => c.ClientId == clientId)
            .ToListAsync();

        var result = new List<CourtAnalyticsDto>();

        foreach (var court in courts)
        {
            var bookings = await _db.Bookings
                .Where(b => b.CourtId == court.Id && b.ClientId == clientId)
                .ToListAsync();

            var totalBookings = bookings.Count;
            var totalRevenue = bookings
                .Where(b => b.Status == "confirmed" || b.Status == "completed")
                .Sum(b => b.TotalAmount);
            var confirmed = bookings.Count(b => b.Status == "confirmed" || b.Status == "completed");
            var pending = bookings.Count(b => b.Status == "pending_payment" || b.Status == "payment_submitted");

            var totalSlots = bookings.Sum(b => b.Slots.Count);
            var totalPossibleSlots = 12 * 30;
            var utilizationRate = totalPossibleSlots > 0
                ? Math.Round((double)totalSlots / totalPossibleSlots * 100, 1)
                : 0;

            result.Add(new CourtAnalyticsDto(
                court.Id.ToString(),
                court.Name,
                totalBookings,
                totalRevenue,
                confirmed,
                pending,
                utilizationRate
            ));
        }

        return result;
    }

    // ========================================
    // ✅ STAFF MANAGEMENT
    // ========================================

    public async Task<List<UserDto>> GetStaffByClientAsync(Guid clientId)
    {
        var staff = await _db.Users
            .Where(u => u.ClientId == clientId && u.Role == "staff")
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return staff.Select(MapToUserDto).ToList();
    }

    public async Task<UserDto> CreateStaffAsync(CreateStaffRequest request, Guid clientId)
    {
        // Check if user already exists for this client
        if (await _db.Users.AnyAsync(u => u.Email == request.Email && u.ClientId == clientId))
            throw new InvalidOperationException("A user with this email already exists for this client.");

        // Check if user exists globally (optional)
        if (await _db.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException("This email is already registered.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Email = request.Email,
            Phone = request.Phone ?? string.Empty,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = "staff",
            ClientId = clientId,
            Status = "active",
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return MapToUserDto(user);
    }

    public async Task UpdateStaffStatusAsync(Guid userId, string status, Guid clientId)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.ClientId == clientId && u.Role == "staff")
            ?? throw new KeyNotFoundException("Staff member not found");

        user.Status = status;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteStaffAsync(Guid userId, Guid clientId)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == userId && u.ClientId == clientId && u.Role == "staff")
            ?? throw new KeyNotFoundException("Staff member not found");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();
    }

    // ========================================
    // PRIVATE HELPERS
    // ========================================

    private static UserDto MapToUserDto(User u) => new(
        u.Id.ToString(),
        u.Name,
        u.Email,
        u.Phone ?? string.Empty,
        u.Role,
        u.Avatar,
        u.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        u.BookingsCount,
        u.Status
    );
}