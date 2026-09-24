namespace PickleballBookingSystem.Entities;

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // ✅ NEW - Link to Court
    public Guid CourtId { get; set; }
    public Court Court { get; set; } = null!;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = "pending_payment";
    public string PaymentMethod { get; set; } = "gcash";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PaymentExpiresAt { get; set; }
    public string? PaymentScreenshot { get; set; }
    public string? PaymentReference { get; set; }
    public string? Notes { get; set; }
    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;
    public Guid? OpenPlaySessionId { get; set; }
    public OpenPlaySession? OpenPlaySession { get; set; }


    public ICollection<TimeSlot> Slots { get; set; } = new List<TimeSlot>();
}   