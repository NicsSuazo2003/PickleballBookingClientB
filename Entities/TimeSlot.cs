namespace PickleballBookingSystem.Entities;

public class TimeSlot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }

    // ✅ NEW - Direct link to Court (optional, can use Booking.CourtId)
    public Guid? CourtId { get; set; }
    public Court? Court { get; set; }

    // ✅ FIX: Ensure Date is always UTC
    private DateTime _date;
    public DateTime Date
    {
        get => _date;
        set => _date = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal Price { get; set; }

    public Booking Booking { get; set; } = null!;
}