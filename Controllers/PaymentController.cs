using Microsoft.AspNetCore.Mvc;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Interfaces;

namespace PickleballBookingSystem.Controllers;

[ApiController, Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _payment;

    public PaymentController(IPaymentService payment) => _payment = payment;

    [HttpPost("process")]
    public async Task<ActionResult<PaymentResponse>> Process(ProcessPaymentRequest request)
    {
        var response = await _payment.ProcessAsync(request);
        return Ok(response);
    }
}