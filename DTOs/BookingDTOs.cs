namespace PickleballBookingSystem.DTOs;

public record CreateBookingRequest(
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string CourtId,           // ✅ NEW - Which court to book
    string Date,
    List<SlotRequest> Slots,
    decimal TotalAmount,
    string? Notes,
    string? Status = null,
    bool AdminOverride = false
);

public record SlotRequest(string StartTime, string EndTime);

public record BookingDto(
    string Id,
    string CourtId,           // ✅ NEW
    string CourtName,         // ✅ NEW - For display
    string CustomerName,
    string CustomerEmail,
    string? CustomerPhone,
    string ReferenceCode,
    string Date,
    List<TimeSlotDto> Slots,
    decimal TotalAmount,
    string Status,
    string PaymentMethod,
    string CreatedAt,
    string? Notes,
    string? PaymentScreenshot,
    DateTime? PaymentExpiresAt,
    string? PaymentReference = null
);

public record TimeSlotDto(
    string Id,
    string Date,
    string StartTime,
    string EndTime,
    bool IsAvailable,
    decimal Price
);

public record AdminUpdateBookingRequest(string Status);

public record TrackBookingRequest(string ReferenceCode, string Email);

public record UploadPaymentScreenshotRequest(IFormFile Screenshot);

// ✅ NEW: staff manual booking
public record StaffCreateBookingRequest(
    string CourtId,
    string Date,
    List<StaffSlotRequest> Slots,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    string? Notes,
    string PaymentMode,       // "cash" | "gcash" | "pay_later" | "free"
    decimal? TotalAmount,     // null = auto-calculate
    string? StaffNotes,       // internal-only notes
    bool SendConfirmation     // whether to email the customer
);

public record StaffSlotRequest(
    string StartTime,
    string EndTime
);

public record BookingSummaryDto(
    string Id,
    string ReferenceCode,
    string CourtName,
    string Date,
    string StartTime,
    string EndTime,
    string Status,
    decimal TotalAmount,
    string CreatedAt
);