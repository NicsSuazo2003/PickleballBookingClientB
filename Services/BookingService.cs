// Services/BookingService.cs
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PickleballBookingSystem.Data;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Entities;
using PickleballBookingSystem.Interfaces;

namespace PickleballBookingSystem.Services;

public class BookingService : IBookingService
{
    private readonly AppDbContext _db;
    private readonly EmailService _email;
    private readonly ISecurityLogger _security;

    // ✅ SUSPICIOUS — patterns for rejecting obvious test bookings
    private static readonly HashSet<string> SuspiciousNamePatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "test", "testing", "sample", "johndoe", "john doe", "asdf", "qwerty", "dummy", "fake"
    };

    private static readonly HashSet<string> SuspiciousEmailLocalParts = new(StringComparer.OrdinalIgnoreCase)
    {
        "test", "testing", "sample", "johndoe", "asdf", "qwerty", "dummy", "fake"
    };

    private static readonly string[] DisposableEmailDomains = new[]
    {
        "@mailinator.com", "@tempmail.com", "@guerrillamail.com",
        "@10minutemail.com", "@throwaway.email"
    };

    private static readonly string[] SuspiciousNotesKeywords = new[]
    {
        "trap", "test", "fake", "hack", "sql", "injection"
    };

    public BookingService(AppDbContext db, EmailService email, ISecurityLogger security)
    {
        _db = db;
        _email = email;
        _security = security;
    }

    public async Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, Guid clientId)
    {
        if (!Guid.TryParse(request.CourtId, out var courtGuid))
            throw new InvalidOperationException("Invalid court ID format");

        var court = await _db.Courts
            .FirstOrDefaultAsync(c => c.Id == courtGuid && c.ClientId == clientId)
            ?? throw new KeyNotFoundException("Court not found");

        if (!DateTime.TryParse(request.Date, out var bookingDate))
            throw new InvalidOperationException("Invalid date format");

        bookingDate = DateTime.SpecifyKind(bookingDate.Date, DateTimeKind.Utc);

        if (request.Slots == null || !request.Slots.Any())
            throw new InvalidOperationException("At least one time slot is required");

        foreach (var slot in request.Slots)
        {
            if (!TimeOnly.TryParse(slot.StartTime, out var startTime))
                throw new InvalidOperationException($"Invalid start time: {slot.StartTime}");
            if (!TimeOnly.TryParse(slot.EndTime, out var endTime))
                throw new InvalidOperationException($"Invalid end time: {slot.EndTime}");

            var conflicting = await _db.Bookings
                .Where(b => b.CourtId == courtGuid
                    && b.Date == bookingDate
                    && b.Status != "cancelled"
                    && b.Status != "expired"
                    && b.Status != "rejected"
                    && b.Status != "refunded")
                .SelectMany(b => b.Slots)
                .Where(s => s.Date == bookingDate
                    && s.StartTime < endTime
                    && s.EndTime > startTime)
                .AnyAsync();

            if (conflicting)
                throw new InvalidOperationException($"Time slot {slot.StartTime}-{slot.EndTime} is already booked");
        }

        var firstSlot = request.Slots.First();
        if (!TimeOnly.TryParse(firstSlot.StartTime, out var firstStartTime))
            throw new InvalidOperationException($"Invalid start time: {firstSlot.StartTime}");
        if (!TimeOnly.TryParse(firstSlot.EndTime, out var firstEndTime))
            throw new InvalidOperationException($"Invalid end time: {firstSlot.EndTime}");

        var isBlocked = await _db.BlockedDates
            .AnyAsync(bd => bd.CourtId == courtGuid
                && bd.Date == bookingDate
                && (bd.StartTime == null || bd.StartTime <= firstStartTime)
                && (bd.EndTime == null || bd.EndTime >= firstEndTime));

        if (isBlocked)
            throw new InvalidOperationException("This time slot is blocked");

        // ✅ SUSPICIOUS — reject obviously-fake test bookings at creation
        if (LooksLikeTest(request.CustomerName, request.CustomerEmail))
        {
            await _security.LogAsync(new SecurityEvent
            {
                ClientId = clientId,
                EventType = "booking.suspicious_rejected",
                Severity = "warning",
                Message = $"Rejected suspicious booking: name='{request.CustomerName}', email='{request.CustomerEmail}'",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    name = request.CustomerName,
                    email = request.CustomerEmail,
                    phone = request.CustomerPhone,
                    courtId = courtGuid,
                    date = bookingDate.ToString("yyyy-MM-dd"),
                    totalAmount = request.TotalAmount,
                    notes = request.Notes,
                }),
                Success = false,
                ErrorCode = "SUSPICIOUS_CUSTOMER",
            });

            throw new InvalidOperationException(
                "This booking looks like a test. Please use your real name and email. " +
                "If this is a mistake, contact us and we'll help.");
        }

        // ✅ SUSPICIOUS — soft signal: log notes with test/trap/fake keywords, but don't reject
        if (NotesMentionSuspiciousKeywords(request.Notes))
        {
            await _security.LogAsync(new SecurityEvent
            {
                ClientId = clientId,
                EventType = "booking.suspicious_notes",
                Severity = "warning",
                Message = "Booking notes contain suspicious keywords",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    notes = request.Notes,
                    email = request.CustomerEmail,
                    name = request.CustomerName,
                }),
                Success = true,
            });
        }

        // ✅ one-pending-per-email guard (scope A)
        var normalizedEmail = request.CustomerEmail.Trim().ToLowerInvariant();
        var existingPending = await _db.Bookings
            .AnyAsync(b => b.ClientId == clientId
                && b.CourtId == courtGuid
                && b.Date == bookingDate
                && b.CustomerEmail == normalizedEmail
                && b.Status == "pending_payment"
                && b.PaymentExpiresAt != null
                && b.PaymentExpiresAt > DateTime.UtcNow);

        if (existingPending)
            throw new InvalidOperationException(
                "You already have a pending booking for this court on this date. " +
                "Please complete or wait for it to expire before booking again.");

        var referenceCode = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        var booking = new Booking
        {
            ClientId = clientId,
            CourtId = courtGuid,
            CustomerName = request.CustomerName.Trim(),
            CustomerEmail = normalizedEmail,
            CustomerPhone = request.CustomerPhone?.Trim(),
            ReferenceCode = referenceCode,
            Date = bookingDate,
            TotalAmount = request.TotalAmount,
            Status = "pending_payment",
            PaymentMethod = "gcash",
            Notes = request.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow,
            PaymentExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Slots = request.Slots.Select(s => new TimeSlot
            {
                CourtId = courtGuid,
                Date = bookingDate,
                StartTime = TimeOnly.Parse(s.StartTime),
                EndTime = TimeOnly.Parse(s.EndTime),
                Price = request.TotalAmount / request.Slots.Count
            }).ToList()
        };

        _db.Bookings.Add(booking);

        // ✅ serialization retry on ReferenceCode collision
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsReferenceCodeCollision(ex))
        {
            _db.Entry(booking).State = EntityState.Detached;

            booking.ReferenceCode = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            _db.Bookings.Add(booking);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException retryEx)
            {
                _db.Entry(booking).State = EntityState.Detached;
                throw new InvalidOperationException(
                    "We couldn't complete your booking due to a temporary error. Please try again.", retryEx);
            }
        }

        // ✅ audit
        await _security.LogAsync(new SecurityEvent
        {
            ClientId = clientId,
            EventType = "booking.created",
            Severity = "info",
            Message = $"Booking created: {booking.ReferenceCode}",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                referenceCode = booking.ReferenceCode,
                courtId = courtGuid,
                date = bookingDate.ToString("yyyy-MM-dd"),
                amount = booking.TotalAmount,
                email = normalizedEmail,
            }),
            Success = true,
        });

        try
        {
            await _email.NotifyAdminNewBookingAsync(
                booking.CustomerName,
                booking.ReferenceCode,
                booking.Date.ToString("yyyy-MM-dd"),
                string.Join(", ", booking.Slots.Select(s => $"{s.StartTime:HH:mm}-{s.EndTime:HH:mm}")),
                $"₱{booking.TotalAmount:N2}");
        }
        catch { }

        return MapToDto(booking, court.Name);
    }

    // ✅ SUSPICIOUS — helper: does this name + email look like an obvious test?
    private static bool LooksLikeTest(string? name, string? email)
    {
        var n = (name ?? string.Empty).Trim().ToLowerInvariant();
        var e = (email ?? string.Empty).Trim().ToLowerInvariant();

        if (string.IsNullOrEmpty(n) && string.IsNullOrEmpty(e)) return false;

        // Name: exact match, or any single token matches a suspicious pattern
        if (!string.IsNullOrEmpty(n))
        {
            if (SuspiciousNamePatterns.Contains(n)) return true;
            var nameTokens = n.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (nameTokens.Any(t => SuspiciousNamePatterns.Contains(t))) return true;
        }

        // Email: local-part exact match, or disposable domain
        if (!string.IsNullOrEmpty(e))
        {
            var local = e.Split('@')[0];
            if (SuspiciousEmailLocalParts.Contains(local)) return true;
            if (DisposableEmailDomains.Any(d => e.EndsWith(d, StringComparison.OrdinalIgnoreCase))) return true;
        }

        return false;
    }

    // ✅ SUSPICIOUS — helper: do the notes contain keywords we want to log?
    private static bool NotesMentionSuspiciousKeywords(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return false;
        var n = notes.ToLowerInvariant();
        return SuspiciousNotesKeywords.Any(k => n.Contains(k));
    }

    private static bool IsReferenceCodeCollision(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException pg
            && pg.SqlState == PostgresErrorCodes.UniqueViolation
            && pg.ConstraintName == "IX_bookings_ReferenceCode";
    }

    public async Task<BookingDto> GetBookingAsync(Guid id, Guid clientId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Slots).Include(b => b.Court)
            .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == clientId)
            ?? throw new KeyNotFoundException("Booking not found");
        return MapToDto(booking, booking.Court?.Name ?? "");
    }

    public async Task<BookingDto> GetBookingByReferenceAsync(string referenceCode, Guid clientId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Slots).Include(b => b.Court)
            .FirstOrDefaultAsync(b => b.ReferenceCode == referenceCode && b.ClientId == clientId)
            ?? throw new KeyNotFoundException("Booking not found");
        return MapToDto(booking, booking.Court?.Name ?? "");
    }

    public async Task<BookingDto> TrackBookingAsync(string referenceCode, string? email, Guid clientId)
    {
        var query = _db.Bookings
            .Include(b => b.Slots).Include(b => b.Court)
            .Where(b => b.ReferenceCode == referenceCode && b.ClientId == clientId);

        if (!string.IsNullOrEmpty(email))
            query = query.Where(b => b.CustomerEmail == email);

        var booking = await query.FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException("Booking not found");
        return MapToDto(booking, booking.Court?.Name ?? "");
    }

    public async Task<List<BookingDto>> GetBookingsAsync(Guid clientId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _db.Bookings.Include(b => b.Slots).Include(b => b.Court)
            .Where(b => b.ClientId == clientId);

        if (fromDate.HasValue) query = query.Where(b => b.Date >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(b => b.Date <= toDate.Value);

        var bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
        return bookings.Select(b => MapToDto(b, b.Court?.Name ?? "")).ToList();
    }

    public async Task<List<BookingDto>> GetAllBookingsAsync(Guid clientId)
    {
        var bookings = await _db.Bookings
            .Include(b => b.Slots).Include(b => b.Court)
            .Where(b => b.ClientId == clientId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
        return bookings.Select(b => MapToDto(b, b.Court?.Name ?? "")).ToList();
    }

    public async Task<BookingDto> UpdateBookingStatusAsync(Guid id, string status, Guid clientId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Slots).Include(b => b.Court)
            .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == clientId)
            ?? throw new KeyNotFoundException("Booking not found");

        var previousStatus = booking.Status;
        booking.Status = status;
        await _db.SaveChangesAsync();
        await SendStatusChangeEmailAsync(booking, previousStatus);
        return MapToDto(booking, booking.Court?.Name ?? "");
    }

    public async Task<BookingDto> AdminUpdateBookingAsync(Guid id, AdminUpdateBookingRequest request, Guid clientId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Slots).Include(b => b.Court)
            .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == clientId)
            ?? throw new KeyNotFoundException("Booking not found");

        var previousStatus = booking.Status;
        booking.Status = request.Status;
        await _db.SaveChangesAsync();
        await SendStatusChangeEmailAsync(booking, previousStatus);
        return MapToDto(booking, booking.Court?.Name ?? "");
    }

    public async Task<BookingDto> UploadPaymentAsync(Guid id, string screenshotBase64, string? paymentReference, Guid clientId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Slots).Include(b => b.Court)
            .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == clientId)
            ?? throw new KeyNotFoundException("Booking not found");

        if (booking.Status != "pending_payment")
            throw new InvalidOperationException("Booking is not pending payment");

        booking.PaymentScreenshot = screenshotBase64;
        booking.PaymentReference = paymentReference;
        booking.Status = "payment_submitted";
        await _db.SaveChangesAsync();
        return MapToDto(booking, booking.Court?.Name ?? "");
    }

    public async Task<BookingDto> UploadPaymentScreenshotAsync(
        Guid id, string? screenshotUrl, string? paymentReference, string? paymentMethod, Guid clientId)
    {
        var booking = await _db.Bookings
            .Include(b => b.Slots).Include(b => b.Court)
            .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == clientId)
            ?? throw new KeyNotFoundException("Booking not found");

        if (booking.Status != "pending_payment")
            throw new InvalidOperationException("Booking is not pending payment");

        // ✅ SUSPICIOUS — reject payment upload on bookings whose identity looks fake
        if (LooksLikeTest(booking.CustomerName, booking.CustomerEmail))
        {
            await _security.LogAsync(new SecurityEvent
            {
                ClientId = clientId,
                EventType = "booking.suspicious_payment_upload",
                Severity = "critical",
                Message = $"Blocked payment upload on suspicious booking {booking.ReferenceCode}",
                Metadata = System.Text.Json.JsonSerializer.Serialize(new
                {
                    referenceCode = booking.ReferenceCode,
                    email = booking.CustomerEmail,
                    name = booking.CustomerName,
                    paymentReference = paymentReference,
                    paymentMethod = paymentMethod,
                }),
                Success = false,
                ErrorCode = "SUSPICIOUS_BOOKING",
            });

            throw new InvalidOperationException(
                "This booking can't submit a payment. Contact us if this is a mistake.");
        }

        if (!string.IsNullOrEmpty(screenshotUrl))
            booking.PaymentScreenshot = screenshotUrl;

        booking.PaymentReference = paymentReference;

        if (!string.IsNullOrWhiteSpace(paymentMethod))
            booking.PaymentMethod = paymentMethod;

        booking.Status = "payment_submitted";
        await _db.SaveChangesAsync();
        return MapToDto(booking, booking.Court?.Name ?? "");
    }

    public async Task ConfirmPaymentAsync(Guid id, Guid clientId)
    {
        var booking = await _db.Bookings.Include(b => b.Slots)
            .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == clientId)
            ?? throw new KeyNotFoundException("Booking not found");

        var previousStatus = booking.Status;
        booking.Status = "confirmed";
        await _db.SaveChangesAsync();
        await SendStatusChangeEmailAsync(booking, previousStatus);
    }

    public async Task CancelBookingAsync(Guid id, Guid clientId)
    {
        var booking = await _db.Bookings
            .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == clientId)
            ?? throw new KeyNotFoundException("Booking not found");

        booking.Status = "cancelled";
        await _db.SaveChangesAsync();
    }

    public async Task AutoCompletePastBookingsAsync(Guid clientId)
    {
        var pastBookings = await _db.Bookings
            .Where(b => b.ClientId == clientId
                && b.Date < DateTime.UtcNow.Date
                && b.Status != "completed"
                && b.Status != "cancelled"
                && b.Status != "rejected"
                && b.Status != "refunded"
                && b.Status != "expired")
            .ToListAsync();

        foreach (var booking in pastBookings) booking.Status = "completed";
        await _db.SaveChangesAsync();
    }

    public async Task CancelExpiredPaymentsAsync(Guid clientId)
    {
        var expiredBookings = await _db.Bookings
            .Where(b => b.ClientId == clientId
                && b.Status == "pending_payment"
                && b.PaymentExpiresAt != null
                && b.PaymentExpiresAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var booking in expiredBookings) booking.Status = "expired";
        await _db.SaveChangesAsync();
    }

    private async Task SendStatusChangeEmailAsync(Booking booking, string previousStatus)
    {
        if (previousStatus == booking.Status) return;

        var timeRange = string.Join(", ", booking.Slots
            .OrderBy(s => s.StartTime)
            .Select(s => $"{s.StartTime:HH:mm}-{s.EndTime:HH:mm}"));
        var dateStr = booking.Date.ToString("yyyy-MM-dd");
        var amountStr = $"₱{booking.TotalAmount:N2}";

        try
        {
            if (booking.Status == "confirmed")
                await _email.NotifyCustomerBookingConfirmedAsync(
                    booking.CustomerEmail, booking.CustomerName, booking.ReferenceCode, dateStr, timeRange, amountStr);
            else if (booking.Status == "rejected")
                await _email.NotifyCustomerBookingRejectedAsync(
                    booking.CustomerEmail, booking.CustomerName, booking.ReferenceCode, dateStr, timeRange, null, amountStr);
            else if (booking.Status == "cancelled")
                await _email.NotifyCustomerBookingCancelledAsync(
                    booking.CustomerEmail, booking.CustomerName, booking.ReferenceCode, dateStr, timeRange, null, amountStr);
            else if (booking.Status == "refunded" && booking.TotalAmount > 0)
                await _email.NotifyCustomerBookingRefundedAsync(
                    booking.CustomerEmail, booking.CustomerName, booking.ReferenceCode, dateStr, timeRange, amountStr);
        }
        catch { }
    }

    private static BookingDto MapToDto(Booking b, string courtName) => new(
        b.Id.ToString(), b.CourtId.ToString(), courtName,
        b.CustomerName, b.CustomerEmail, b.CustomerPhone,
        b.ReferenceCode, b.Date.ToString("yyyy-MM-dd"),
        b.Slots.Select(s => new TimeSlotDto(
            s.Id.ToString(), s.Date.ToString("yyyy-MM-dd"),
            s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"),
            false, s.Price)).ToList(),
        b.TotalAmount, b.Status, b.PaymentMethod,
        b.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        b.Notes, b.PaymentScreenshot, b.PaymentExpiresAt, b.PaymentReference);

    public async Task<RescheduleBookingResponse> RescheduleBookingAsync(
        Guid id, RescheduleBookingRequest request, Guid clientId)
    {
        if (request.Slots == null || !request.Slots.Any())
            throw new InvalidOperationException("At least one time slot is required");

        var booking = await _db.Bookings
            .Include(b => b.Slots).Include(b => b.Court)
            .FirstOrDefaultAsync(b => b.Id == id && b.ClientId == clientId)
            ?? throw new KeyNotFoundException("Booking not found");

        if (booking.Status is "cancelled" or "rejected" or "expired" or "refunded")
            throw new InvalidOperationException($"Cannot reschedule a booking that is already {booking.Status}");

        var targetCourtId = booking.CourtId;
        if (!string.IsNullOrWhiteSpace(request.CourtId))
        {
            if (!Guid.TryParse(request.CourtId, out var parsedCourt))
                throw new InvalidOperationException("Invalid court ID format");
            targetCourtId = parsedCourt;
        }

        var targetCourt = await _db.Courts
            .FirstOrDefaultAsync(c => c.Id == targetCourtId && c.ClientId == clientId)
            ?? throw new KeyNotFoundException("Target court not found");

        var targetDate = booking.Date;
        if (!string.IsNullOrWhiteSpace(request.Date))
        {
            if (!DateTime.TryParse(request.Date, out var parsedDate))
                throw new InvalidOperationException("Invalid date format");
            targetDate = DateTime.SpecifyKind(parsedDate.Date, DateTimeKind.Utc);
        }

        var newSlots = new List<(TimeOnly Start, TimeOnly End)>();
        foreach (var s in request.Slots)
        {
            if (!TimeOnly.TryParse(s.StartTime, out var st))
                throw new InvalidOperationException($"Invalid start time: {s.StartTime}");
            if (!TimeOnly.TryParse(s.EndTime, out var en))
                throw new InvalidOperationException($"Invalid end time: {s.EndTime}");
            if (en <= st)
                throw new InvalidOperationException($"Slot end must be after start: {s.StartTime}-{s.EndTime}");
            newSlots.Add((st, en));
        }

        var ordered = newSlots.OrderBy(s => s.Start).ToList();
        for (int i = 1; i < ordered.Count; i++)
            if (ordered[i].Start < ordered[i - 1].End)
                throw new InvalidOperationException("New slots overlap each other");

        var openHour = targetCourt.OpenTime.Hour;
        var closeHour = targetCourt.CloseTime.Hour == 0 ? 24 : targetCourt.CloseTime.Hour;
        foreach (var (start, end) in ordered)
            if (start.Hour < openHour || end.Hour > closeHour)
                throw new InvalidOperationException($"{start:HH\\:mm}-{end:HH\\:mm} is outside this court's operating hours");

        foreach (var (start, end) in ordered)
        {
            var conflicting = await _db.Bookings
                .Where(b => b.Id != booking.Id
                    && b.CourtId == targetCourtId
                    && b.Date == targetDate
                    && b.Status != "cancelled"
                    && b.Status != "expired"
                    && b.Status != "rejected"
                    && b.Status != "refunded")
                .SelectMany(b => b.Slots)
                .Where(s => s.Date == targetDate && s.StartTime < end && s.EndTime > start)
                .AnyAsync();
            if (conflicting)
                throw new InvalidOperationException($"Time slot {start:HH\\:mm}-{end:HH\\:mm} is already booked on this court");
        }

        var blocked = await _db.BlockedDates
            .Where(bd => bd.ClientId == clientId
                && bd.Date == targetDate
                && (bd.CourtId == null || bd.CourtId == targetCourtId))
            .ToListAsync();

        foreach (var bd in blocked)
        {
            if (bd.StartTime == null)
                throw new InvalidOperationException("This date is fully blocked for that court");

            var blockStart = bd.StartTime.Value;
            var blockEnd = bd.EndTime?.Hour == 0 ? new TimeOnly(23, 59) : (bd.EndTime ?? new TimeOnly(23, 59));

            foreach (var (start, end) in ordered)
                if (start < blockEnd && end > blockStart)
                    throw new InvalidOperationException($"Time slot {start:HH\\:mm}-{end:HH\\:mm} overlaps a blocked window");
        }

        var previousSlots = booking.Slots
            .OrderBy(s => s.StartTime)
            .Select(s => new TimeSlotDto(
                s.Id.ToString(), s.Date.ToString("yyyy-MM-dd"),
                s.StartTime.ToString("HH:mm"), s.EndTime.ToString("HH:mm"),
                false, s.Price))
            .ToList();
        var previousDate = booking.Date;
        var previousCourtId = booking.CourtId;
        var previousCourtName = booking.Court?.Name ?? "";
        var previousAmount = booking.TotalAmount;

        var hoursTotal = ordered.Sum(s => (decimal)(s.End - s.Start).TotalHours);
        var newTotal = hoursTotal * targetCourt.PricePerHour;

        _db.TimeSlots.RemoveRange(booking.Slots);

        booking.Slots = ordered.Select(s => new TimeSlot
        {
            BookingId = booking.Id,
            CourtId = targetCourtId,
            Date = targetDate,
            StartTime = s.Start,
            EndTime = s.End,
            Price = newTotal / ordered.Count
        }).ToList();

        booking.CourtId = targetCourtId;
        booking.Court = targetCourt;
        booking.Date = targetDate;
        booking.TotalAmount = newTotal;

        var changeSummary =
            $"[RESCHEDULED {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC] " +
            $"{previousDate:yyyy-MM-dd} {previousCourtName} " +
            $"→ {targetDate:yyyy-MM-dd} {targetCourt.Name}" +
            (string.IsNullOrWhiteSpace(request.Reason) ? "" : $" · Reason: {request.Reason}") +
            (string.IsNullOrWhiteSpace(request.StaffNotes) ? "" : $" · Staff: {request.StaffNotes}");

        booking.Notes = string.IsNullOrWhiteSpace(booking.Notes)
            ? changeSummary : $"{booking.Notes}\n{changeSummary}";

        await _db.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(booking.CustomerEmail))
        {
            try
            {
                await _email.NotifyCustomerBookingRescheduledAsync(
                    booking.CustomerEmail, booking.CustomerName, booking.ReferenceCode,
                    previousDate.ToString("yyyy-MM-dd"),
                    string.Join(", ", previousSlots.Select(s => $"{s.StartTime}-{s.EndTime}")),
                    previousCourtName,
                    targetDate.ToString("yyyy-MM-dd"),
                    string.Join(", ", ordered.Select(s => $"{s.Start:HH\\:mm}-{s.End:HH\\:mm}")),
                    targetCourt.Name,
                    $"₱{newTotal:N2}", request.Reason);
            }
            catch { }
        }

        var delta = newTotal - previousAmount;
        return new RescheduleBookingResponse(
            MapToDto(booking, targetCourt.Name), previousSlots,
            previousDate.ToString("yyyy-MM-dd"), previousCourtId.ToString(), previousCourtName,
            previousAmount, newTotal, delta > 0 ? delta : 0m, delta < 0 ? -delta : 0m);
    }

    public async Task<BookingDto> CreateStaffBookingAsync(StaffCreateBookingRequest request, Guid clientId)
    {
        if (!Guid.TryParse(request.CourtId, out var courtGuid))
            throw new InvalidOperationException("Invalid court ID format");

        var court = await _db.Courts
            .FirstOrDefaultAsync(c => c.Id == courtGuid && c.ClientId == clientId)
            ?? throw new KeyNotFoundException("Court not found");

        if (!DateTime.TryParse(request.Date, out var bookingDate))
            throw new InvalidOperationException("Invalid date format");

        if (request.Slots == null || !request.Slots.Any())
            throw new InvalidOperationException("At least one time slot is required");

        foreach (var slot in request.Slots)
        {
            if (!TimeOnly.TryParse(slot.StartTime, out var startTime))
                throw new InvalidOperationException($"Invalid start time: {slot.StartTime}");
            if (!TimeOnly.TryParse(slot.EndTime, out var endTime))
                throw new InvalidOperationException($"Invalid end time: {slot.EndTime}");

            var conflicting = await _db.Bookings
                .Where(b => b.CourtId == courtGuid
                    && b.Date == bookingDate
                    && b.Status != "cancelled"
                    && b.Status != "expired"
                    && b.Status != "rejected"
                    && b.Status != "refunded")
                .SelectMany(b => b.Slots)
                .Where(s => s.Date == bookingDate && s.StartTime < endTime && s.EndTime > startTime)
                .AnyAsync();
            if (conflicting)
                throw new InvalidOperationException($"Time slot {slot.StartTime}-{slot.EndTime} is already booked");
        }

        var mode = (request.PaymentMode ?? "cash").ToLowerInvariant();
        string status, paymentMethod;
        DateTime? expiresAt;
        switch (mode)
        {
            case "gcash": status = "pending_payment"; paymentMethod = "gcash"; expiresAt = DateTime.UtcNow.AddMinutes(15); break;
            case "pay_later": status = "pending_payment"; paymentMethod = "cash"; expiresAt = null; break;
            case "free":
            case "comp": status = "confirmed"; paymentMethod = "comp"; expiresAt = null; break;
            case "cash":
            default: status = "confirmed"; paymentMethod = "cash"; expiresAt = null; break;
        }

        decimal totalAmount;
        if (request.TotalAmount.HasValue && request.TotalAmount.Value >= 0)
        {
            totalAmount = request.TotalAmount.Value;
        }
        else
        {
            var totalMinutes = request.Slots.Sum(s =>
            {
                TimeOnly.TryParse(s.StartTime, out var st);
                TimeOnly.TryParse(s.EndTime, out var en);
                return (en - st).TotalMinutes;
            });
            totalAmount = court.PricePerHour * (decimal)(totalMinutes / 60.0);
        }

        var referenceCode = $"ST-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";

        var booking = new Booking
        {
            ClientId = clientId,
            CourtId = courtGuid,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail ?? "",
            CustomerPhone = request.CustomerPhone,
            ReferenceCode = referenceCode,
            Date = bookingDate,
            TotalAmount = totalAmount,
            Status = status,
            PaymentMethod = paymentMethod,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            PaymentExpiresAt = expiresAt,
            Slots = request.Slots.Select(s => new TimeSlot
            {
                CourtId = courtGuid,
                Date = bookingDate,
                StartTime = TimeOnly.Parse(s.StartTime),
                EndTime = TimeOnly.Parse(s.EndTime),
                Price = totalAmount / request.Slots.Count
            }).ToList()
        };

        _db.Bookings.Add(booking);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsReferenceCodeCollision(ex))
        {
            _db.Entry(booking).State = EntityState.Detached;
            booking.ReferenceCode = $"ST-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
            _db.Bookings.Add(booking);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException retryEx)
            {
                _db.Entry(booking).State = EntityState.Detached;
                throw new InvalidOperationException(
                    "We couldn't complete the booking due to a temporary error. Please try again.", retryEx);
            }
        }

        // ✅ audit
        await _security.LogAsync(new SecurityEvent
        {
            ClientId = clientId,
            EventType = "booking.created_staff",
            Severity = "info",
            Message = $"Staff booking created: {booking.ReferenceCode}",
            Metadata = System.Text.Json.JsonSerializer.Serialize(new
            {
                referenceCode = booking.ReferenceCode,
                courtId = courtGuid,
                date = bookingDate.ToString("yyyy-MM-dd"),
                amount = totalAmount,
                status,
                paymentMethod,
            }),
            Success = true,
        });

        if (request.SendConfirmation && !string.IsNullOrWhiteSpace(booking.CustomerEmail))
        {
            try
            {
                await _email.NotifyCustomerBookingConfirmedAsync(
                    booking.CustomerEmail,
                    booking.CustomerName,
                    booking.ReferenceCode,
                    booking.Date.ToString("yyyy-MM-dd"),
                    string.Join(", ", booking.Slots.Select(s => $"{s.StartTime:HH:mm} - {s.EndTime:HH:mm}")),
                    $"₱{booking.TotalAmount:N2}");
            }
            catch { }
        }

        return MapToDto(booking, court.Name);
    }

    public async Task<List<BookingSummaryDto>> TrackBookingSummariesByEmailAsync(string email, Guid clientId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new InvalidOperationException("Email is required.");

        var normalized = email.Trim().ToLowerInvariant();

        var bookings = await _db.Bookings
            .Where(b => b.ClientId == clientId
                     && b.CustomerEmail.ToLower() == normalized
                     && (b.Status == "pending_payment" || b.Status == "payment_submitted"))
            .Include(b => b.Court)
            .Include(b => b.Slots)
            .OrderByDescending(b => b.CreatedAt)
            .Take(20)
            .ToListAsync();

        return bookings.Select(b =>
        {
        var firstSlot = b.Slots.OrderBy(s => s.StartTime).FirstOrDefault();
        var lastSlot = b.Slots.OrderByDescending(s => s.EndTime).FirstOrDefault();

        return new BookingSummaryDto(
            b.Id.ToString(),
            b.ReferenceCode,
            b.Court?.Name ?? "",
            b.Date.ToString("yyyy-MM-dd"),
            firstSlot?.StartTime.ToString("HH:mm") ?? "",
            lastSlot?.EndTime.ToString("HH:mm") ?? "",
            b.Status,
            b.TotalAmount,
            b.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                       );
        }).ToList();
    }
}