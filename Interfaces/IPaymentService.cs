using PickleballBookingSystem.DTOs;

namespace PickleballBookingSystem.Interfaces;

public interface IPaymentService
{
    Task<PaymentResponse> ProcessAsync(ProcessPaymentRequest request);
}