namespace PickleballBookingSystem.DTOs;

public record ProcessPaymentRequest(
    string Method,
    decimal Amount,
    string BookingId,
    string? GcashNumber,
    string? CardNumber,
    string? CardExpiry,
    string? CardCvv
);

public record PaymentResponse(bool Success, string TransactionId);