namespace PickleballBookingSystem.Entities;

public class PriceRule
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;        // e.g., "Weekday Peak", "Weekend"
    public string DayOfWeek { get; set; } = string.Empty;    // "Monday", "Tuesday", ... "Weekend", "Weekday", or "All"
    public TimeOnly StartTime { get; set; }                   // e.g., 17:00
    public TimeOnly EndTime { get; set; }                     // e.g., 22:00
    public decimal PricePerHour { get; set; }
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;                   // Higher = overrides lower
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
}