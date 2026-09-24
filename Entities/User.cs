// Entities/User.cs
namespace PickleballBookingSystem.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role { get; set; } = "staff";
    public string? Avatar { get; set; }

    // ✅ FIX: Ensure CreatedAt is always UTC
    private DateTime _createdAt;
    public DateTime CreatedAt
    {
        get => _createdAt;
        set => _createdAt = DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    public int BookingsCount { get; set; }
    public string Status { get; set; } = "active";

    // ✅ Add ClientId to associate user with a client
    public Guid ClientId { get; set; }
    public Client? Client { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}