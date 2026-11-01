// Entities/Client.cs
using System.ComponentModel.DataAnnotations.Schema;

namespace PickleballBookingSystem.Entities;

public class Client
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#1A2E1A";
    public string AccentColor { get; set; } = "#C9A94E";
    public string? GcashNumber { get; set; }
    public string? GcashAccountName { get; set; }

    [Column(TypeName = "jsonb")]
    public string? PaymentMethods { get; set; }

    [Column(TypeName = "jsonb")]
    public string? AvailableAmenities { get; set; }

    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public string Status { get; set; } = "active";

    // ✅ NEW — Batch A booking-window fields
    // Mode: "rolling" | "fixed" | "closed"
    //   rolling = bookings open relative to today (e.g. 30 days ahead)
    //   fixed   = bookings only between BookingOpenDate and BookingCloseDate
    //   closed  = bookings temporarily disabled
    public string BookingWindowMode { get; set; } = "rolling";

    private DateTime? _bookingOpenDate;
    public DateTime? BookingOpenDate
    {
        get => _bookingOpenDate;
        set => _bookingOpenDate = value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null;
    }

    private DateTime? _bookingCloseDate;
    public DateTime? BookingCloseDate
    {
        get => _bookingCloseDate;
        set => _bookingCloseDate = value.HasValue
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : null;
    }

    public ICollection<Court> Courts { get; set; } = new List<Court>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}