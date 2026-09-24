namespace PickleballBookingSystem.Entities;

public class BlockedDate
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ✅ Link to Court (optional - null means all courts)
    public Guid? CourtId { get; set; }
    public Court? Court { get; set; }

    // ✅ FIX: Ensure Date is always UTC
    private DateTime _date;
    public DateTime Date
    {
        get => _date;
        set => _date = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public string? Reason { get; set; }

    // ✅ FIX: Ensure CreatedAt is always UTC
    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
}