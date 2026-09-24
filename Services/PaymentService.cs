using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Interfaces;

namespace PickleballBookingSystem.Services;

public class PaymentService : IPaymentService
{
    public async Task<PaymentResponse> ProcessAsync(ProcessPaymentRequest request)
    {
        await Task.Delay(500);

        if (request.Method == "card" && request.CardNumber?.Replace(" ", "").StartsWith("0000") == true)
            throw new InvalidOperationException("Card declined. Please try a different payment method.");

        return new PaymentResponse(true, $"txn-{DateTime.UtcNow:yyyyMMddHHmmss}");
    }
}