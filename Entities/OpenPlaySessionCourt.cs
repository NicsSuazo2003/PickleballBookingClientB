namespace PickleballBookingSystem.Entities;

public class OpenPlaySessionCourt
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OpenPlaySessionId { get; set; }
    public OpenPlaySession OpenPlaySession { get; set; } = null!;

    public Guid CourtId { get; set; }
    public Court Court { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}