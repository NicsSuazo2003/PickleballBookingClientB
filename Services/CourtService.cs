using Microsoft.EntityFrameworkCore;
using PickleballBookingSystem.Data;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Entities;
using PickleballBookingSystem.Interfaces;

namespace PickleballBookingSystem.Services;

public class CourtService : ICourtService
{
    private readonly AppDbContext _db;

    public CourtService(AppDbContext db) => _db = db;

    // ========================================
    // ✅ MULTI-COURT METHODS (WITH CLIENT FILTER)
    // ========================================

    public async Task<List<CourtDto>> GetAllCourtsAsync(Guid clientId)
    {
        return await _db.Courts
            .Where(c => c.ClientId == clientId)
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    public async Task<CourtDto> GetCourtByIdAsync(Guid id, Guid clientId)
    {
        var court = await _db.Courts
            .FirstOrDefaultAsync(c => c.Id == id && c.ClientId == clientId)
            ?? throw new KeyNotFoundException("Court not found");
        return MapToDto(court);
    }

    public async Task<CourtDto> CreateCourtAsync(CreateCourtRequest request, Guid clientId)
    {
        var court = new Court
        {
            Name = request.Name,
            Type = request.Type,
            Indoor = request.Indoor,
            PricePerHour = request.PricePerHour,
            PeakPricePerHour = request.PeakPricePerHour,  // ✅ ADDED
            Amenities = request.Amenities ?? new List<string>(),
            OpenTime = TimeOnly.Parse(request.OpenTime),
            CloseTime = TimeOnly.Parse(request.CloseTime),
            Dimensions = request.Dimensions ?? "",
            Surface = request.Surface ?? "",
            Status = "active",
            Rating = 0,
            ClientId = clientId
        };
        _db.Courts.Add(court);
        await _db.SaveChangesAsync();
        return MapToDto(court);
    }

    public async Task<CourtDto> UpdateCourtAsync(Guid id, UpdateCourtRequest request, Guid clientId)
    {
        var court = await _db.Courts
            .FirstOrDefaultAsync(c => c.Id == id && c.ClientId == clientId)
            ?? throw new KeyNotFoundException("Court not found");

        if (request.Name is not null) court.Name = request.Name;
        if (request.Type is not null) court.Type = request.Type;
        if (request.Indoor.HasValue) court.Indoor = request.Indoor.Value;
        if (request.PricePerHour.HasValue) court.PricePerHour = request.PricePerHour.Value;
        if (request.PeakPricePerHour.HasValue) court.PeakPricePerHour = request.PeakPricePerHour.Value;
        if (request.Description is not null) court.Description = request.Description;  // ✅ ADD THIS
        if (request.Amenities is not null) court.Amenities = request.Amenities;
        if (request.ImageUrl is not null) court.ImageUrl = request.ImageUrl;
        if (request.Images is not null) court.Images = request.Images;
        if (request.Status is not null) court.Status = request.Status;
        if (request.OpenTime is not null) court.OpenTime = TimeOnly.Parse(request.OpenTime);
        if (request.CloseTime is not null) court.CloseTime = TimeOnly.Parse(request.CloseTime);
        if (request.Dimensions is not null) court.Dimensions = request.Dimensions;
        if (request.Surface is not null) court.Surface = request.Surface;

        await _db.SaveChangesAsync();
        return MapToDto(court);
    }
    public async Task DeleteCourtAsync(Guid id, Guid clientId)
    {
        var court = await _db.Courts
            .FirstOrDefaultAsync(c => c.Id == id && c.ClientId == clientId)
            ?? throw new KeyNotFoundException("Court not found");
        _db.Courts.Remove(court);
        await _db.SaveChangesAsync();
    }

    // ========================================
    // ✅ AVAILABILITY (COURT-SPECIFIC WITH CLIENT)
    // ========================================

    public async Task<List<TimeSlotAvailabilityDto>> GetCourtAvailabilityAsync(Guid courtId, DateTime date, Guid clientId)
    {
        date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

        var court = await _db.Courts
            .FirstOrDefaultAsync(c => c.Id == courtId && c.ClientId == clientId)
            ?? throw new KeyNotFoundException("Court not found");

        var openHour = court.OpenTime.Hour;
        var closeHour = court.CloseTime.Hour;
        if (closeHour == 0) closeHour = 24;

        // ✅ Slots taken by regular bookings on THIS court
        var bookedTimes = await _db.TimeSlots
            .Where(s => s.Date.Date == date.Date && s.Booking.CourtId == courtId)
            .Join(_db.Bookings.Where(b =>
                    b.Status != "cancelled"
                    && b.Status != "expired"
                    && b.Status != "rejected"
                    && b.Status != "refunded"
                    && b.ClientId == clientId),
                s => s.BookingId, b => b.Id, (s, b) => s.StartTime)
            .ToListAsync();

        var blockedDates = await _db.BlockedDates
            .Where(b => b.Date.Date == date.Date && (b.CourtId == null || b.CourtId == courtId) && b.ClientId == clientId)
            .ToListAsync();

        var priceRules = await _db.PriceRules
            .Where(r => r.IsActive && r.ClientId == clientId)
            .OrderByDescending(r => r.Priority)
            .ToListAsync();

        // ✅ NEW — hours occupied by any active Open Play session using this court
        var openPlayWindows = await _db.OpenPlaySessions
            .Where(s => s.IsActive
                     && s.ClientId == clientId
                     && s.Date.Date == date.Date
                     && s.SessionCourts.Any(sc => sc.CourtId == courtId))
            .Select(s => new { s.StartTime, s.EndTime })
            .ToListAsync();

        var dayOfWeek = date.DayOfWeek.ToString();
        var isWeekend = date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;

        var bookedSet = bookedTimes.Select(t => $"{t.Hour:D2}:00").ToHashSet();

        var blockedSet = new HashSet<int>();
        foreach (var bd in blockedDates)
        {
            if (bd.StartTime == null)
                for (int h = openHour; h < closeHour; h++) blockedSet.Add(h);
            else
            {
                var endH = bd.EndTime?.Hour ?? closeHour;
                if (endH == 0) endH = 24;
                for (int h = bd.StartTime.Value.Hour; h < endH; h++) blockedSet.Add(h);
            }
        }

        var phTime = DateTime.UtcNow.AddHours(8);
        var isToday = date.Date == phTime.Date;

        var slots = new List<TimeSlotAvailabilityDto>();

        for (int h = openHour; h < closeHour; h++)
        {
            var slotTime = new TimeOnly(h % 24, 0);
            var startTime = $"{h % 24:D2}:00";
            var endTime = $"{(h + 1) % 24:D2}:00";

            var isPast = isToday && (h < phTime.Hour || (h == phTime.Hour && 0 < phTime.Minute));
            var isBooked = bookedSet.Contains(startTime);
            var isBlocked = blockedSet.Contains(h);

            // ✅ NEW — blocked by an active Open Play session window
            var isOpenPlayBlocked = openPlayWindows.Any(w =>
                w.StartTime.Hour <= h && h < (w.EndTime.Hour == 0 ? 24 : w.EndTime.Hour));

            var slotPrice = court.PricePerHour;
            foreach (var rule in priceRules)
            {
                var dayMatch = rule.DayOfWeek == "All" || rule.DayOfWeek == dayOfWeek ||
                               (rule.DayOfWeek == "Weekend" && isWeekend) ||
                               (rule.DayOfWeek == "Weekday" && !isWeekend);
                if (dayMatch && slotTime >= rule.StartTime && slotTime < rule.EndTime)
                {
                    slotPrice = rule.PricePerHour;
                    break;
                }
            }

            slots.Add(new TimeSlotAvailabilityDto(
                $"slot-{courtId}-{date:yyyy-MM-dd}-{h}",
                date.ToString("yyyy-MM-dd"),
                startTime, endTime,
                !isPast && !isBooked && !isBlocked && !isOpenPlayBlocked,
                slotPrice));
        }

        return slots;
    }

    // ========================================
    // ✅ BLOCKED DATES (CLIENT-SPECIFIC)
    // ========================================

    public async Task<List<BlockedDateDto>> GetBlockedDatesAsync(Guid? courtId, Guid clientId)
    {
        var query = _db.BlockedDates.Where(b => b.ClientId == clientId);

        if (courtId.HasValue)
            query = query.Where(b => b.CourtId == courtId || b.CourtId == null);

        return await query
            .OrderBy(b => b.Date)
            .Select(b => new BlockedDateDto(
                b.Id.ToString(),
                b.Date.ToString("yyyy-MM-dd"),
                b.StartTime.HasValue ? b.StartTime.Value.ToString("HH:mm") : null,
                b.EndTime.HasValue ? b.EndTime.Value.ToString("HH:mm") : null,
                b.Reason
            ))
            .ToListAsync();
    }

    public async Task<BlockedDateDto> AddBlockedDateAsync(CreateBlockedDateRequest request, Guid? courtId, Guid clientId)
    {
        var blocked = new BlockedDate
        {
            CourtId = courtId,
            ClientId = clientId,
            Date = DateTime.SpecifyKind(DateTime.Parse(request.Date).Date, DateTimeKind.Utc),
            StartTime = request.StartTime != null ? TimeOnly.Parse(request.StartTime) : null,
            EndTime = request.EndTime != null ? TimeOnly.Parse(request.EndTime) : null,
            Reason = request.Reason
        };
        _db.BlockedDates.Add(blocked);
        await _db.SaveChangesAsync();
        return new BlockedDateDto(
            blocked.Id.ToString(),
            blocked.Date.ToString("yyyy-MM-dd"),
            request.StartTime,
            request.EndTime,
            request.Reason
        );
    }

    public async Task DeleteBlockedDateAsync(Guid id, Guid clientId)
    {
        var blocked = await _db.BlockedDates
            .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == clientId)
            ?? throw new KeyNotFoundException("Blocked date not found");
        _db.BlockedDates.Remove(blocked);
        await _db.SaveChangesAsync();
    }

