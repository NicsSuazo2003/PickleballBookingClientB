using System;

namespace PickleballBookingSystem.Entities;

public class OpenPlaySession
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    // Primary court (kept for backward compatibility — first court in SessionCourts)
    public Guid CourtId { get; set; }
    public Court Court { get; set; } = null!;

    // ✅ NEW — every court this session uses
    public ICollection<OpenPlaySessionCourt> SessionCourts { get; set; }
        = new List<OpenPlaySessionCourt>();

    // ✅ FIX: Ensure Date is always UTC
    private DateTime _date;
    public DateTime Date
    {
        get => _date;
        set => _date = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int MaxPlayers { get; set; } = 12;
    public int CurrentPlayers { get; set; } = 0;

    public decimal PricePerPlayer { get; set; }

    public string SkillLevel { get; set; } = "All Levels";
    public string? HostName { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    // ✅ FIX: Ensure CreatedAt is always UTC
    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}