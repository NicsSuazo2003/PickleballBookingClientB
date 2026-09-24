using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PickleballBookingSystem.DTOs;
using PickleballBookingSystem.Interfaces;
using PickleballBookingSystem.Middleware;

namespace PickleballBookingSystem.Controllers;

[ApiController, Route("api/bookings")]
public class BookingController : ControllerBase
{
    private readonly IBookingService _booking;
    private readonly IConfiguration _config;
    private readonly ClientResolver _clientResolver;
    private readonly IClientService _clientService;

    public BookingController(
        IBookingService booking,
        IConfiguration config,
        ClientResolver clientResolver,
        IClientService clientService)
    {
        _booking = booking;
        _config = config;
        _clientResolver = clientResolver;
        _clientService = clientService;
    }

    private async Task<Guid> GetClientId()
    {
        var subdomain = _clientResolver.GetSubdomain();
        if (string.IsNullOrEmpty(subdomain))
            throw new UnauthorizedAccessException("Client identification required");

        try
        {
            return await _clientService.GetClientIdBySubdomainAsync(subdomain);
        }
        catch (KeyNotFoundException)
        {
            throw new UnauthorizedAccessException($"Client not found for subdomain: {subdomain}");
        }
    }

    [HttpPost]
    public async Task<ActionResult<BookingDto>> Create(CreateBookingRequest request)
    {
        var clientId = await GetClientId();
        var booking = await _booking.CreateBookingAsync(request, clientId);
        return CreatedAtAction(nameof(Track), new { referenceCode = booking.ReferenceCode }, booking);
    }

    [HttpGet("track/{referenceCode}")]
    public async Task<ActionResult<BookingDto>> Track(string referenceCode, [FromQuery] string? email)
    {
        var clientId = await GetClientId();
        var booking = await _booking.TrackBookingAsync(referenceCode, email, clientId);
        if (booking == null) return NotFound(new { message = "Booking not found" });
        return Ok(booking);
    }

    [HttpGet("track-summaries-by-email")]
    public async Task<ActionResult<List<BookingSummaryDto>>> TrackSummariesByEmail([FromQuery] string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return BadRequest(new { message = "Email is required" });

        var clientId = await GetClientId();
        var summaries = await _booking.TrackBookingSummariesByEmailAsync(email, clientId);
        return Ok(summaries);
    }

    // ✅ UPDATED — accepts an optional paymentMethod from the checkout form
    [HttpPost("{id}/upload-payment")]
    public async Task<ActionResult<BookingDto>> UploadPayment(
        Guid id,
        [FromForm] string paymentReference,
        [FromForm] string? paymentMethod,
        IFormFile? screenshot)
    {
        if (string.IsNullOrWhiteSpace(paymentReference))
            return BadRequest(new { message = "Payment reference is required" });

        var clientId = await GetClientId();
        string? screenshotUrl = null;

        // Screenshot is optional — only hit Supabase if one was actually sent
        if (screenshot != null && screenshot.Length > 0)
        {
            var supabaseUrl = _config["Supabase:Url"]!;
            var supabaseKey = _config["Supabase:Key"]!;
            var fileName = $"payment-{Guid.NewGuid()}{Path.GetExtension(screenshot.FileName)}";

            using var content = new StreamContent(screenshot.OpenReadStream());
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(screenshot.ContentType);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post,
                $"{supabaseUrl}/storage/v1/object/PickleImgs/{fileName}")
            {
                Content = content
            };
            httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", supabaseKey);

            var http = new HttpClient();
            var response = await http.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode)
                return BadRequest(new { message = "Screenshot upload failed" });

            screenshotUrl = $"{supabaseUrl}/storage/v1/object/public/PickleImgs/{fileName}";
        }

        // ✅ Pass paymentMethod through to the service
        var booking = await _booking.UploadPaymentScreenshotAsync(
            id,
            screenshotUrl,
            paymentReference,
            paymentMethod,
            clientId);

        return Ok(booking);
    }

    [Authorize(Roles = "admin"), HttpGet("admin")]
    public async Task<ActionResult<List<BookingDto>>> GetAll()
    {
        var clientId = await GetClientId();
        var bookings = await _booking.GetAllBookingsAsync(clientId);
        return Ok(bookings);
    }

    [Authorize(Roles = "admin"), HttpPut("admin/{id}")]
    public async Task<ActionResult<BookingDto>> AdminUpdate(Guid id, AdminUpdateBookingRequest request)
    {
        var clientId = await GetClientId();
        var booking = await _booking.AdminUpdateBookingAsync(id, request, clientId);
        return Ok(booking);
    }

    [Authorize(Roles = "admin"), HttpPost("admin-create")]
    public async Task<ActionResult<BookingDto>> AdminCreate(CreateBookingRequest request)
    {
        var clientId = await GetClientId();
        request = request with { AdminOverride = true };
        var booking = await _booking.CreateBookingAsync(request, clientId);
        return Ok(booking);
    }
}