    // ========================================
    // ⚠️ DEPRECATED - Single Court Methods (Backward Compatibility)
    // ========================================

    public async Task<CourtDto> GetCourtAsync()
    {
        var court = await _db.Courts.FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Court not found");
        return MapToDto(court);
    }

    public async Task<List<TimeSlotAvailabilityDto>> GetAvailabilityAsync(DateTime date)
    {
        var court = await _db.Courts.FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Court not found");
        return await GetCourtAvailabilityAsync(court.Id, date, court.ClientId);
    }

    public async Task<CourtDto> UpdateCourtSettingsAsync(UpdateCourtRequest request)
    {
        var court = await _db.Courts.FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Court not found");
        return await UpdateCourtAsync(court.Id, request, court.ClientId);
    }

    // ========================================
    // ✅ PRICE RULES (CLIENT-SPECIFIC)
    // ========================================

    public async Task<List<PriceRuleDto>> GetPriceRulesAsync(Guid clientId)
    {
        return await _db.PriceRules
            .Where(r => r.ClientId == clientId && r.IsActive)
            .OrderBy(r => r.Priority)
            .ThenBy(r => r.DayOfWeek)
            .Select(r => new PriceRuleDto(
                r.Id.ToString(),
                r.Name,
                r.DayOfWeek,
                r.StartTime.ToString("HH:mm"),
                r.EndTime.ToString("HH:mm"),
                r.PricePerHour,
                r.IsActive,
                r.Priority
            ))
            .ToListAsync();
    }

    public async Task<PriceRuleDto> CreatePriceRuleAsync(CreatePriceRuleRequest request, Guid clientId)
    {
        var rule = new PriceRule
        {
            Name = request.Name,
            DayOfWeek = request.DayOfWeek,
            StartTime = TimeOnly.Parse(request.StartTime),
            EndTime = TimeOnly.Parse(request.EndTime),
            PricePerHour = request.PricePerHour,
            Priority = request.Priority,
            ClientId = clientId
        };
        _db.PriceRules.Add(rule);
        await _db.SaveChangesAsync();
        return MapPriceRuleToDto(rule);
    }

    public async Task<PriceRuleDto> UpdatePriceRuleAsync(Guid id, UpdatePriceRuleRequest request, Guid clientId)
    {
        var rule = await _db.PriceRules
            .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == clientId)
            ?? throw new KeyNotFoundException("Price rule not found");

        if (request.Name is not null) rule.Name = request.Name;
        if (request.DayOfWeek is not null) rule.DayOfWeek = request.DayOfWeek;
        if (request.StartTime is not null) rule.StartTime = TimeOnly.Parse(request.StartTime);
        if (request.EndTime is not null) rule.EndTime = TimeOnly.Parse(request.EndTime);
        if (request.PricePerHour.HasValue) rule.PricePerHour = request.PricePerHour.Value;
        if (request.IsActive.HasValue) rule.IsActive = request.IsActive.Value;
        if (request.Priority.HasValue) rule.Priority = request.Priority.Value;
        await _db.SaveChangesAsync();
        return MapPriceRuleToDto(rule);
    }

    public async Task DeletePriceRuleAsync(Guid id, Guid clientId)
    {
        var rule = await _db.PriceRules
            .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == clientId)
            ?? throw new KeyNotFoundException("Price rule not found");
        _db.PriceRules.Remove(rule);
        await _db.SaveChangesAsync();
    }

    // ========================================
    // ✅ PRIVATE HELPERS
    // ========================================

    private static PriceRuleDto MapPriceRuleToDto(PriceRule r) => new(
        r.Id.ToString(),
        r.Name,
        r.DayOfWeek,
        r.StartTime.ToString("HH:mm"),
        r.EndTime.ToString("HH:mm"),
        r.PricePerHour,
        r.IsActive,
        r.Priority
    );

    private static CourtDto MapToDto(Court c) => new(
        c.Id.ToString(),
        c.Name,
        c.Type,
        c.Indoor,
        c.PricePerHour,
        c.PeakPricePerHour,  // ✅ ADDED
        c.Description,
        c.Amenities,
        c.Rating,
        c.ImageUrl,
        c.Images,
        c.Status,
        c.OpenTime.ToString("HH:mm"),
        c.CloseTime.ToString("HH:mm"),
        c.Dimensions,
        c.Surface
    );
}